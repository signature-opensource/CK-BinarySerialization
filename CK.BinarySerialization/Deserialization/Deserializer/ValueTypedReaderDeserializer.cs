using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Direct deserializer for value type <typeparamref name="T"/> bound to a <see cref="TypedReader{T}"/> 
    /// that avoids a useless intermediate relay.
    /// <para>
    /// This deserializer cannot read a previously written reference type: when a type has changed from class
    /// to struct, the <see cref="ValueTypeDeserializerWithRef{T}"/> must be used (as long as previously written 
    /// reference types must still be read).
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public sealed class ValueTypedReaderDeserializer<T> : IDeserializationDriverInternal, IValueTypeNonNullableDeserializationDriver<T> where T : struct
    {
        sealed class NullableAdapter : IValueTypeNullableDeserializationDriver<T>
        {
            readonly ValueTypedReaderDeserializer<T> _deserializer;
            readonly TypedReader<T?> _reader;

            public NullableAdapter( ValueTypedReaderDeserializer<T> deserializer )
            {
                _deserializer = deserializer;
                ResolvedType = typeof( Nullable<> ).MakeGenericType( typeof( T ) );
                _reader = ReadInstance;
            }

            public IValueTypeNullableDeserializationDriver<T> ToNullable => this;

            public IValueTypeNonNullableDeserializationDriver<T> ToNonNullable => _deserializer;

            public Type ResolvedType { get; }

            public Delegate TypedReader => _reader;

            public bool IsCacheable => _deserializer.IsCacheable;

            IDeserializationDriver IDeserializationDriver.ToNullable => this;

            IDeserializationDriver IDeserializationDriver.ToNonNullable => _deserializer;

            public T? ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
            {
                Debug.Assert( readInfo.IsNullable );
                if( d.Reader.ReadBoolean() )
                {
                    return _deserializer._reader( d, readInfo.ToNonNullable );
                }
                return default;
            }

        }

        readonly NullableAdapter _null;
        readonly TypedReader<T> _reader;

        /// <summary>
        /// Initializes a new <see cref="ValueTypedReaderDeserializer{T}"/> bound to a reader function
        /// that states whether it is cached or not.
        /// </summary>
        /// <param name="reader">The reader function.</param>
        /// <param name="isCached">Whether this deserializer is cached.</param>
        public ValueTypedReaderDeserializer( TypedReader<T> reader, bool isCached )
        {
            _null = new NullableAdapter( this );
            _reader = reader;
            IsCacheable = isCached;
        }

        /// <summary>
        /// Initializes a new <see cref="ValueTypedReaderDeserializer{T}"/> from a constructor 
        /// from which a reader function that calls the constructor is derived.
        /// </summary>
        /// <param name="ctor">Deserialization constructor. See <see cref="BinaryDeserializer.Helper.GetTypedReaderConstructor(Type)"/>.</param>
        /// <param name="isCached">Whether this deserializer is cached.</param>
        public ValueTypedReaderDeserializer( ConstructorInfo ctor, bool isCached )
            : this( BinaryDeserializer.Helper.CreateTypedReaderNewDelegate<T>( ctor ), isCached )
        {
        }

        /// <inheritdoc />
        public Type ResolvedType => typeof( T );

        /// <inheritdoc />
        public Delegate TypedReader => _reader;

        /// <inheritdoc />
        public bool IsCacheable { get; }

        /// <inheritdoc />
        public IValueTypeNullableDeserializationDriver<T> ToNullable => _null;

        /// <inheritdoc />
        public IValueTypeNonNullableDeserializationDriver<T> ToNonNullable => this;

        IDeserializationDriver IDeserializationDriver.ToNullable => _null;

        IDeserializationDriver IDeserializationDriver.ToNonNullable => this;

        T IValueTypeNonNullableDeserializationDriver<T>.ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _reader( d, readInfo );

        object IDeserializationDriverInternal.ReadObjectData( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _reader( d, readInfo );

    }
}
