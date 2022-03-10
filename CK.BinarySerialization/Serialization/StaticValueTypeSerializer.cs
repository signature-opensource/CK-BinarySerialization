using System;
using System.Reflection;

namespace CK.BinarySerialization
{

    /// <summary>
    /// Serializer for value type <typeparamref name="T"/> that must expose a static writer method:
    /// <para>
    /// <c>public static void Write( IBinarySerializer s, in T o ) { ... }</c>
    /// </para>
    /// <para>
    /// This is the most efficient driver implementation but it can be used only when writing the type 
    /// doesn't require any states (typically subordinated types drivers).
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class StaticValueTypeSerializer<T> : ISerializationDriverInternal where T : struct
    {
        class ValueTypeNullable : ISerializationDriver
        {
            readonly StaticValueTypeSerializer<T> _serializer;
            readonly UntypedWriter _uWriter;
            readonly TypedWriter<T?> _tWriter;

            public ValueTypeNullable( StaticValueTypeSerializer<T> serializer )
            {
                _serializer = serializer;
                _tWriter = WriteNullable;
                _uWriter = WriteNullableObject;
            }

            public Delegate UntypedWriter => _uWriter;

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
                    _serializer._tWriter( s, o.Value );
                }
                else
                {
                    s.Writer.Write( (byte)SerializationMarker.Null );
                }
            }

            public void WriteNullableObject( IBinarySerializer s, in object? o ) => WriteNullable( s, (T?)o );
        }

        readonly ValueTypeNullable _nullable;
        readonly UntypedWriter _uWriter;
        readonly TypedWriter<T> _tWriter;

        /// <summary>
        /// Initializes a <see cref="StaticValueTypeSerializer{T}"/> where the public static Write method must be 
        /// declared in the specialized type.
        /// </summary>
        public StaticValueTypeSerializer()
        {
            _tWriter = GetTypedWriter( GetType() );
            _uWriter = WriteUntyped;
            _nullable = new ValueTypeNullable( this );
        }

        /// <summary>
        /// Initializes a <see cref="StaticValueTypeSerializer{T}"/> where the public static Write method must be 
        /// declared in the <paramref name="writerHost"/> type.
        /// </summary>
        /// <param name="writerHost"></param>
        public StaticValueTypeSerializer( Type writerHost )
        {
            _tWriter = GetTypedWriter( writerHost );
            _uWriter = WriteUntyped;
            _nullable = new ValueTypeNullable( this );
        }

        static TypedWriter<T> GetTypedWriter( Type writerHost )
        {
            MethodInfo? writer = SharedBinarySerializerContext.GetStaticWriter( writerHost, typeof(T) );
            return (TypedWriter<T>)writer.CreateDelegate( typeof( TypedWriter<T> ) );
        }

        /// <inheritdoc />
        public Delegate UntypedWriter => _uWriter;

        /// <inheritdoc />
        public TypedWriter<T> TypedWriter => _tWriter;

        Delegate ISerializationDriver.TypedWriter => _tWriter;

        void ISerializationDriverInternal.WriteObjectData( IBinarySerializer s, in object o ) => _tWriter( s, (T)o );

        /// <inheritdoc />
        public ISerializationDriver ToNullable => _nullable;

        /// <inheritdoc />
        public ISerializationDriver ToNonNullable => this;

        /// <inheritdoc />
        public abstract string DriverName { get; }

        /// <inheritdoc />
        public abstract int SerializationVersion { get; }

        void WriteUntyped( IBinarySerializer s, in object o ) => _tWriter( s, (T)o );
    }
}
