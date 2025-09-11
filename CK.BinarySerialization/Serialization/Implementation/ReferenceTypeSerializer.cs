using System;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization;

/// <summary>
/// Serializer for reference type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type to deserialize.</typeparam>
public abstract class ReferenceTypeSerializer<T> : ISerializationDriverInternal where T : class
{
    readonly TypedWriter<T> _tWriter;

    /// <summary>
    /// Initializes a new <see cref="ReferenceTypeSerializer{T}"/>.
    /// </summary>
    protected ReferenceTypeSerializer()
    {
        _tWriter = WriteRefOrInstance;
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
    public ISerializationDriver Nullable => this;

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
