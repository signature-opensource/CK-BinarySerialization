using CK.Core;
using System;
using System.Diagnostics;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Static thread safe singleton factory for <see cref="ICKSimpleBinarySerializable"/> and <see cref="ICKVersionedBinarySerializable"/>
    /// serializers.
    /// <para>
    /// Since this kind on serialization don't need any other resolvers (drivers only depends on the actual type), a singleton
    /// is fine. We could have cached the drivers in a static global concurrent dictionary. However, since using independent 
    /// <see cref="SharedBinarySerializerContext"/> is rather theoretical (in practice, only the <see cref="BinarySerializer.DefaultSharedContext"/>
    /// will be used), introducing another level of cache would be quite pointless.
    /// </para>
    /// </summary>
    public class SimpleBinarySerializableFactory : ISerializerResolver
    {
        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static readonly SimpleBinarySerializableFactory Instance = new SimpleBinarySerializableFactory();

        SimpleBinarySerializableFactory() { }

        /// <summary>
        /// Handles the type if it implements <see cref="ICKSimpleBinarySerializable"/> or <see cref="ICKVersionedBinarySerializable"/>.
        /// </summary>
        /// <param name="t">The type for which a serialization driver must be resolved.</param>
        /// <returns>A new driver or null.</returns>
        public ISerializationDriver? TryFindDriver( Type t )
        {
            try
            {
                if( typeof( ICKVersionedBinarySerializable ).IsAssignableFrom( t ) )
                {
                    if( !t.IsValueType && !t.IsSealed )
                    {
                        Throw.InvalidOperationException( $"Type '{t}' cannot implement {nameof(ICKVersionedBinarySerializable)} interface. It must be a sealed class or a value type." );
                    }
                    return CreateSealed( t );
                }
                if( typeof( ICKSimpleBinarySerializable ).IsAssignableFrom( t ) )
                {
                    return CreateSimple( t );
                }
            }
            catch( System.Reflection.TargetInvocationException ex )
            {
                if( ex.InnerException != null ) throw ex.InnerException;
                throw;
            }
            return null;
        }

        sealed class SimpleBinarySerializableDriverR<T> : ReferenceTypeSerializer<T> where T : class, ICKSimpleBinarySerializable
        {
            public override string DriverName => "SimpleBinarySerializable";

            public override int SerializationVersion => -1;

            protected internal override void Write( IBinarySerializer s, in T o ) => o.Write( s.Writer );
        }

        sealed class SimpleBinarySerializableDriverV<T> : StaticValueTypeSerializer<T> where T : struct, ICKSimpleBinarySerializable
        {
            public override string DriverName => "SimpleBinarySerializable";

            public override int SerializationVersion => -1;

            public static void Write( IBinarySerializer s, in T o ) => o.Write( s.Writer );
        }

        static ISerializationDriver CreateSimple( Type t )
        {
            if( t.IsValueType )
            {
                var tV = typeof( SimpleBinarySerializableDriverV<> ).MakeGenericType( t );
                return (ISerializationDriver)Activator.CreateInstance( tV )!;
            }
            var tR = typeof( SimpleBinarySerializableDriverR<> ).MakeGenericType( t );
            return ((ISerializationDriver)Activator.CreateInstance( tR )!).ToNullable;
        }

        sealed class SealedBinarySerializableDriverR<T> : ReferenceTypeSerializer<T> where T : class, ICKVersionedBinarySerializable
        {
            public override string DriverName => "VersionedBinarySerializable";

            public SealedBinarySerializableDriverR( int version ) => SerializationVersion = version;

            public override int SerializationVersion { get; }

            protected internal override void Write( IBinarySerializer s, in T o ) => o.WriteData( s.Writer );
        }

        sealed class SealedBinarySerializableDriverV<T> : StaticValueTypeSerializer<T> where T : struct, ICKVersionedBinarySerializable
        {
            public override string DriverName => "VersionedBinarySerializable";

            public SealedBinarySerializableDriverV( int version ) => SerializationVersion = version;

            public override int SerializationVersion { get; }

            public static void Write( IBinarySerializer s, in T o ) => o.WriteData( s.Writer );
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
            return ((ISerializationDriver)Activator.CreateInstance( tR, v )!).ToNullable;
        }
    }
}
