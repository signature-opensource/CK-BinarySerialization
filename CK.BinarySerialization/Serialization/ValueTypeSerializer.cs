using System;

namespace CK.BinarySerialization
{

    /// <summary>
    /// Serializer for type <typeparamref name="T"/> that serializes nullable as well as non nullable instances.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class ValueTypeSerializer<T> : IValueTypeNonNullableSerializationDriver<T> where T : struct
    {
        class ValueTypeNullable : IValueTypeNullableSerializationDriver<T>
        {
            readonly ValueTypeSerializer<T> _serializer;
            readonly UntypedWriter _uWriter;
            readonly TypedWriter<T?> _tWriter;

            public ValueTypeNullable( ValueTypeSerializer<T> serializer )
            {
                _serializer = serializer;
                DriverName = _serializer.DriverName + '?';
                Type = typeof( Nullable<> ).MakeGenericType( _serializer.Type );
                _tWriter = WriteNullable;
                _uWriter = WriteNullableObject;
            }

            public UntypedWriter UntypedWriter => _uWriter;

            public TypedWriter<T?> TypedWriter => _tWriter;

            Delegate ISerializationDriver.TypedWriter => _tWriter;

            public IValueTypeNullableSerializationDriver<T> ToNullable => this;

            public IValueTypeNonNullableSerializationDriver<T> ToNonNullable => _serializer;

            public Type Type { get; }

            public string DriverName { get; }

            public int SerializationVersion => _serializer.SerializationVersion;

            INullableSerializationDriver ISerializationDriver.ToNullable => this;

            INonNullableSerializationDriver ISerializationDriver.ToNonNullable => _serializer;

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

        /// <inheritdoc />
        public Type Type => typeof( T );

        public UntypedWriter UntypedWriter => _uWriter;

        public TypedWriter<T> TypedWriter => _tWriter;

        Delegate ISerializationDriver.TypedWriter => _tWriter;

        /// <inheritdoc />
        public IValueTypeNullableSerializationDriver<T> ToNullable => _nullable;

        /// <inheritdoc />
        public IValueTypeNonNullableSerializationDriver<T> ToNonNullable => this;

        INullableSerializationDriver ISerializationDriver.ToNullable => _nullable;

        INonNullableSerializationDriver ISerializationDriver.ToNonNullable => this;

        public abstract string DriverName { get; }

        public abstract int SerializationVersion { get; }

        void IValueTypeNonNullableSerializationDriver<T>.Write( IBinarySerializer w, in T o ) => Write( w, in o );

        void WriteUntyped( IBinarySerializer w, in object o ) => Write( w, (T)o );
        
        void INonNullableSerializationDriver.WriteObject( IBinarySerializer w, in object o ) => Write( w, (T)o );
    }
}
