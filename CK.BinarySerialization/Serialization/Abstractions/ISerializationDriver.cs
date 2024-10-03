using CK.BinarySerialization.Serialization;
using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization;


/// <summary>
/// Serialization driver that knows how to serialize a null or not null instance 
/// of one or more type.
/// </summary>
public interface ISerializationDriver
{
    /// <summary>
    /// Gets the driver's name that will be serialized.
    /// </summary>
    string DriverName { get; }

    /// <summary>
    /// Gets the serialization version. This driver can directly hard codes its version or
    /// uses the <see cref="SerializationVersionAttribute"/> on the type.
    /// This can be -1 when no version is defined.
    /// </summary>
    int SerializationVersion { get; }

    /// <summary>
    /// Gets the writer <see cref="BinarySerialization.UntypedWriter"/> that accepts a an untyped object that will be down-casted to the actual type.
    /// If this is a non nullable serialization driver, the writer will throw if the 
    /// object to write is null.
    /// </summary>
    Delegate UntypedWriter { get; }

    /// <summary>
    /// Gets a strongly typed <see cref="TypedWriter{T}"/> function for this type and nullability.
    /// If this is a non nullable serialization driver, the writer will throw if the 
    /// object to write is null.
    /// </summary>
    Delegate TypedWriter { get; }

    /// <summary>
    /// Gets the nullable driver.
    /// </summary>
    ISerializationDriver ToNullable { get; }

    /// <summary>
    /// Gets the non nullable driver.
    /// </summary>
    ISerializationDriver ToNonNullable { get; }

    /// <summary>
    /// Gets whether this serialization driver can be cached.
    /// <para>
    /// The huge majority of serialization drivers can be cached at the <see cref="SharedBinarySerializerContext"/>
    /// level since they depend on the serialized (concrete) type. However some of them may rely on stable information
    /// available in the <see cref="BinarySerializerContext.Services"/> (this is the case for IPoco serialization
    /// that uses the PocoDirectory): these ones must use <see cref="SerializationDriverCacheLevel.Context"/>.
    /// </para>
    /// <para>
    /// If the serialization driver relies on transient/unstable information <see cref="SerializationDriverCacheLevel.Never"/> should
    /// be used. (This latter case should be quite exceptional.)
    /// </para>
    /// <para>
    /// A serialization driver that relies on other drivers must combine the levels:
    /// see <see cref="SerializationDriverCacheLevelExtensions.Combine(SerializationDriverCacheLevel, SerializationDriverCacheLevel)"/>.
    /// </para>
    /// </summary>
    SerializationDriverCacheLevel CacheLevel { get; }
}
