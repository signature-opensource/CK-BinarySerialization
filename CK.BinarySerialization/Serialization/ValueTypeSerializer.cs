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
                    s.Writer.Write( (byte)SerializationMarker.Struct );
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

        public ValueTypeSerializer()
        {
            _nullable = new ValueTypeNullable( this );
            _tWriter = Write;
        }

        /// <summary>
        /// Must write the instance data.
        /// </summary>
        /// <param name="r">The binary reader.</param>
        /// <param name="readInfo">The read type info.</param>
        /// <returns>The new instance.</returns>
        internal protected abstract void Write( IBinarySerializer s, in T o );

        public Delegate UntypedWriter => _tWriter;

        public Delegate TypedWriter => _tWriter;

        void ISerializationDriverInternal.WriteObjectData( IBinarySerializer s, in object o ) => Write( s, (T)o );

        public ISerializationDriver ToNullable => _nullable;

        public ISerializationDriver ToNonNullable => this;

        public abstract string DriverName { get; }

        public abstract int SerializationVersion { get; }

    }
}
