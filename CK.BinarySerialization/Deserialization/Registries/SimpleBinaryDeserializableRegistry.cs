using CK.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    public class SimpleBinaryDeserializableRegistry : IDeserializerResolver
    {
        static readonly ConcurrentDictionary<Type, object> _cache;

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly IDeserializerResolver Default = new SimpleBinaryDeserializableRegistry();

        SimpleBinaryDeserializableRegistry() { }

        static SimpleBinaryDeserializableRegistry()
        {
            _cache = new ConcurrentDictionary<Type, object>();
        }

        public object? TryFindDriver( TypeReadInfo info )
        {
            // Cache only the driver if the type is a ICKSimpleBinarySerializable.
            if( info.DriverName == "SimpleBinarySerializable" )
            {
                var t = info.ResolveLocalType();
                if( !typeof(ICKSimpleBinarySerializable).IsAssignableFrom(t) )
                {
                    throw new Exception( $"Type '{t}' has been serialized thanks to its ISimpleBinarySerializable implementation but it doesn't support it anymore." );
                }
                return _cache.GetOrAdd( info.ResolveLocalType(), Create );
            }
            return null;
        }

        sealed class SimpleBinaryDeserializableDriver<T> : IDeserializationDriver<T> where T : notnull, ICKSimpleBinarySerializable
        {
            public T ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo )
            {
                return (T)Activator.CreateInstance( typeof(T), r.Reader )!;
            }
        }

        static ITypeSerializationDriver Create( Type t )
        {
            var tS = typeof( SimpleBinaryDeserializableDriver<> ).MakeGenericType( t );
            return (ITypeSerializationDriver)Activator.CreateInstance( tS )!;
        }

    }
}
