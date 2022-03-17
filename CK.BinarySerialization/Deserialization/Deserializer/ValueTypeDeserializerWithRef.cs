using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Deserializer base for value type <typeparamref name="T"/> that can handle previously written instances as classes.
    /// <para>
    /// This deserializer should be used when a type has changed from class
    /// to struct as long as previously written reference types must still be read.
    /// Once only value types can be read, the standard <see cref="ValueTypeDeserializer{T}"/> should be used since it's
    /// more efficient than this one.
    /// </para>
    /// <para>
    /// By design, <see cref="IDeserializationDriver.IsCached"/> is false.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class ValueTypeDeserializerWithRef<T> : IValueTypeDeserializerWithRefInternal, IValueTypeNonNullableDeserializationDriver<T> where T : struct
    {
        sealed class NullableFromRefAdapter : IValueTypeNullableDeserializationDriver<T>
        {
            readonly ValueTypeDeserializerWithRef<T> _deserializer;
            readonly TypedReader<T?> _reader;

            public NullableFromRefAdapter( ValueTypeDeserializerWithRef<T> deserializer )
            {
                _deserializer = deserializer;
                ResolvedType = typeof( Nullable<> ).MakeGenericType( typeof( T ) );
                _reader = ReadInstance;
            }

            public IValueTypeNullableDeserializationDriver<T> ToNullable => this;

            public IValueTypeNonNullableDeserializationDriver<T> ToNonNullable => _deserializer;

            public Type ResolvedType { get; }

            public Delegate TypedReader => _reader;

            public bool IsCached => false;  

            IDeserializationDriver IDeserializationDriver.ToNullable => this;

            IDeserializationDriver IDeserializationDriver.ToNonNullable => _deserializer;

            public T? ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
            {
                Debug.Assert( readInfo.IsNullable );
                SerializationMarker b = (SerializationMarker)d.Reader.ReadByte();
                if( b != SerializationMarker.Null )
                {
                    return readInfo.IsValueType 
                            ? _deserializer.ReadInstance( d, readInfo.ToNonNullable ) 
                            : _deserializer.ReadRefOrInstance( d, readInfo.ToNonNullable, b );
                }
                return default;
            }
        }

        readonly NullableFromRefAdapter _null;
        readonly TypedReader<T> _reader;

        /// <summary>
        /// Initializes a <see cref="ValueTypeDeserializerWithRef{T}"/> that can read 
        /// its data from a written reference type.
        /// </summary>
        protected ValueTypeDeserializerWithRef()
        {
            _null = new NullableFromRefAdapter( this );
            _reader = ReadRefOrInstance;
        }

        T ReadRefOrInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
        {
            if( readInfo.IsValueType ) return ReadInstance( d, readInfo );
            return ReadRefOrInstance( d, readInfo, (SerializationMarker)d.Reader.ReadByte() );
        }

        T ReadRefOrInstance( IBinaryDeserializer d, ITypeReadInfo readInfo, SerializationMarker b )
        {
            Debug.Assert( !readInfo.IsValueType );
            if( b == SerializationMarker.ObjectRef ) return (T)Unsafe.As<BinaryDeserializerImpl>( d ).ReadObjectRef();
            if( b == SerializationMarker.DeferredObject ) return (T)Unsafe.As<BinaryDeserializerImpl>( d ).ReadObjectCore( b, readInfo, this );
            return ReadInstanceAndTrack( d, readInfo );
        }

        T ReadInstanceAndTrack( IBinaryDeserializer d, ITypeReadInfo readInfo )
        {
            var o = ReadInstance( d, readInfo );
            Unsafe.As<BinaryDeserializerImpl>( d ).Track( o );
            return o;
        }

        /// <summary>
        /// Must read a non null instance from the deserializer.
        /// </summary>
        /// <param name="d">The deserializer.</param>
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

        /// <inheritdoc />
        public bool IsCached => false;

        IDeserializationDriver IDeserializationDriver.ToNullable => _null;

        IDeserializationDriver IDeserializationDriver.ToNonNullable => this;

        T IValueTypeNonNullableDeserializationDriver<T>.ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => ReadInstanceAndTrack( d, readInfo );

        object IDeserializationDriverInternal.ReadObjectData( IBinaryDeserializer d, ITypeReadInfo readInfo ) => ReadInstanceAndTrack( d, readInfo );

        object IValueTypeDeserializerWithRefInternal.ReadRawObjectData( IBinaryDeserializer d, ITypeReadInfo readInfo ) => ReadInstance( d, readInfo );
    }
}
