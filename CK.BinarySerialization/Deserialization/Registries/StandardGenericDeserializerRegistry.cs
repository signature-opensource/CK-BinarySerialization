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
    /// </summary>
    public class StandardGenericDeserializerRegistry : IDeserializerResolver
    {
        // We cannot use the final Type as a key: a List<Car> may be deserialized as a 
        // List<SuperCar>.
        // We can use a simple ConcurrentDictionary and its simple GetOrAdd method since 2
        // deserializers of the same TypeReadInfo are identical: we can accept the concurrency issue
        // and live with some duplicated deserializers (this should barely happen).
        // The key is an object that contains the resolved items drivers (and may be an
        // optional type indicator) to the resolved driver.
        // Only if all the subtypes drivers are available do we build the final driver.
        // This lookup obviously costs but this is done only once per deserialization session
        // since the TypeReadInfo caches the final driver (including unresolved ones).
        // Note that subtypes driver resolution sollicitates the whole set of resolvers (including this one).
        // The object key is:
        //  - The (Type LocalType, IDeserializationDriver WrittenUnderlyingDriverType) for an enum: the target enum
        //    must exist locally but its current underlying type may not be the same as the written one.
        //  - Boxed ValueTuple for other types:
        //     - TupleKey below for ValueTuple and Tuple.
        //     - (IDeserializationDriver Item, int Rank) for array (Rank >= 1).
        //     - (IDeserializationDriver Item, Type D) where D is the generic type definition for DList<>, DStack<> or DQueue<>.  
        readonly ConcurrentDictionary<object, IDeserializationDriver> _cache;
        readonly IDeserializerResolver _resolver;

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
        /// Gets the default registry.
        /// </summary>
        public static readonly IDeserializerResolver Default = new StandardGenericDeserializerRegistry( BinaryDeserializer.DefaultSharedContext );

        public StandardGenericDeserializerRegistry( IDeserializerResolver resolver ) 
        {
            _cache = new ConcurrentDictionary<object, IDeserializationDriver>();
            _resolver = resolver;
        }

        public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
        {
            switch( info.DriverName )
            {
                case "Enum":
                    {
                        // Enum is automatically adapted to its local type, including using any integral local type.
                        Debug.Assert( info.Info.Kind == TypeReadInfoKind.Enum && info.Info.SubTypes.Count == 1 );
                        var uD = info.Info.SubTypes[0].GetDeserializationDriver();
                        return _cache.GetOrAdd( (info.LocalType, uD), CreateEnum );
                    }
                case "ValueTuple":
                    {
                        return CreateTuple( info.Info, true );
                    }
                case "Tuple":
                    {
                        return CreateTuple( info.Info, false );
                    }
                case "Array":
                    {
                        Debug.Assert( info.Info.Kind == TypeReadInfoKind.Array );
                        Debug.Assert( info.Info.SubTypes.Count == 1 );
                        var item = info.Info.SubTypes[0].GetDeserializationDriver();
                        return _cache.GetOrAdd( (item, info.Info.ArrayRank), CreateArray );
                    }
                case "Dictionary": return TryGetDoubleGenericParameter( info.Info, typeof( Deserialization.DDictionary<,> ) );
                case "List": return TryGetSingleGenericParameter( info.Info, typeof( Deserialization.DList<> ) );
                case "Stack": return TryGetSingleGenericParameter( info.Info, typeof( Deserialization.DStack<> ) );
                case "Queue": return TryGetSingleGenericParameter( info.Info, typeof( Deserialization.DQueue<> ) );
            }
            return null;
        }

        private IDeserializationDriver? CreateTuple( ITypeReadInfo info, bool isValueTuple )
        {
            var tA = new IDeserializationDriver[info.SubTypes.Count];
            for( int i = 0; i < tA.Length; ++i )
            {
                tA[i] = info.SubTypes[i].GetDeserializationDriver();
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
            var item = info.SubTypes[0].GetDeserializationDriver();
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
            var item1 = info.SubTypes[0].GetDeserializationDriver();
            var item2 = info.SubTypes[1].GetDeserializationDriver();
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

        IDeserializationDriver CreateEnum( object key )
        {
            var k = ((Type L, IDeserializationDriver U))key;
            var uLocal = k.L.GetEnumUnderlyingType();
            if( uLocal == k.U.ResolvedType )
            {
                var tSame = typeof( Deserialization.DEnum<,> ).MakeGenericType( k.L, k.U.ResolvedType );
                return (IDeserializationDriver)Activator.CreateInstance( tSame, k.U.TypedReader )!;
            }
            // If k.L.IsEnum is false (the local type is NOT an enum), we could do this (it works):
            //
            //   var tDiffOther = typeof( Deserialization.DEnumDiff<,,> ).MakeGenericType( k.L, k.L, k.U.ResolvedType );
            //   return (IDeserializationDriver)Activator.CreateInstance( tDiffOther, k.U.TypedReader )!;
            //
            // However we don't to stay consistent. Mutations must be coherent, if we do this then we should also handle 
            // the opposite (written integral types read as enums).
            // Supporting mapping to different enums makes deserialization hooks and IMutableTypeReadInfo.SetLocalType
            // a simple way to handle enum migrations.
            var tDiff = typeof( Deserialization.DEnumDiff<,,> ).MakeGenericType( k.L, uLocal, k.U.ResolvedType );
            return (IDeserializationDriver)Activator.CreateInstance( tDiff, k.U.TypedReader )!;
        }

    }
}
