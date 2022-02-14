using CK.Core;
using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Deserializer for reference type <typeparamref name="T"/> from a <see cref="ICKBinaryReader"/>.
    /// The object cannot have references to other objects in the graph.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class SimpleReferenceTypeDeserializer<T> : INonNullableDeserializationDriver where T : class
    {
        class NullableAdapter : INullableDeserializationDriver
        {
            readonly SimpleReferenceTypeDeserializer<T> _deserializer;
            readonly TypedReader<T?> _reader;

            public NullableAdapter( SimpleReferenceTypeDeserializer<T> deserializer )
            {
                _deserializer = deserializer;
                _reader = ReadInstance;
            }

            public Type ResolvedType => _deserializer.ResolvedType;

            public Delegate TypedReader => _reader;

            INullableDeserializationDriver IDeserializationDriver.ToNullable => this;

            INonNullableDeserializationDriver IDeserializationDriver.ToNonNullable => _deserializer;

            public object? ReadAsObject( IBinaryDeserializer r, TypeReadInfo readInfo ) => ReadInstance( r, readInfo );

            public T? ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo )
            {
                if( r.Reader.ReadBoolean() )
                {
                    return _deserializer.ReadInstance( r.Reader, readInfo );
                }
                return default;
            }

        }

        readonly NullableAdapter _null;
        readonly TypedReader<T> _reader;

        protected SimpleReferenceTypeDeserializer()
        {
            _null = new NullableAdapter( this );
            _reader = ReadInstance;
        }

        T ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => ReadInstance( r.Reader, readInfo );

        /// <summary>
        /// Must read a non null instance from the binary reader.
        /// </summary>
        /// <param name="r">The binary reader.</param>
        /// <param name="readInfo">The read type info.</param>
        /// <returns>The new instance.</returns>
        protected abstract T ReadInstance( ICKBinaryReader r, TypeReadInfo readInfo );

        /// <inheritdoc />
        public Type ResolvedType => typeof( T );

        /// <inheritdoc />
        public Delegate TypedReader => _reader;

        INullableDeserializationDriver IDeserializationDriver.ToNullable => _null;

        INonNullableDeserializationDriver IDeserializationDriver.ToNonNullable => this;

        object INonNullableDeserializationDriver.ReadAsObject( IBinaryDeserializer r, TypeReadInfo readInfo ) => ReadInstance( r.Reader, readInfo );

    }
}
