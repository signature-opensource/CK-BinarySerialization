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
    public sealed class StandardGenericDeserializerRegistry : IDeserializerResolver
    {
        // Caching here relies on the subordinated deserializers and the generic type to synthesize.
        // We can use a simple ConcurrentDictionary and its simple GetOrAdd method since 2
        // deserializers with the same subordinated deserializers: we can accept the concurrency issue
        // and duplicated calls to create functions (this should barely happen) and the GetOrAdd will
        // always return the winner.
        // The key is an object that contains the resolved subordinated items drivers (and may be an
        // optional type indicator) to the resolved driver.
        // Only if all the subtypes drivers are available do we build the final driver.
        // This lookup obviously costs but this is done only once per deserialization session
        // since the TypeReadInfo caches the final driver (including unresolved ones).
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
        /// Initializes a new <see cref="StandardGenericDeserializerRegistry"/>.
        /// </summary>
        /// <param name="context">The bound shared context. Used only to detect mismatch of resolution context.</param>
        public StandardGenericDeserializerRegistry( SharedBinaryDeserializerContext context ) 
        {
            _cache = new ConcurrentDictionary<object, IDeserializationDriver>();
            _context = context;
        }

        public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
        {
            if( _context != info.Context ) throw new ArgumentException( "Deserialization context mismatch." );
            switch( info.DriverName )
            {
                case "Enum":
                    {
                        // Enum is automatically adapted to its local type, including using any integral local type.
                        Debug.Assert( info.ReadInfo.Kind == TypeReadInfoKind.Enum && info.ReadInfo.SubTypes.Count == 1 );
                        var uD = info.ReadInfo.SubTypes[0].GetConcreteDriver();
                        // We cache only the "nominal deserializer".
                        if( info.TargetType == uD.ResolvedType )
                        {
                            return _cache.GetOrAdd( (info.TargetType, uD), CreateNominalEnum );
                        }
                        // If info.TargetType.IsEnum is false (the target type is NOT an enum), we could do this (it works):
                        //
                        //   var tDiffOther = typeof( Deserialization.DEnumDiff<,,> ).MakeGenericType( info.TargetType, info.TargetType, uD.ResolvedType );
                        //   return (IDeserializationDriver)Activator.CreateInstance( tDiffOther, uD.TypedReader )!;
                        //
                        // However we don't to stay consistent. Mutations must be coherent, if we do this then we should also handle 
                        // the opposite (written integral types read as enums). We may do this once.
                        // 
                        // For the moment, supporting mapping to different enums types only makes deserialization hooks and
                        // IMutableTypeReadInfo.SetTargetType a simple way to handle enum migrations.
                        //
                        if( !info.TargetType.IsEnum ) return null;

                        var tDiff = typeof( Deserialization.DEnumDiff<,,> )
                                    .MakeGenericType( info.TargetType, info.TargetType.GetEnumUnderlyingType(), uD.ResolvedType );
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
                        var item = info.ReadInfo.SubTypes[0].GetPotentiallyAbstractDriver();
                        return _cache.GetOrAdd( (item, info.ReadInfo.ArrayRank), CreateArray );
                    }
                case "Dictionary": return TryGetDoubleGenericParameter( info.ReadInfo, typeof( Deserialization.DDictionary<,> ) );
                case "List": return TryGetSingleGenericParameter( info.ReadInfo, typeof( Deserialization.DList<> ) );
                case "Stack": return TryGetSingleGenericParameter( info.ReadInfo, typeof( Deserialization.DStack<> ) );
                case "Queue": return TryGetSingleGenericParameter( info.ReadInfo, typeof( Deserialization.DQueue<> ) );
            }
            return null;
        }

        IDeserializationDriver? CreateTuple( ITypeReadInfo info, bool isValueTuple )
        {
            var tA = new IDeserializationDriver[info.SubTypes.Count];
            for( int i = 0; i < tA.Length; ++i )
            {
                tA[i] = info.SubTypes[i].GetPotentiallyAbstractDriver();
            }
            var key = new TupleKey( tA, isValueTuple );
            return _cache.GetOrAdd( key, DoCreateTuple );

            static IDeserializationDriver DoCreateTuple( object key )
            {
                var k = (TupleKey)key;
                var types = k.Drivers.Select( d => d.ResolvedType ).ToArray();
                var tG = types.Length switch
                {
                    1 => k.IsValueTuple ? typeof( Deserialization.DValueTuple<> ) : typeof( Deserialization.DTuple<> ),
                    2 => k.IsValueTuple ? typeof( Deserialization.DValueTuple<,> ) : typeof( Deserialization.DTuple<,> ),
                    3 => k.IsValueTuple ? typeof( Deserialization.DValueTuple<,,> ) : typeof( Deserialization.DTuple<,,> ),
                    4 => k.IsValueTuple ? typeof( Deserialization.DValueTuple<,,,> ) : typeof( Deserialization.DTuple<,,,> ),
                    5 => k.IsValueTuple ? typeof( Deserialization.DValueTuple<,,,,> ) : typeof( Deserialization.DTuple<,,,,> ),
                    6 => k.IsValueTuple ? typeof( Deserialization.DValueTuple<,,,,,> ) : typeof( Deserialization.DTuple<,,,,,> ),
                    7 => k.IsValueTuple ? typeof( Deserialization.DValueTuple<,,,,,,> ) : typeof( Deserialization.DTuple<,,,,,,> ),
                    _ => throw new NotSupportedException( "Tuple or ValueTuple with 8 or more parameters are not supported." )
                };
                var tD = tG.MakeGenericType( types );
                return (IDeserializationDriver)Activator.CreateInstance( tD, new object?[] { k.Drivers.Select( d => d.TypedReader ).ToArray() } )!;
            }
        }

        IDeserializationDriver? TryGetSingleGenericParameter( ITypeReadInfo info, Type tGenD )
        {
            Debug.Assert( info.SubTypes.Count == 1 );
            var item = info.SubTypes[0].GetPotentiallyAbstractDriver();
            var k = (item, tGenD);
            var d = _cache.GetOrAdd( k, CreateSingleGenericTypeParam );
            return d;

            static IDeserializationDriver CreateSingleGenericTypeParam( object key )
            {
                var k = ((IDeserializationDriver I, Type D))key;
                var tS = k.D.MakeGenericType( k.I.ResolvedType );
                return (IDeserializationDriver)Activator.CreateInstance( tS, k.I.TypedReader )!;
            }
        }

        IDeserializationDriver? TryGetDoubleGenericParameter( ITypeReadInfo info, Type tGenD )
        {
            Debug.Assert( info.SubTypes.Count == 2 );
            var item1 = info.SubTypes[0].GetPotentiallyAbstractDriver();
            var item2 = info.SubTypes[1].GetPotentiallyAbstractDriver();
            var k = (item1, item2, tGenD);
            return _cache.GetOrAdd( k, CreateDoubleGenericTypeParam );

            static IDeserializationDriver CreateDoubleGenericTypeParam( object key )
            {
                var k = ((IDeserializationDriver I1, IDeserializationDriver I2, Type D))key;
                var tS = k.D.MakeGenericType( k.I1.ResolvedType, k.I2.ResolvedType );
                return (IDeserializationDriver)Activator.CreateInstance( tS, k.I1.TypedReader, k.I2.TypedReader )!;
            }
        }

        IDeserializationDriver CreateArray( object key )
        {
            var k = ((IDeserializationDriver I, int Rank))key;
            if( k.Rank == 1 )
            {
                var t1 = typeof( Deserialization.DArray<> ).MakeGenericType( k.I.ResolvedType );
                return (IDeserializationDriver)Activator.CreateInstance( t1, k.I.TypedReader )!;
            }
            var tA = k.I.ResolvedType.MakeArrayType( k.Rank );
            var tM = typeof( Deserialization.DArrayMD<,> ).MakeGenericType( tA, k.I.ResolvedType );
            return (IDeserializationDriver)Activator.CreateInstance( tM, k.I.TypedReader )!;
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
