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
        // Associates the resolved item driver and the rank to the array driver.
        readonly ConcurrentDictionary<Key, IDeserializationDriver> _cache;
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
            _cache = new ConcurrentDictionary<Key, IDeserializationDriver>();
            _resolver = resolver;
        }

        public IDeserializationDriver? TryFindDriver( TypeReadInfo info )
        {
            if( info.DriverName == "ArrayItem" )
            {
                Debug.Assert( info.ElementTypeReadInfo != null );
                var item = _resolver.TryFindDriver( info.ElementTypeReadInfo );
                if( item == null ) return null;
                var k = new Key( item, info.ArrayRank );
                return _cache.GetOrAdd( k, CreateArray );
            }
            return null;
        }

        IDeserializationDriver CreateArray( Key k )
        {
            if( k.Rank == 1 )
            {
                var tS = typeof( Deserialization.DArrayItem<> ).MakeGenericType( k.Item.ResolvedType );
                return (IDeserializationDriver)Activator.CreateInstance( tS, k.Item )!;
            }
            throw new NotImplementedException( "Arrays with more than one dimension are not yet supported." );
        }
    }
}
