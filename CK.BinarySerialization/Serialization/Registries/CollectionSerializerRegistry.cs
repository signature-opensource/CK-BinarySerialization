using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    public class CollectionSerializerRegistry : ISerializerResolver
    {
        readonly ConcurrentDictionary<Type, IUntypedSerializationDriver?> _cache;
        readonly ISerializerResolver _resolver;

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly ISerializerResolver Default = new CollectionSerializerRegistry( BinarySerializer.DefaultResolver );

        public CollectionSerializerRegistry( ISerializerResolver resolver ) 
        {
            _cache = new ConcurrentDictionary<Type, IUntypedSerializationDriver?>();
            _resolver = resolver;
        }

        public ISerializationDriver<T>? TryFindDriver<T>()
        {
            return (ISerializationDriver<T>?)TryFindDriver( typeof( T ) );
        }

        public IUntypedSerializationDriver? TryFindDriver( Type t )
        {
            if( t.ContainsGenericParameters ) return null;
            if( t.IsArray )
            {
                return _cache.GetOrAdd( t, CreateArray );
            }
            return null;
        }

        IUntypedSerializationDriver? CreateArray( Type t )
        {
            var tE = t.GetElementType()!;
            var dItem = _resolver.TryFindDriver( tE );
            if( dItem == null ) return null;    
            var tS = typeof( Serialization.DArrayItem<> ).MakeGenericType( tE );
            return (IUntypedSerializationDriver)Activator.CreateInstance( tS, dItem )!;
        }
    }
}
