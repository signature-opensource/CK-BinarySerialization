using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CK.BinarySerialization
{
    public class CollectionDeserializerRegistry : IDeserializerResolver
    {
        // Associates the resolved item driver and the rank to the array driver or
        // a fake rank for other generic with only one item:
        //  - 0 for List<>.
        readonly ConcurrentDictionary<Key, IDeserializationDriver> _singleItemcache;
        readonly IDeserializerResolver _resolver;

        readonly struct Key : IEquatable<Key>
        {
            public readonly IDeserializationDriver Item;
            public readonly int Rank;
            readonly int _hash;

            public Key( IDeserializationDriver item, int rank )
            {
                Item = item;
                Rank = rank;
                _hash = item.GetHashCode() + rank;
            }

            public override int GetHashCode() => _hash;

            public override bool Equals( object? obj ) => obj is Key key && Equals( key );

            public bool Equals( [AllowNull] Key other ) => Item == other.Item && Rank == other.Rank;
        }

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly IDeserializerResolver Default = new CollectionDeserializerRegistry( BinaryDeserializer.DefaultResolver );

        public CollectionDeserializerRegistry( IDeserializerResolver resolver ) 
        {
            _singleItemcache = new ConcurrentDictionary<Key, IDeserializationDriver>();
            _resolver = resolver;
        }

        public IDeserializationDriver? TryFindDriver( TypeReadInfo info )
        {
            if( info.DriverName == "Array" )
            {
                Debug.Assert( info.ElementTypeReadInfo != null );
                var item = info.ElementTypeReadInfo.TryGetDeserializationDriver( _resolver );
                if( item == null ) return null;
                var k = new Key( item, info.ArrayRank );
                return _singleItemcache.GetOrAdd( k, CreateArray );
            }
            if( info.DriverName == "List" )
            {
                Debug.Assert( info.GenericParameters.Count == 1 );
                var item = info.GenericParameters[0].TryGetDeserializationDriver( _resolver );
                if( item == null ) return null;
                var k = new Key( item, 0 );
                return _singleItemcache.GetOrAdd( k, CreateList );
            }
            return null;
        }

        IDeserializationDriver CreateArray( Key k )
        {
            if( k.Rank == 1 )
            {
                var tS = typeof( Deserialization.DArray<> ).MakeGenericType( k.Item.ResolvedType );
                return (IDeserializationDriver)Activator.CreateInstance( tS, k.Item.TypedReader )!;
            }
            throw new NotImplementedException( "Arrays with more than one dimension are not yet supported." );
        }

        IDeserializationDriver CreateList( Key k )
        {
            Debug.Assert( k.Rank == 0 );
            var tS = typeof( Deserialization.DList<> ).MakeGenericType( k.Item.ResolvedType );
            return (IDeserializationDriver)Activator.CreateInstance( tS, k.Item.TypedReader )!;
        }
    }
}
