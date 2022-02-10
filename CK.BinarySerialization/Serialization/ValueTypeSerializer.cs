using System;

namespace CK.BinarySerialization
{


    /// <summary>
    /// Serializer for type <typeparamref name="T"/> that serializes nullable as well as non nullable instances.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class ValueTypeSerializer<T> : INonNullableSerializationDriver<T> where T : struct
    {
        class Nullable<T> : INullableSerializationDriver<T> where T : struct
        {
            readonly ValueTypeSerializer<T> _serializer;

            public Nullable( ValueTypeSerializer<T> serializer )
            {
                _serializer = serializer;
                DriverName = _serializer.DriverName + '?';
            }

            public INullableSerializationDriver<T> ToNullable => this;

            public INonNullableSerializationDriver<T> ToNonNullable => _serializer;

            public Type Type => _serializer.Type;

            public string DriverName { get; }

            public int SerializationVersion => _serializer.SerializationVersion;

            INullableSerializationDriver ISerializationDriver.ToNullable => this;

            INonNullableSerializationDriver ISerializationDriver.ToNonNullable => _serializer;

            public void WriteNullable( IBinarySerializer w, in T? o )
            {
                if( o.HasValue )
                {
                    w.Writer.Write( (byte)SerializationMarker.Struct );
                    Write( w, o.Value );
                }
                else
                {
                    w.Writer.Write( (byte)SerializationMarker.Null );
                }
            }

            public void WriteNullableObject( IBinarySerializer w, object? o )
            {
                ((INullableSerializationDriver)_serializer).WriteNullableObject( w, o );
            }
        }

        readonly Nullable _nullable;

        public ValueTypeSerializer()
        {
            _nullable = new Nullable( this );
        }

        /// <summary>
        /// Must write the instance data.
        /// </summary>
        /// <param name="r">The binary reader.</param>
        /// <param name="readInfo">The read type info.</param>
        /// <returns>The new instance.</returns>
        protected abstract void Write( IBinarySerializer w, in T o );

        /// <inheritdoc />
        public Type Type => typeof( T );

        /// <inheritdoc />
        public INullableSerializationDriver<T> ToNullable => _nullable;

        /// <inheritdoc />
        public INonNullableSerializationDriver<T> ToNonNullable => this;

        INullableSerializationDriver ISerializationDriver.ToNullable => _nullable;

        INonNullableSerializationDriver ISerializationDriver.ToNonNullable => this;

        public abstract string DriverName { get; }

        public abstract int SerializationVersion { get; }

        void INonNullableSerializationDriver<T>.Write( IBinarySerializer w, in T o ) => Write( w, o );

        void INonNullableSerializationDriver.WriteObject(IBinarySerializer w, object o) => Write( w, (T)o );
    }
}
