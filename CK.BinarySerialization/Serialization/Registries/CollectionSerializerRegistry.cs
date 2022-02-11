using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Handles Array, List, Dictionary, Tuple, ValueTuple, KeyValuePair and other generics.
    /// </summary>
    public class CollectionSerializerRegistry : ISerializerResolver
    {
        readonly ConcurrentDictionary<Type, ISerializationDriver?> _cache;
        readonly ISerializerResolver _resolver;

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly ISerializerResolver Default = new CollectionSerializerRegistry( BinarySerializer.DefaultResolver );

        public CollectionSerializerRegistry( ISerializerResolver resolver ) 
        {
            _cache = new ConcurrentDictionary<Type, ISerializationDriver?>();
            _resolver = resolver;
        }

        /// <inheritdoc />
        public IValueTypeSerializationDriver<T>? TryFindValueTypeDriver<T>() where T : struct
        {
            return (IValueTypeSerializationDriver<T>?)TryFindDriver( typeof( T ) );
        }

        /// <inheritdoc />
        public IReferenceTypeSerializationDriver<T>? TryFindReferenceTypeDriver<T>() where T : class
        {
            return (IReferenceTypeSerializationDriver<T>?)TryFindDriver( typeof( T ) );
        }

        public ISerializationDriver? TryFindDriver( Type t )
        {
            if( t.ContainsGenericParameters ) return null;
            if( t.IsArray )
            {
                return _cache.GetOrAdd( t, CreateArray );
            }
            if( t.IsGenericType )
            {
                var tGen = t.GetGenericTypeDefinition();
                if( tGen == typeof( List<> ) )
                {
                    return _cache.GetOrAdd( t, CreateList );
                }
            }
            return null;
        }

        ISerializationDriver? CreateArray( Type t )
        {
            var tE = t.GetElementType()!;
            var dItem = _resolver.TryFindDriver( tE );
            if( dItem == null ) return null;    
            var tS = typeof( Serialization.DArray<> ).MakeGenericType( tE );
            return (ISerializationDriver)Activator.CreateInstance( tS, dItem.TypedWriter )!;
        }

        ISerializationDriver? CreateList( Type t )
        {
            var tE = t.GetGenericArguments()[0];
            var dItem = _resolver.TryFindDriver( tE );
            if( dItem == null ) return null;    
            var tS = typeof( Serialization.DList<> ).MakeGenericType( tE );
            return (ISerializationDriver)Activator.CreateInstance( tS, dItem.TypedWriter )!;
        }
    }
}
