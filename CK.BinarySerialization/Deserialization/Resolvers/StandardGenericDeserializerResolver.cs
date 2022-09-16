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
        //  - The (Type LocalType, IDeserializationDriver WrittenUnderlyingDriverType) for an enum: the target enum
        //    must exist locally but its current underlying type may not be the same as the written one.
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
                        // If info.TargetType.IsEnum is false (the target type is NOT an enum), we could do this for integral types (it works):
                        //
                        //   var tDiffOther = typeof( Deserialization.DEnumDiff<,,> ).MakeGenericType( info.TargetType, info.TargetType, uD.ResolvedType );
                        //   return (IDeserializationDriver)Activator.CreateInstance( tDiffOther, uD.TypedReader )!;
                        //
                        // However we don't do this to stay consistent. Mutations must be coherent, if we do this then we should also handle 
                        // the opposite (written integral types read as enums). We may do this once.
                        // 
                        // For the moment, to support mapping to different enums types use deserialization hooks and
                        // IMutableTypeReadInfo.SetTargetType.
                        //
                        if( !info.ExpectedType.IsEnum ) return null;

                        // Enum is automatically adapted to its local type, including using any integral local type.
                        Debug.Assert( info.ReadInfo.Kind == TypeReadInfoKind.Enum && info.ReadInfo.SubTypes.Count == 1 );
                        var uD = info.ReadInfo.SubTypes[0].GetConcreteDriver( null );
                        // We cache only if no type adaptation is required.
                        if( info.ExpectedType.GetEnumUnderlyingType() == uD.ResolvedType )
                        {
                            return _cache.GetOrAdd( (info.ExpectedType, uD), CreateNominalEnum );
                        }

                        var tDiff = typeof( Deserialization.DEnumDiff<,,> )
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
                        var item = info.ReadInfo.SubTypes[0].GetPotentiallyAbstractDriver( null );
                        return item.IsCacheable
                                ? _cache.GetOrAdd( (item, info.ReadInfo.ArrayRank), CreateCachedArray )
                                : CreateArray( item, info.ReadInfo.ArrayRank );
                    }
                case "Dictionary": return TryGetDoubleGenericParameter( info.ReadInfo, typeof( Deserialization.DDictionary<,> ) );
                case "List": return TryGetSingleGenericParameter( info.ReadInfo, typeof( Deserialization.DList<> ) );
                case "Set": return TryGetSingleGenericParameter( info.ReadInfo, typeof( Deserialization.DHashSet<> ) );
                case "Stack": return TryGetSingleGenericParameter( info.ReadInfo, typeof( Deserialization.DStack<> ) );
                case "Queue": return TryGetSingleGenericParameter( info.ReadInfo, typeof( Deserialization.DQueue<> ) );
                case "KeyValuePair": return TryGetDoubleGenericParameter( info.ReadInfo, typeof( Deserialization.DKeyValuePair<,> ) );
            }
            return null;
        }

        IDeserializationDriver CreateTuple( ITypeReadInfo info, bool isValueTuple )
        {
            var tA = new IDeserializationDriver[info.SubTypes.Count];
            bool isCached = true;
            for( int i = 0; i < tA.Length; ++i )
            {
                var d = info.SubTypes[i].GetPotentiallyAbstractDriver( null );
                isCached &= d.IsCacheable;
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
            var item = info.SubTypes[0].GetPotentiallyAbstractDriver( null );
            return item.IsCacheable
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
            var item1 = info.SubTypes[0].GetPotentiallyAbstractDriver( null );
            var item2 = info.SubTypes[1].GetPotentiallyAbstractDriver( null );
            return item1.IsCacheable && item2.IsCacheable
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

        IDeserializationDriver CreateNominalEnum( object key )
        {
            var k = ((Type Target, IDeserializationDriver U))key;
            Debug.Assert( k.Target.GetEnumUnderlyingType() == k.U.ResolvedType );
            var tSame = typeof( Deserialization.DEnum<,> ).MakeGenericType( k.Target, k.U.ResolvedType );
            return (IDeserializationDriver)Activator.CreateInstance( tSame, k.U.TypedReader )!;
        }

    }
}
