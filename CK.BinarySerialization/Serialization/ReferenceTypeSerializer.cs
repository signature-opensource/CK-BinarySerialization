using System;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Serializer for reference type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class ReferenceTypeSerializer<T> : ISerializationDriverInternal where T : class
    {
        sealed class ReferenceTypeNullable : ISerializationDriver
        {
            readonly ReferenceTypeSerializer<T> _serializer;
            //readonly UntypedWriter _uWriter;
            readonly TypedWriter<T?> _tWriter;

            public ReferenceTypeNullable( ReferenceTypeSerializer<T> serializer )
            {
                _serializer = serializer;
                _tWriter = WriteNullable;
                //_uWriter = WriteNullableObject;
            }

            public Delegate UntypedWriter => _tWriter;

            public Delegate TypedWriter => _tWriter;

            public string DriverName => _serializer.DriverName;

            public int SerializationVersion => _serializer.SerializationVersion;

            public ISerializationDriver ToNullable => this;

            public ISerializationDriver ToNonNullable => _serializer;

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

            //void WriteNullableObject( IBinarySerializer w, in object? o ) => WriteNullable( w, (T?)o );
        }

        readonly ReferenceTypeNullable _nullable;
        //readonly UntypedWriter _uWriter;
        readonly UntypedWriter _noRefWriter;
        readonly TypedWriter<T> _tWriter;

        public ReferenceTypeSerializer()
        {
            _noRefWriter = WriteObjectData;
            //_uWriter = WriteUntypedRefOrInstance;
            _tWriter = WriteRefOrInstance;
            _nullable = new ReferenceTypeNullable( this );
        }

        //void WriteUntypedRefOrInstance( IBinarySerializer w, in object o ) => WriteRefOrInstance( w, (T)o );

        void WriteRefOrInstance( IBinarySerializer s, in T o )
        {
            if( Unsafe.As<BinarySerializerImpl>( s ).TrackObject( o ) )
            {
                s.Writer.Write( (byte)SerializationMarker.Object );
                Write( s, o );  
            }
        }

        void WriteObjectData( IBinarySerializer w, in object o ) => Write( w, (T)o );

        /// <summary>
        /// Must write the instance data.
        /// </summary>
        /// <param name="s">The binary serializer.</param>
        /// <param name="o">The instance to write.</param>
        internal protected abstract void Write( IBinarySerializer s, in T o );

        /// <inheritdoc />
        public Delegate UntypedWriter => _tWriter;

        /// <inheritdoc />
        public TypedWriter<T> TypedWriter => _tWriter;

        Delegate ISerializationDriver.TypedWriter => _tWriter;

        UntypedWriter ISerializationDriverInternal.NoRefNoNullWriter => _noRefWriter;

        public ISerializationDriver ToNullable => _nullable;

        public ISerializationDriver ToNonNullable => this;

        public abstract string DriverName { get; }

        public abstract int SerializationVersion { get; }

    }
}
