using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Strongly typed specialization of <see cref="ISerializationDriver"/>.
    /// </summary>
    public interface ISerializationDriver<T> : ISerializationDriver where T : notnull
    {
        /// <summary>
        /// Gets the driver that serializes a nullable <typeparamref name="T"/>.
        /// </summary>
        new INullableSerializationDriver<T> ToNullable { get; }

        /// <summary>
        /// Gets the driver that serializes a non nullable <typeparamref name="T"/>.
        /// </summary>
        new INonNullableSerializationDriver<T> ToNonNullable { get; }
    }
}
