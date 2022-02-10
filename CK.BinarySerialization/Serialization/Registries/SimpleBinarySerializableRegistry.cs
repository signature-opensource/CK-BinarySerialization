using CK.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Static thread safe registry for <see cref="ICKSimpleBinarySerializable"/> and <see cref="ISealedVersionedSimpleSerializable"/>
    /// serializers.
    /// <para>
    /// Since this kind on serialization don't need any other resolvers (drivers only depends on the actual type), a singleton
    /// cache is fine.
    /// </para>
    /// </summary>
    public class SimpleBinarySerializableRegistry : ISerializerResolver
    {
        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly ISerializerResolver Default = new SimpleBinarySerializableRegistry();

        SimpleBinarySerializableRegistry() { }

        public ISerializationDriver<T>? TryFindDriver<T>()
        {
            return (ISerializationDriver<T>?)TryFindDriver( typeof(T) );
        }

        public IUntypedSerializationDriver? TryFindDriver( Type t )
        {
            // Cache only the driver if the type is a ICKSimpleBinarySerializable or a .
            if( typeof( ICKSimpleBinarySerializable ).IsAssignableFrom( t ) )
            {
                return SharedCache.Serialization.GetOrAdd( t, CreateSimple );
            }
            if( typeof( ISealedVersionedSimpleSerializable ).IsAssignableFrom( t ) )
            {
                if( !t.IsValueType && !t.IsSealed )
                {
                    throw new InvalidOperationException( $"Type '{t}' cannot implement ISealedVersionedSimpleSerializable interface. It must be a sealed class or a value type." );
                }
                return SharedCache.Serialization.GetOrAdd( t, CreateSealed );
            }
            return null;
        }

        sealed class SimpleBinarySerializableDriver<T> : INonNullableSerializationDriver<T> where T : ICKSimpleBinarySerializable
        {
            public string DriverName => "SimpleBinarySerializable";

            public int SerializationVersion => -1;

            public void WriteData( IBinarySerializer w, in T o )
            {
                o.Write( w.Writer );
            }
        }

        static IUntypedSerializationDriver CreateSimple( Type t )
        {
            var tS = typeof( SimpleBinarySerializableDriver<> ).MakeGenericType( t );
            return (IUntypedSerializationDriver)Activator.CreateInstance( tS )!;
        }

        sealed class SealedBinarySerializableDriver<T> : INonNullableSerializationDriver<T> where T : ISealedVersionedSimpleSerializable
        {
            public string DriverName => "SealedVersionBinarySerializable";

            public SealedBinarySerializableDriver( int version )
            {
                SerializationVersion = version;
            }

            public int SerializationVersion { get; }

            public void WriteData( IBinarySerializer w, in T o )
            {
                o.Write( w.Writer );
            }
        }

        static IUntypedSerializationDriver CreateSealed( Type t )
        {
            int v = SerializationVersionAttribute.GetRequiredVersion( t );
            var tS = typeof( SealedBinarySerializableDriver<> ).MakeGenericType( t );
            return (IUntypedSerializationDriver)Activator.CreateInstance( tS, v )!;
        }
    }
}
