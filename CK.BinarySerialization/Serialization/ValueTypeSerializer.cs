using System;

namespace CK.BinarySerialization
{

    /// <summary>
    /// Serializer for type <typeparamref name="T"/> that serializes nullable as well as non nullable instances.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class ValueTypeSerializer<T> : ISerializationDriverInternal where T : struct
    {
        sealed class ValueTypeNullable : ISerializationDriver
        {
            readonly ValueTypeSerializer<T> _serializer;
            readonly UntypedWriter _uWriter;
            readonly TypedWriter<T?> _tWriter;

            public ValueTypeNullable( ValueTypeSerializer<T> serializer )
            {
                _serializer = serializer;
                _tWriter = WriteNullable;
                _uWriter = WriteNullableObject;
            }

            public Delegate UntypedWriter => _tWriter;

            public Delegate TypedWriter => _tWriter;

            public string DriverName => _serializer.DriverName;

            public int SerializationVersion => _serializer.SerializationVersion;

            public ISerializationDriver ToNullable => this;

            public ISerializationDriver ToNonNullable => _serializer;

            public void WriteNullable( IBinarySerializer s, in T? o )
            {
                if( o.HasValue )
                {
                    s.Writer.Write( (byte)SerializationMarker.ObjectData );
                    _serializer.Write( s, o.Value );
                }
                else
                {
                    s.Writer.Write( (byte)SerializationMarker.Null );
                }
            }

            public void WriteNullableObject( IBinarySerializer s, in object? o ) => WriteNullable( s, (T?)o );

        }

        readonly ValueTypeNullable _nullable;
        readonly TypedWriter<T> _tWriter;

        /// <summary>
        /// Initializes this serializer.
        /// </summary>
        protected ValueTypeSerializer()
        {
            _nullable = new ValueTypeNullable( this );
            _tWriter = Write;
        }

        /// <summary>
        /// Must write the instance data.
        /// </summary>
        /// <param name="s">The serializer.</param>
        /// <param name="o">The object to write.</param>
        internal protected abstract void Write( IBinarySerializer s, in T o );

        /// <inheritdoc />
        public Delegate UntypedWriter => _tWriter;

        /// <inheritdoc />
        public Delegate TypedWriter => _tWriter;

        void ISerializationDriverInternal.WriteObjectData( IBinarySerializer s, in object o ) => Write( s, (T)o );

        /// <inheritdoc />
        public ISerializationDriver ToNullable => _nullable;

        /// <inheritdoc />
        public ISerializationDriver ToNonNullable => this;

        /// <inheritdoc />
        public abstract string DriverName { get; }

        /// <inheritdoc />
        public abstract int SerializationVersion { get; }

    }
}
