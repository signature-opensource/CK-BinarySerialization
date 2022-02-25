using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
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
        /// Gets the serialization version if <see cref="SerializationVersionAttribute"/> is on the type.
        /// This is -1 when no version is defined.
        /// </summary>
        int SerializationVersion { get; }

        /// <summary>
        /// Gets the writer function that accepts a an untyped object that will be down-casted to the actual type.
        /// If this is a non nullable serialization driver, the writer will throw if the 
        /// object to write is null.
        /// </summary>
        UntypedWriter UntypedWriter { get; }

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
    }
}
