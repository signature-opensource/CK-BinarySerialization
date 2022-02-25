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

            public void WriteNullable( IBinarySerializer w, in T? o )
            {
                if( o.HasValue )
                {
                    w.Writer.Write( (byte)SerializationMarker.Struct );
                    _serializer.Write( w, o.Value );
                }
                else
                {
                    w.Writer.Write( (byte)SerializationMarker.Null );
                }
            }

            public void WriteNullableObject( IBinarySerializer w, in object? o ) => WriteNullable( w, (T?)o );

        }

        readonly ValueTypeNullable _nullable;
        readonly UntypedWriter _uWriter;
        readonly TypedWriter<T> _tWriter;

        public ValueTypeSerializer()
        {
            _nullable = new ValueTypeNullable( this );
            _tWriter = Write;
            _uWriter = WriteUntyped;
        }

        /// <summary>
        /// Must write the instance data.
        /// </summary>
        /// <param name="r">The binary reader.</param>
        /// <param name="readInfo">The read type info.</param>
        /// <returns>The new instance.</returns>
        internal protected abstract void Write( IBinarySerializer w, in T o );

        public Delegate UntypedWriter => _tWriter;

        public Delegate TypedWriter => _tWriter;

        UntypedWriter ISerializationDriverInternal.NoRefNoNullWriter => _uWriter;

        public ISerializationDriver ToNullable => _nullable;

        public ISerializationDriver ToNonNullable => this;

        public abstract string DriverName { get; }

        public abstract int SerializationVersion { get; }

        void WriteUntyped( IBinarySerializer w, in object o ) => Write( w, (T)o );

    }
}
