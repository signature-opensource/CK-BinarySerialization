using CK.Core;
using System;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Base reference deserializer for <see cref="ReferenceTypeDeserializer{T}"/> and <see cref="SimpleReferenceTypeDeserializer{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class ReferenceTypeDeserializerBase<T> : IDeserializationDriverInternal where T : class
    {
        class NullableAdapter : IDeserializationDriver
        {
            readonly ReferenceTypeDeserializerBase<T> _deserializer;
            readonly TypedReader<T?> _reader;

            public NullableAdapter( ReferenceTypeDeserializerBase<T> deserializer )
            {
                _deserializer = deserializer;
                _reader = ReadInstance;
            }

            public Type ResolvedType => _deserializer.ResolvedType;

            public Delegate TypedReader => _reader;

            IDeserializationDriver IDeserializationDriver.ToNullable => this;

            IDeserializationDriver IDeserializationDriver.ToNonNullable => _deserializer;

            public T? ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
            {
                var b = d.Reader.ReadByte();
                if( b != (byte)SerializationMarker.Null )
                {
                    return _deserializer.ReadRefOrInstance( d, readInfo, b );
                }
                return default;
            }

        }

        readonly NullableAdapter _null;
        readonly TypedReader<T> _reader;

        private protected ReferenceTypeDeserializerBase()
        {
            _null = new NullableAdapter( this );
            _reader = ReadRefOrInstance;
        }

        T ReadRefOrInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
        {
            var b = readInfo.IsValueType ? (byte)SerializationMarker.Object : d.Reader.ReadByte();
            return ReadRefOrInstance( d, readInfo, b );
        }

        T ReadRefOrInstance( IBinaryDeserializer d, ITypeReadInfo readInfo, byte b )
        {
            if( b == (byte)SerializationMarker.ObjectRef ) return (T)Unsafe.As<BinaryDeserializerImpl>( d ).ReadObjectRef();
            return ReadInstance( d, readInfo );
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

        IDeserializationDriver IDeserializationDriver.ToNullable => _null;

        IDeserializationDriver IDeserializationDriver.ToNonNullable => this;

        object IDeserializationDriverInternal.ReadObjectData( IBinaryDeserializer d, ITypeReadInfo readInfo ) => ReadInstance( d, readInfo );

    }
}
