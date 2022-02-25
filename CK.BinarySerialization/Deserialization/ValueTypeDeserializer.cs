using System;
using System.Diagnostics;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Deserializer base for value type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class ValueTypeDeserializer<T> : IDeserializationDriverInternal, IValueTypeNonNullableDeserializationDriver<T> where T : struct
    {
        class NullableAdapter : IValueTypeNullableDeserializationDriver<T>
        {
            readonly ValueTypeDeserializer<T> _deserializer;
            readonly TypedReader<T?> _reader;

            public NullableAdapter( ValueTypeDeserializer<T> deserializer )
            {
                _deserializer = deserializer;
                ResolvedType = typeof( Nullable<> ).MakeGenericType( typeof( T ) );
                _reader = ReadInstance;
            }

            public IValueTypeNullableDeserializationDriver<T> ToNullable => this;

            public IValueTypeNonNullableDeserializationDriver<T> ToNonNullable => _deserializer;

            public Type ResolvedType { get; }

            public Delegate TypedReader => _reader;

            IDeserializationDriver IDeserializationDriver.ToNullable => this;

            IDeserializationDriver IDeserializationDriver.ToNonNullable => _deserializer;

            public object? ReadAsObject( IBinaryDeserializer d, ITypeReadInfo readInfo ) => ReadInstance( d, readInfo );

            public T? ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
            {
                Debug.Assert( readInfo.IsNullable );
                if( d.Reader.ReadBoolean() )
                {
                    return _deserializer.ReadInstance( d, readInfo.ToNonNullable );
                }
                return default;
            }

        }

        readonly NullableAdapter _null;
        readonly TypedReader<T> _reader;

        protected ValueTypeDeserializer()
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
        protected abstract T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo );

        /// <inheritdoc />
        public Type ResolvedType => typeof( T );

        /// <inheritdoc />
        public Delegate TypedReader => _reader;

        /// <inheritdoc />
        public IValueTypeNullableDeserializationDriver<T> ToNullable => _null;

        /// <inheritdoc />
        public IValueTypeNonNullableDeserializationDriver<T> ToNonNullable => this;

        IDeserializationDriver IDeserializationDriver.ToNullable => _null;

        IDeserializationDriver IDeserializationDriver.ToNonNullable => this;

        T IValueTypeNonNullableDeserializationDriver<T>.ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => ReadInstance( d, readInfo );

        object IDeserializationDriverInternal.ReadObjectData( IBinaryDeserializer d, ITypeReadInfo readInfo ) => ReadInstance( d, readInfo );

    }
}
