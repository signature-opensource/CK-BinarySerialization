using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Handles standard generic types. This registry relies on the <see cref="TypeReadInfo.DriverName"/>
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
        //     - (IDeserializationDriver Item, int Rank) for array (Rank >= 1).
        //     - (IDeserializationDriver Item, Type D) where D is the generic type definition for DList<>, DStack<> or DQueue<>.  
        readonly ConcurrentDictionary<object, IDeserializationDriver> _cache;
        readonly IDeserializerResolver _resolver;

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly IDeserializerResolver Default = new StandardGenericDeserializerRegistry( BinaryDeserializer.DefaultResolver );

        public StandardGenericDeserializerRegistry( IDeserializerResolver resolver ) 
        {
            _cache = new ConcurrentDictionary<object, IDeserializationDriver>();
            _resolver = resolver;
        }

        public IDeserializationDriver? TryFindDriver( TypeReadInfo info )
        {
            switch( info.DriverName )
            {
                case "Enum":
                    {
                        Debug.Assert( info.Kind == TypeReadInfo.TypeKind.Enum && info.ElementTypeReadInfo != null );
                        var uD = info.ElementTypeReadInfo.TryResolveDeserializationDriver();
                        if( uD == null ) return null;
                        var localType = info.TryResolveLocalType();
                        if( localType == null ) return null;
                        return _cache.GetOrAdd( (localType,uD), CreateEnum );
                    }
                case "Array":
                    {
                        Debug.Assert( info.Kind == TypeReadInfo.TypeKind.Array );
                        Debug.Assert( info.ElementTypeReadInfo != null );
                        var item = info.ElementTypeReadInfo.TryResolveDeserializationDriver();
                        if( item == null ) return null;
                        return _cache.GetOrAdd( (item, info.ArrayRank), CreateArray );
                    }
                case "List": return TryGetSingleGenericParameter( info, typeof( Deserialization.DList<> ) );
                case "Stack": return TryGetSingleGenericParameter( info, typeof( Deserialization.DStack<> ) );
                case "Queue": return TryGetSingleGenericParameter( info, typeof( Deserialization.DQueue<> ) );
            }
            return null;
        }

        private IDeserializationDriver? TryGetSingleGenericParameter( TypeReadInfo info, Type tGenD )
        {
            Debug.Assert( info.GenericParameters.Count == 1 );
            var item = info.GenericParameters[0].TryResolveDeserializationDriver();
            if( item == null ) return null;
            var k = (item, tGenD);
            return _cache.GetOrAdd( k, CreateSingleGenericTypeParam );

            static IDeserializationDriver CreateSingleGenericTypeParam( object key )
            {
                var k = ((IDeserializationDriver I, Type D))key;
                var tS = k.D.MakeGenericType( k.I.ResolvedType );
                return (IDeserializationDriver)Activator.CreateInstance( tS, k.I.TypedReader )!;
            }
        }

        IDeserializationDriver CreateArray( object key )
        {
            var k = ((IDeserializationDriver I, int Rank))key;
            if( k.Rank == 1 )
            {
                var tS = typeof( Deserialization.DArray<> ).MakeGenericType( k.I.ResolvedType );
                return (IDeserializationDriver)Activator.CreateInstance( tS, k.I.TypedReader )!;
            }
            else if( k.Rank > 1 )
            {

            }
            throw new NotImplementedException( "Arrays with more than one dimension are not yet supported." );
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
            var tDiff = typeof( Deserialization.DEnumDiff<,,> ).MakeGenericType( k.L, uLocal, k.U.ResolvedType );
            return (IDeserializationDriver)Activator.CreateInstance( tDiff, k.U.TypedReader )!;
        }

    }
}
