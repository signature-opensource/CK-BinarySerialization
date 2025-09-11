using CK.Core;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization;

/// <summary>
/// Deserializer for reference type <typeparamref name="T"/> from a <see cref="ICKBinaryReader"/>.
/// The object cannot have references to other objects in the graph.
/// <para>
/// This deserializer handles the value to reference type mutation natively.
/// </para>
/// <para>
/// The default constructor sets <see cref="ReferenceTypeDeserializerBase{T}.IsCached"/> to true. This is fine for basic drivers but as soon as
/// the driver depends on others (like generics drivers), the non default constructor should be used. 
/// </para>
/// </summary>
/// <typeparam name="T">The type to deserialize.</typeparam>
public abstract class SimpleReferenceTypeDeserializer<T> : ReferenceTypeDeserializerBase<T> where T : class
{
    /// <summary>
    /// Initializes a new <see cref="SimpleReferenceTypeDeserializer{T}"/> that states whether it is cached or not.
    /// </summary>
    protected SimpleReferenceTypeDeserializer()
        : base( true )
    {
    }

    /// <summary>
    /// Initializes a new <see cref="SimpleReferenceTypeDeserializer{T}"/> that states whether it is cached or not.
    /// </summary>
    /// <param name="isCached">Whether this deserializer is cached.</param>
    protected SimpleReferenceTypeDeserializer( bool isCached )
        : base( isCached )
    {
    }

    /// <summary>
    /// Calls the protected <see cref="ReadInstance(ICKBinaryReader, ITypeReadInfo)"/> and
    /// handles struct to class migration if needed.
    /// </summary>
    /// <param name="d">The deserializer.</param>
    /// <param name="readInfo">The information.</param>
    /// <returns>The new instance.</returns>
    protected sealed override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
    {
        var o = ReadInstance( d.Reader, readInfo );
        if( !readInfo.IsValueType ) Unsafe.As<BinaryDeserializerImpl>( d ).Track( o );
        return o;
    }

    /// <summary>
    /// Must read a non null instance from the binary reader.
    /// </summary>
    /// <param name="r">The binary reader.</param>
    /// <param name="readInfo">The read type info.</param>
    /// <returns>The new instance.</returns>
    protected abstract T ReadInstance( ICKBinaryReader r, ITypeReadInfo readInfo );
}
