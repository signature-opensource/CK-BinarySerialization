using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Serializer for type <typeparamref name="T"/> that serializes nullable as well as non nullable instances.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class ReferenceTypeSerializer<T> : IReferenceTypeNonNullableSerializationDriver<T> where T : class
    {
        class ReferenceTypeNullable : IReferenceTypeNullableSerializationDriver<T>
        {
            readonly ReferenceTypeSerializer<T> _serializer;
            readonly UntypedWriter _uWriter;
            readonly TypedWriter<T?> _tWriter;

            public ReferenceTypeNullable( ReferenceTypeSerializer<T> serializer )
            {
                _serializer = serializer;
                DriverName = _serializer.DriverName + '?';
                _tWriter = WriteNullable;
                _uWriter = WriteNullableObject;
            }

            public UntypedWriter UntypedWriter => _uWriter;

            public TypedWriter<T?> TypedWriter => _tWriter;

            Delegate ISerializationDriver.TypedWriter => _tWriter;

            public IReferenceTypeNullableSerializationDriver<T> ToNullable => this;

            public IReferenceTypeNonNullableSerializationDriver<T> ToNonNullable => _serializer;

            public Type Type => _serializer.Type;

            public string DriverName { get; }

            public int SerializationVersion => _serializer.SerializationVersion;

            INullableSerializationDriver ISerializationDriver.ToNullable => this;

            INonNullableSerializationDriver ISerializationDriver.ToNonNullable => _serializer;

            public void WriteNullable( IBinarySerializer w, in T? o )
            {
                if( o != null )
                {
                    w.Writer.Write( (byte)SerializationMarker.Object );
                    _serializer.Write( w, o );
                }
                else
                {
                    w.Writer.Write( (byte)SerializationMarker.Null );
                }
            }

            public void WriteNullableObject( IBinarySerializer w, in object? o ) => WriteNullable( w, (T?)o );
        }

        readonly ReferenceTypeNullable _nullable;
        readonly UntypedWriter _uWriter;
        readonly TypedWriter<T> _tWriter;

        public ReferenceTypeSerializer()
        {
            _nullable = new ReferenceTypeNullable( this );
            _uWriter = WriteUntyped;
            _tWriter = Write;
        }

        /// <summary>
        /// Must write the instance data.
        /// </summary>
        /// <param name="r">The binary reader.</param>
        /// <param name="readInfo">The read type info.</param>
        /// <returns>The new instance.</returns>
        internal protected abstract void Write( IBinarySerializer w, in T o );

        /// <inheritdoc />
        public Type Type => typeof( T );

        public UntypedWriter UntypedWriter => _uWriter;

        public TypedWriter<T> TypedWriter => _tWriter;

        Delegate ISerializationDriver.TypedWriter => _tWriter;

        /// <inheritdoc />
        public IReferenceTypeNullableSerializationDriver<T> ToNullable => _nullable;

        /// <inheritdoc />
        public IReferenceTypeNonNullableSerializationDriver<T> ToNonNullable => this;

        INullableSerializationDriver ISerializationDriver.ToNullable => _nullable;

        INonNullableSerializationDriver ISerializationDriver.ToNonNullable => this;

        public abstract string DriverName { get; }

        public abstract int SerializationVersion { get; }

        void IReferenceTypeNonNullableSerializationDriver<T>.Write( IBinarySerializer w, in T o ) => Write( w, in o );

        void WriteUntyped( IBinarySerializer w, in object o ) => Write( w, (T)o );

        void INonNullableSerializationDriver.WriteObject( IBinarySerializer w, in object o) => Write( w, (T)o );
    }
}
