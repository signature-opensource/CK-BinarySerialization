using CK.BinarySerialization.Serialization;
using System;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization;

/// <summary>
/// Serializer for reference type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type to deserialize.</typeparam>
public abstract class ReferenceTypeSerializer<T> : ISerializationDriverInternal where T : class
{
    sealed class ReferenceTypeNullable : ISerializationDriver
    {
        readonly ReferenceTypeSerializer<T> _serializer;
        readonly TypedWriter<T?> _tWriter;

        public ReferenceTypeNullable( ReferenceTypeSerializer<T> serializer )
        {
            _serializer = serializer;
            _tWriter = WriteNullable;
        }

        public Delegate UntypedWriter => _tWriter;

        public Delegate TypedWriter => _tWriter;

        public string DriverName => _serializer.DriverName;

        public int SerializationVersion => _serializer.SerializationVersion;

        public ISerializationDriver Nullable => this;

        public ISerializationDriver NonNullable => _serializer;

        public SerializationDriverCacheLevel CacheLevel => _serializer.CacheLevel;

        public void WriteNullable( IBinarySerializer s, in T? o )
        {
            if( o != null )
            {
                _serializer.WriteRefOrInstance( s, o );
            }
            else
            {
                s.Writer.Write( (byte)SerializationMarker.Null );
            }
        }
    }

    readonly ReferenceTypeNullable _nullable;
    readonly TypedWriter<T> _tWriter;

    /// <summary>
    /// Initializes a new <see cref="ReferenceTypeSerializer{T}"/>.
    /// </summary>
    protected ReferenceTypeSerializer()
    {
        _tWriter = WriteRefOrInstance;
        _nullable = new ReferenceTypeNullable( this );
    }

    void WriteRefOrInstance( IBinarySerializer s, in T o )
    {
        if( Unsafe.As<BinarySerializerImpl>( s ).TrackObject( o ) )
        {
            s.Writer.Write( (byte)SerializationMarker.ObjectData );
            Write( s, o );
        }
    }

    void ISerializationDriverInternal.WriteObjectData( IBinarySerializer s, in object o ) => Write( s, (T)o );

    /// <summary>
    /// Must write the instance data.
    /// </summary>
    /// <param name="s">The binary serializer.</param>
    /// <param name="o">The instance to write.</param>
    internal protected abstract void Write( IBinarySerializer s, in T o );

    /// <inheritdoc />
    public Delegate UntypedWriter => _tWriter;

    /// <inheritdoc />
    public Delegate TypedWriter => _tWriter;

    /// <inheritdoc />
    public ISerializationDriver Nullable => _nullable;

    /// <inheritdoc />
    public ISerializationDriver NonNullable => this;

    /// <inheritdoc />
    public abstract string DriverName { get; }

    /// <inheritdoc />
    public abstract int SerializationVersion { get; }

    /// <summary>
    /// Defaults to <see cref="SerializationDriverCacheLevel.SharedContext"/>.
    /// </summary>
    public virtual SerializationDriverCacheLevel CacheLevel => SerializationDriverCacheLevel.SharedContext;

}
