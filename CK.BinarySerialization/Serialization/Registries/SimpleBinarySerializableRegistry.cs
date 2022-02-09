using CK.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    public class SimpleBinarySerializableRegistry : ISerializerResolver
    {
        static readonly ConcurrentDictionary<Type, ITypeSerializationDriver> _cache;

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly ISerializerResolver Default = new SimpleBinarySerializableRegistry();

        SimpleBinarySerializableRegistry() { }

        static SimpleBinarySerializableRegistry()
        {
            _cache = new ConcurrentDictionary<Type, ITypeSerializationDriver>();
        }

        public ITypeSerializationDriver<T>? TryFindDriver<T>() where T : notnull
        {
            return (ITypeSerializationDriver<T>?)TryFindDriver( typeof(T) );
        }

        public ITypeSerializationDriver? TryFindDriver( Type t )
        {
            // Cache only the driver if the type is a ICKSimpleBinarySerializable.
            if( typeof( ICKSimpleBinarySerializable ).IsAssignableFrom( t ) )
            {
                return _cache.GetOrAdd( t, Create );
            }
            return null;
        }

        sealed class SimpleBinarySerializableDriver<T> : ITypeSerializationDriver<T> where T : ICKSimpleBinarySerializable
        {
            public string DriverName => "SimpleBinarySerializable";

            public int SerializationVersion => -1;

            public void WriteData( IBinarySerializer w, in T o )
            {
                o.Write( w.Writer );
            }
        }

        static ITypeSerializationDriver Create( Type t )
        {
            var tS = typeof( SimpleBinarySerializableDriver<> ).MakeGenericType( t );
            return (ITypeSerializationDriver)Activator.CreateInstance( tS )!;
        }
    }
}
