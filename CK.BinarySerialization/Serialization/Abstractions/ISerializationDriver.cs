using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Serialization driver that knows how to serialize a null or not null instance 
    /// of its <see cref="Type"/> thanks to its 2 drivers.
    /// </summary>
    public interface ISerializationDriver
    {
        /// <summary>
        /// Gets the actual type (nullable or not) that this drivers is able to serialize.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets whether this driver is a <see cref="INullableSerializationDriver"/> or a <see cref="INonNullableSerializationDriver"/>.
        /// </summary>
        bool IsNullable { get; }

        /// <summary>
        /// Gets the driver's name that will be serialized.
        /// </summary>
        string DriverName { get; }

        /// <summary>
        /// Gets the serialization version if <see cref="SerializationVersionAttribute"/> is on the type.
        /// This is -1 when no version is defined.
        /// </summary>
        int SerializationVersion { get; }

        /// <summary>
        /// Gets the writer function that can write an instance of this type as a nullable object.
        /// If this is a <see cref="INonNullableSerializationDriver"/>, the writer will throw if the 
        /// object to write is null.
        /// </summary>
        UntypedWriter UntypedWriter { get; }

        /// <summary>
        /// Gets a strongly typed <see cref="TypedWriter{T}"/> function for this <see cref="Type"/> and nullability.
        /// </summary>
        Delegate TypedWriter { get; }

        /// <summary>
        /// Gets the nullable driver.
        /// </summary>
        INullableSerializationDriver ToNullable { get; }

        /// <summary>
        /// Gets the non nullable driver.
        /// </summary>
        INonNullableSerializationDriver ToNonNullable { get; }

    }
}
