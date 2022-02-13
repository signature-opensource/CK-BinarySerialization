using CK.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Static thread safe singleton for <see cref="ICKSimpleBinarySerializable"/> and <see cref="ISealedVersionedSimpleSerializable"/>
    /// serializers.
    /// <para>
    /// Since this kind on serialization don't need any other resolvers (drivers only depends on the actual type), a singleton
    /// cache is fine.
    /// </para>
    /// </summary>
    public class SimpleBinarySerializableRegistry : ISerializerResolver
    {
        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static readonly SimpleBinarySerializableRegistry Instance = new SimpleBinarySerializableRegistry();

        SimpleBinarySerializableRegistry() { }

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
            // Cache only the driver if the type is a ICKSimpleBinarySerializable or a .
            if( typeof( ICKSimpleBinarySerializable ).IsAssignableFrom( t ) )
            {
                return InternalSharedCache.Serialization.GetOrAdd( t, CreateSimple );
            }
            if( typeof( ISealedVersionedSimpleSerializable ).IsAssignableFrom( t ) )
            {
                if( !t.IsValueType && !t.IsSealed )
                {
                    throw new InvalidOperationException( $"Type '{t}' cannot implement ISealedVersionedSimpleSerializable interface. It must be a sealed class or a value type." );
                }
                return InternalSharedCache.Serialization.GetOrAdd( t, CreateSealed );
            }
            return null;
        }

        sealed class SimpleBinarySerializableDriverR<T> : ReferenceTypeSerializer<T> where T : class, ICKSimpleBinarySerializable
        {
            public override string DriverName => "SimpleBinarySerializable";

            public override int SerializationVersion => -1;

            protected internal override void Write( IBinarySerializer w, in T o ) => o.Write( w.Writer );
        }

        sealed class SimpleBinarySerializableDriverV<T> : ValueTypeSerializer<T> where T : struct, ICKSimpleBinarySerializable
        {
            public override string DriverName => "SimpleBinarySerializable";

            public override int SerializationVersion => -1;

            protected internal override void Write( IBinarySerializer w, in T o ) => o.Write( w.Writer );
        }

        static ISerializationDriver CreateSimple( Type t )
        {
            if( t.IsValueType )
            {
                var tV = typeof( SimpleBinarySerializableDriverV<> ).MakeGenericType( t );
                return (ISerializationDriver)Activator.CreateInstance( tV )!;
            }
            var tR = typeof( SimpleBinarySerializableDriverR<> ).MakeGenericType( t );
            return (ISerializationDriver)Activator.CreateInstance( tR )!;
        }

        sealed class SealedBinarySerializableDriverR<T> : ReferenceTypeSerializer<T> where T : class, ISealedVersionedSimpleSerializable
        {
            public override string DriverName => "SealedVersionBinarySerializable";

            public SealedBinarySerializableDriverR( int version ) => SerializationVersion = version;

            public override int SerializationVersion { get; }

            protected internal override void Write( IBinarySerializer w, in T o ) => o.Write( w.Writer );
        }

        sealed class SealedBinarySerializableDriverV<T> : ValueTypeSerializer<T> where T : struct, ISealedVersionedSimpleSerializable
        {
            public override string DriverName => "SealedVersionBinarySerializable";

            public SealedBinarySerializableDriverV( int version ) => SerializationVersion = version;

            public override int SerializationVersion { get; }

            protected internal override void Write( IBinarySerializer w, in T o ) => o.Write( w.Writer );
        }

        static ISerializationDriver CreateSealed( Type t )
        {
            int v = SerializationVersionAttribute.GetRequiredVersion( t );
            if( t.IsValueType )
            {
                var tV = typeof( SealedBinarySerializableDriverV<> ).MakeGenericType( t );
                return (ISerializationDriver)Activator.CreateInstance( tV, v )!;
            }
            var tR = typeof( SealedBinarySerializableDriverR<> ).MakeGenericType( t );
            return (ISerializationDriver)Activator.CreateInstance( tR, v )!;
        }
    }
}
