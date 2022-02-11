using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Deserializer for type <typeparamref name="T"/> that handles nullable as well as non nullable written instances.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class ReferenceTypeDeserializer<T> : IReferenceTypeNonNullableDeserializationDriver<T> where T : class
    {
        class NullableAdapter : IReferenceTypeNullableDeserializationDriver<T>
        {
            readonly ReferenceTypeDeserializer<T> _deserializer;
            readonly TypedReader<T?> _reader;

            public NullableAdapter( ReferenceTypeDeserializer<T> deserializer )
            {
                _deserializer = deserializer;
                _reader = ReadInstance;
            }

            public IReferenceTypeNullableDeserializationDriver<T> ToNullable => this;

            public IReferenceTypeNonNullableDeserializationDriver<T> ToNonNullable => _deserializer;

            public Type ResolvedType => _deserializer.ResolvedType;

            public Delegate TypedReader => _reader;

            INullableDeserializationDriver IDeserializationDriver.ToNullable => this;

            INonNullableDeserializationDriver IDeserializationDriver.ToNonNullable => _deserializer;

            public object? ReadAsObject( IBinaryDeserializer r, TypeReadInfo readInfo ) => ReadInstance( r, readInfo );

            public T? ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo )
            {
                if( r.Reader.ReadBoolean() )
                {
                    return ReadInstance( r, readInfo );
                }
                return default;
            }

        }

        readonly NullableAdapter _null;
        readonly TypedReader<T> _reader;

        protected ReferenceTypeDeserializer()
        {
            _null = new NullableAdapter( this );
            _reader = ReadInstance;
        }

        /// <summary>
        /// Must read a non null instance from the binary reader.
        /// </summary>
        /// <param name="r">The binary reader.</param>
        /// <param name="readInfo">The read type info.</param>
        /// <returns>The new instance.</returns>
        protected abstract T ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo );

        /// <inheritdoc />
        public Type ResolvedType => typeof( T );

        /// <inheritdoc />
        public Delegate TypedReader => _reader;

        /// <inheritdoc />
        public IReferenceTypeNullableDeserializationDriver<T> ToNullable => _null;

        /// <inheritdoc />
        public IReferenceTypeNonNullableDeserializationDriver<T> ToNonNullable => this;

        INullableDeserializationDriver IDeserializationDriver.ToNullable => _null;

        INonNullableDeserializationDriver IDeserializationDriver.ToNonNullable => this;

        T IReferenceTypeNonNullableDeserializationDriver<T>.ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => ReadInstance( r, readInfo );

        object INonNullableDeserializationDriver.ReadAsObject( IBinaryDeserializer r, TypeReadInfo readInfo ) => ReadInstance( r, readInfo );

    }
}
