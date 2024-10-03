using CK.BinarySerialization.Deserialization;
using CK.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace CK.BinarySerialization;

/// <summary>
/// Handles standard generic types. This registry relies on the <see cref="ITypeReadInfo.DriverName"/>
/// to try to resolve and cache the deserializers.
/// <para>
/// The cache key of the already resolved deserializers depends on the resolved type and can only 
/// be managed by each instance of this registry since the deserializers relies on other ones provided
/// by the bound <see cref="SharedBinaryDeserializerContext"/>.
/// </para>
/// </summary>
public sealed class StandardGenericDeserializerResolver : IDeserializerResolver
{
    // Caching here relies on the subordinated cached deserializers and the generic type to synthesize.
    // We can use a simple ConcurrentDictionary and its simple GetOrAdd: we can accept the concurrency issue
    // and duplicated calls to create functions (this should barely happen) and the GetOrAdd will
    // always return the winner.
    // The key is an object that contains the resolved subordinated items drivers (and may be an
    // optional type indicator) to the resolved driver.
    // Only if all the subtypes drivers are available do we build the final driver.
    // This lookup obviously costs but this is done only once per deserialization session
    // since the TypeReadInfo ultimately caches the final driver (including unresolved and non cached ones).
    // The object key is:
    //  - The local Type for an enum when the local target enum underlying type is the same as the written one.
    //  - TupleKey (see below) for ValueTuple and Tuple.
    //  - Boxed ValueTuple for other types:
    //     - (IDeserializationDriver Item, int Rank) for array (Rank >= 1).
    //     - (IDeserializationDriver Item, Type D) where D is the generic type definition for DList<>, DStack<> or DQueue<>.  
    readonly ConcurrentDictionary<object, IDeserializationDriver> _cache;
    readonly SharedBinaryDeserializerContext _context;

    class TupleKey : IEquatable<TupleKey>
    {
        public readonly IDeserializationDriver[] Drivers;
        public readonly bool IsValueTuple;

        public TupleKey( IDeserializationDriver[] d, bool isValueTuple )
        {
            Drivers = d;
            IsValueTuple = isValueTuple;
        }

