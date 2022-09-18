using CK.BinarySerialization.Deserialization;
using CK.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace CK.BinarySerialization
{
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
                        return CreateTuple( info.ReadInfo, true );
                    }
                case "Tuple":
                    {
                        return CreateTuple( info.ReadInfo, false );
                    }
                case "Array":
                    {
                        Debug.Assert( info.ReadInfo.Kind == TypeReadInfoKind.Array );
                        Debug.Assert( info.ReadInfo.SubTypes.Count == 1 );
                        // Here, expected type should be info.TargetType.SubTypes[0] where
                        // TargetType is a NullableTypeTree...
                        var item = info.ReadInfo.SubTypes[0].GetPotentiallyAbstractDriver( null );
                        return item.IsCached
                                ? _cache.GetOrAdd( (item, info.ReadInfo.ArrayRank), CreateCachedArray )
                                : CreateArray( item, info.ReadInfo.ArrayRank );
                    }
                case "Dictionary": return TryGetDoubleGenericParameter( info.ReadInfo, typeof( DDictionary<,> ) );
                case "List": return TryGetSingleGenericParameter( info.ReadInfo, typeof( DList<> ) );
                case "Set": return TryGetSingleGenericParameter( info.ReadInfo, typeof( DHashSet<> ) );
                case "Stack": return TryGetSingleGenericParameter( info.ReadInfo, typeof( DStack<> ) );
                case "Queue": return TryGetSingleGenericParameter( info.ReadInfo, typeof( DQueue<> ) );
                case "KeyValuePair": return TryGetDoubleGenericParameter( info.ReadInfo, typeof( DKeyValuePair<,> ) );
            }
            return null;
        }

        IDeserializationDriver CreateTuple( ITypeReadInfo info, bool isValueTuple )
        {
            var tA = new IDeserializationDriver[info.SubTypes.Count];
            bool isCached = true;
            for( int i = 0; i < tA.Length; ++i )
            {
                // The expected type should be based on the NullableTypeTree TargetType SubTypes...
                var d = info.SubTypes[i].GetPotentiallyAbstractDriver( null );
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

        IDeserializationDriver TryGetSingleGenericParameter( ITypeReadInfo info, Type tGenD )
        {
            Debug.Assert( info.SubTypes.Count == 1 );
            // The expected type should be based on the NullableTypeTree TargetType SubTypes...
            var item = info.SubTypes[0].GetPotentiallyAbstractDriver( null );
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

        IDeserializationDriver TryGetDoubleGenericParameter( ITypeReadInfo info, Type tGenD )
        {
            Debug.Assert( info.SubTypes.Count == 2 );
            // The expected types should be based on the NullableTypeTree TargetType SubTypes...
            var item1 = info.SubTypes[0].GetPotentiallyAbstractDriver( null );
            var item2 = info.SubTypes[1].GetPotentiallyAbstractDriver( null );
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
}
