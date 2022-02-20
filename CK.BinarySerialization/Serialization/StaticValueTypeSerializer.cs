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
    public abstract class StaticValueTypeSerializer<T> : IValueTypeNonNullableSerializationDriver<T> where T : struct
    {
        class ValueTypeNullable : IValueTypeNullableSerializationDriver<T>
        {
            readonly StaticValueTypeSerializer<T> _serializer;
            readonly UntypedWriter _uWriter;
            readonly TypedWriter<T?> _tWriter;

            public ValueTypeNullable( StaticValueTypeSerializer<T> serializer )
            {
                _serializer = serializer;
                DriverName = _serializer.DriverName + '?';
                Type = typeof( Nullable<> ).MakeGenericType( _serializer.Type );
                _tWriter = WriteNullable;
                _uWriter = WriteNullableObject;
            }

            public bool IsNullable => true;

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
                    _serializer._tWriter( w, o.Value );
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
        public Type Type => typeof( T );

        /// <inheritdoc />
        public bool IsNullable => false;

        /// <inheritdoc />
        public UntypedWriter UntypedWriter => _uWriter;

        /// <inheritdoc />
        public TypedWriter<T> TypedWriter => _tWriter;

        Delegate ISerializationDriver.TypedWriter => _tWriter;

        /// <inheritdoc />
        public IValueTypeNullableSerializationDriver<T> ToNullable => _nullable;

        /// <inheritdoc />
        public IValueTypeNonNullableSerializationDriver<T> ToNonNullable => this;

        INullableSerializationDriver ISerializationDriver.ToNullable => _nullable;

        INonNullableSerializationDriver ISerializationDriver.ToNonNullable => this;

        /// <inheritdoc />
        public abstract string DriverName { get; }

        /// <inheritdoc />
        public abstract int SerializationVersion { get; }

        void WriteUntyped( IBinarySerializer w, in object o ) => _tWriter( w, (T)o );
        
        void INonNullableSerializationDriver.WriteObject( IBinarySerializer w, in object o ) => _tWriter( w, (T)o );
    }
}