        public bool Equals( [AllowNull] TupleKey other ) => other is not null
                                                            && IsValueTuple == other.IsValueTuple && Drivers.SequenceEqual( other.Drivers );
        public override bool Equals( object? obj ) => Equals( obj as TupleKey );
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            Array.ForEach( Drivers, hashCode.Add );
            return IsValueTuple ? hashCode.ToHashCode() : -hashCode.ToHashCode();
        }
    }

    /// <summary>
    /// Initializes a new <see cref="StandardGenericDeserializerResolver"/>.
    /// </summary>
    /// <param name="context">The bound shared context. Used only to detect mismatch of resolution context.</param>
    public StandardGenericDeserializerResolver( SharedBinaryDeserializerContext context ) 
    {
        _cache = new ConcurrentDictionary<object, IDeserializationDriver>();
        _context = context;
    }

    /// <summary>
    /// Synthesizes a deserialization driver for enumerations, list, array, value tuples and other 
    /// basic types.
    /// </summary>
    /// <param name="info">The info to resolve.</param>
    /// <returns>The driver or null.</returns>
    public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
    {
        Throw.CheckArgument( "Deserialization context mismatch.", _context == info.Context.Shared );
        switch( info.DriverName )
        {
            case "Enum":
                {
                    Debug.Assert( info.ReadInfo.Kind == TypeReadInfoKind.Enum && info.ReadInfo.SubTypes.Count == 1 );

                    if( !info.ExpectedType.IsEnum )
                    {
                        var target = BasicTypesDeserializerResolver.IsBasicallyConvertible( info.ExpectedType );
                        if( target == TypeCode.Empty )
                        {
                            return null;
                        }
                        var underlying = info.ReadInfo.SubTypes[0].GetConcreteDriver( null );
                        var tV = typeof( DChangeBasicType<,> ).MakeGenericType( info.ExpectedType, underlying.ResolvedType );
                        return (IDeserializationDriver)Activator.CreateInstance( tV, underlying.TypedReader, target )!;
                    }

                    // We cache only if no type adaptation is required and IsPossibleNominalDeserialization.
                    var uD = info.ReadInfo.SubTypes[0].GetConcreteDriver( null );
                    if( info.IsPossibleNominalDeserialization && info.ExpectedType.GetEnumUnderlyingType() == uD.ResolvedType )
                    {
                        return _cache.GetOrAdd( info.ExpectedType, CreateNominalEnum, uD );
                    }

                    var tDiff = typeof( DEnumDiff<,,> )
                                .MakeGenericType( info.ExpectedType, info.ExpectedType.GetEnumUnderlyingType(), uD.ResolvedType );
                    return (IDeserializationDriver)Activator.CreateInstance( tDiff, uD.TypedReader )!;
                }
            case "ValueTuple":
                {
                    return TryGetTuple( ref info, true );
                }
            case "Tuple":
                {
                    return TryGetTuple( ref info, false );
                }
            case "Array":
                {
                    Debug.Assert( info.ReadInfo.Kind == TypeReadInfoKind.Array );
                    Debug.Assert( info.ReadInfo.SubTypes.Count == 1 );
                    // Fast path
                    if( info.ExpectedType.IsArray )
                    {
                        var item = info.ReadInfo.SubTypes[0].GetPotentiallyAbstractDriver( info.ExpectedType.GetElementType() );
                        return item.IsCached
                                ? _cache.GetOrAdd( (item, info.ReadInfo.ArrayRank), CreateCachedArray )
                                : CreateArray( item, info.ReadInfo.ArrayRank );
                    }
                    if( info.ReadInfo.ArrayRank == 1 && info.ExpectedType.IsGenericType )
                    {
                        var eDef = info.ExpectedType.GetGenericTypeDefinition();
                        if( eDef == typeof( List<> ) )
                        {
                            return GetSingleGenericParameter( ref info, typeof( DList<> ) );
                        }
                        if( eDef == typeof( Stack<> ) )
                        {
                            return GetSingleGenericParameter( ref info, typeof( DStack<> ) );
                        }
                    }
                    return null;
                }
            case "Dictionary":
                if( !info.ExpectedType.IsGenericType || info.ExpectedType.GetGenericTypeDefinition() != typeof(Dictionary<,>) )
                {
                    return null;
                }
                return TryGetDoubleGenericParameter( ref info, typeof( DDictionary<,> ) );
            case "List":
            case "Stack":
                return TryGetListOrStack( ref info );
            case "Set":
                {
                    if( !info.ExpectedType.IsGenericType ) return null;
                    Type tGenTarget = info.ExpectedType.GetGenericTypeDefinition();
                    if( tGenTarget != typeof( HashSet<> ) ) return null;
                    return GetSingleGenericParameter( ref info, typeof( DHashSet<> ) );
                }
            case "Queue":
                {
                    if( !info.ExpectedType.IsGenericType ) return null;
                    Type tGenTarget = info.ExpectedType.GetGenericTypeDefinition();
                    if( tGenTarget != typeof( Queue<> ) ) return null;
                    return GetSingleGenericParameter( ref info, typeof( DQueue<> ) );
                }
            case "KeyValuePair":
                if( !info.ExpectedType.IsGenericType || info.ExpectedType.GetGenericTypeDefinition() != typeof( KeyValuePair<,> ) )
                {
                    return null;
                }
                return TryGetDoubleGenericParameter( ref info, typeof( DKeyValuePair<,> ) );
        }
        return null;
    }

    IDeserializationDriver? TryGetListOrStack( ref DeserializerResolverArg info )
    {
        if( info.ExpectedType.IsSZArray )
        {
            var item = info.ReadInfo.SubTypes[0].GetPotentiallyAbstractDriver( info.ExpectedType.GetElementType() );
            return item.IsCached
                    ? _cache.GetOrAdd( (item, 1), CreateCachedArray )
                    : CreateArray( item, 1 );
        }
        if( !info.ExpectedType.IsGenericType ) return null;
        Type tGenTarget = info.ExpectedType.GetGenericTypeDefinition();
        Type tGenD;
        if( tGenTarget == typeof( List<> ) )
        {
            tGenD = typeof( DList<> );
        }
        else if( tGenTarget == typeof( Stack<> ) )
        {
            tGenD = typeof( DStack<> );
        }
        else
        {
            return null;
        }
        return GetSingleGenericParameter( ref info, tGenD );
    }

    IDeserializationDriver? TryGetTuple( ref DeserializerResolverArg info, bool isValueTuple )
    {
        var tA = new IDeserializationDriver[info.ReadInfo.SubTypes.Count];
        if( !info.ExpectedType.IsGenericType ) return null;
        var args = info.ExpectedType.GetGenericArguments();
        if( args.Length != info.ReadInfo.SubTypes.Count ) return null;
        bool isCached = true;
        for( int i = 0; i < tA.Length; ++i )
        {
            var d = info.ReadInfo.SubTypes[i].GetPotentiallyAbstractDriver( args[i] );
            isCached &= d.IsCached;
            tA[i] = d;
        }
        var key = new TupleKey( tA, isValueTuple );
        return isCached 
                ? _cache.GetOrAdd( key, CreateCached )
                : CreateTuple( tA, isValueTuple, false );

        static IDeserializationDriver CreateCached( object key )
        {
            var k = (TupleKey)key;
            return CreateTuple( k.Drivers, k.IsValueTuple, true );
        }
        
        static IDeserializationDriver CreateTuple( IDeserializationDriver[] drivers, bool isValueTuple, bool isCached )
        {
            var types = drivers.Select( d => d.ResolvedType ).ToArray();
            var tG = types.Length switch
            {
                1 => isValueTuple ? typeof( Deserialization.DValueTuple<> ) : typeof( Deserialization.DTuple<> ),
                2 => isValueTuple ? typeof( Deserialization.DValueTuple<,> ) : typeof( Deserialization.DTuple<,> ),
                3 => isValueTuple ? typeof( Deserialization.DValueTuple<,,> ) : typeof( Deserialization.DTuple<,,> ),
                4 => isValueTuple ? typeof( Deserialization.DValueTuple<,,,> ) : typeof( Deserialization.DTuple<,,,> ),
                5 => isValueTuple ? typeof( Deserialization.DValueTuple<,,,,> ) : typeof( Deserialization.DTuple<,,,,> ),
                6 => isValueTuple ? typeof( Deserialization.DValueTuple<,,,,,> ) : typeof( Deserialization.DTuple<,,,,,> ),
                7 => isValueTuple ? typeof( Deserialization.DValueTuple<,,,,,,> ) : typeof( Deserialization.DTuple<,,,,,,> ),
                _ => throw new NotSupportedException( "Tuple or ValueTuple with 8 or more parameters are not supported." )
            };
            var tD = tG.MakeGenericType( types );
            return (IDeserializationDriver)Activator.CreateInstance( tD, drivers.Select( d => d.TypedReader ).ToArray(), isCached )!;
        }

    }

    IDeserializationDriver GetSingleGenericParameter( ref DeserializerResolverArg info, Type tGenD )
    {
        Debug.Assert( info.ReadInfo.SubTypes.Count == 1 );
        Debug.Assert( info.ExpectedType.GetGenericArguments().Length == 1 );
        var item = info.ReadInfo.SubTypes[0].GetPotentiallyAbstractDriver( info.ExpectedType.GetGenericArguments()[0] );
        return item.IsCached
                ? _cache.GetOrAdd( (item, tGenD), CreateCached )
                : Create( item, tGenD );

        static IDeserializationDriver CreateCached( object key )
        {
            var (item, tGenD) = ((IDeserializationDriver, Type))key;
            return Create( item, tGenD );
        }

        static IDeserializationDriver Create( IDeserializationDriver item, Type tGenD )
        {
            var tS = tGenD.MakeGenericType( item.ResolvedType );
            return (IDeserializationDriver)Activator.CreateInstance( tS, item )!;
        }
    }

    IDeserializationDriver? TryGetDoubleGenericParameter( ref DeserializerResolverArg info, Type tGenD )
    {
        Debug.Assert( info.ReadInfo.SubTypes.Count == 2 );
        Debug.Assert( tGenD.IsGenericType );
        var args = info.ExpectedType.GetGenericArguments();
        Debug.Assert( args.Length == 2 );
        var item1 = info.ReadInfo.SubTypes[0].GetPotentiallyAbstractDriver( args[0] );
        var item2 = info.ReadInfo.SubTypes[1].GetPotentiallyAbstractDriver( args[1] );
        return item1.IsCached && item2.IsCached
                ? _cache.GetOrAdd( (item1, item2, tGenD), CreateCached )
                : Create( item1, item2, tGenD );

        static IDeserializationDriver CreateCached( object key )
        {
            var (item1, item2, tGenD) = ((IDeserializationDriver, IDeserializationDriver, Type))key;
            return Create( item1, item2, tGenD );
        }
        
        static IDeserializationDriver Create( IDeserializationDriver item1, IDeserializationDriver item2, Type tGenD )
        {
            var tS = tGenD.MakeGenericType( item1.ResolvedType, item2.ResolvedType );
            return (IDeserializationDriver)Activator.CreateInstance( tS, item1, item2 )!;
        }
    }

    static IDeserializationDriver CreateCachedArray( object key )
    {
        var (item, rank) = ((IDeserializationDriver I, int Rank))key;
        return CreateArray( item, rank );
    }

    static IDeserializationDriver CreateArray( IDeserializationDriver item, int rank )
    {
        if( rank == 1 )
        {
            var t1 = typeof( Deserialization.DArray<> ).MakeGenericType( item.ResolvedType );
            return (IDeserializationDriver)Activator.CreateInstance( t1, item )!;
        }
        var tA = item.ResolvedType.MakeArrayType( rank );
        var tM = typeof( Deserialization.DArrayMD<,> ).MakeGenericType( tA, item.ResolvedType );
        return (IDeserializationDriver)Activator.CreateInstance( tM, item )!;
    }

    IDeserializationDriver CreateNominalEnum( object keyType, IDeserializationDriver underlying )
    {
        var type = (Type)keyType;
        Debug.Assert( type.GetEnumUnderlyingType() == underlying.ResolvedType );
        var tSame = typeof( Deserialization.DEnum<,> ).MakeGenericType( type, underlying.ResolvedType );
        return (IDeserializationDriver)Activator.CreateInstance( tSame, underlying.TypedReader )!;
    }

}
