using System;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Serializer for reference type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class ReferenceTypeSerializer<T> : INonNullableSerializationDriverInternal, IReferenceTypeNonNullableSerializationDriver<T> where T : class
    {
        sealed class ReferenceTypeNullable : IReferenceTypeNullableSerializationDriver<T>
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

            public bool IsNullable => true;

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
                    _serializer.WriteRefOrInstance( w, o );
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
        readonly UntypedWriter _noRefWriter;
        readonly TypedWriter<T> _tWriter;

        public ReferenceTypeSerializer()
        {
            _noRefWriter = WriteObjectData;
            _uWriter = WriteUntypedRefOrInstance;
            _tWriter = WriteRefOrInstance;
            _nullable = new ReferenceTypeNullable( this );
        }

        void WriteUntypedRefOrInstance( IBinarySerializer w, in object o ) => WriteRefOrInstance( w, (T)o );

        void WriteRefOrInstance( IBinarySerializer s, in T o )
        {
            if( Unsafe.As<BinarySerializerImpl>( s ).TrackObject( o ) )
            {
                s.Writer.Write( (byte)SerializationMarker.Object );
                Write( s, o );  
            }
        }

        /// <summary>
        /// Must write the instance data.
        /// </summary>
        /// <param name="s">The binary serializer.</param>
        /// <param name="o">The instance to write.</param>
        internal protected abstract void Write( IBinarySerializer s, in T o );

        /// <inheritdoc />
        public Type Type => typeof( T );

        /// <inheritdoc />
        public bool IsNullable => false;

        /// <inheritdoc />
        public UntypedWriter UntypedWriter => _uWriter;

        /// <inheritdoc />
        public TypedWriter<T> TypedWriter => _tWriter;

        Delegate ISerializationDriver.TypedWriter => _tWriter;

        UntypedWriter INonNullableSerializationDriverInternal.NoRefNoNullWriter => _noRefWriter;


        /// <inheritdoc />
        public IReferenceTypeNullableSerializationDriver<T> ToNullable => _nullable;

        /// <inheritdoc />
        public IReferenceTypeNonNullableSerializationDriver<T> ToNonNullable => this;

        INullableSerializationDriver ISerializationDriver.ToNullable => _nullable;

        INonNullableSerializationDriver ISerializationDriver.ToNonNullable => this;

        public abstract string DriverName { get; }

        public abstract int SerializationVersion { get; }

        void WriteObjectData( IBinarySerializer w, in object o ) => Write( w, (T)o );

        //void INonNullableSerializationDriver.WriteObject( IBinarySerializer w, in object o) => Write( w, (T)o );
    }
}
