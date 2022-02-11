using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Strongly typed specialization of <see cref="ISerializationDriver"/> for value types.
    /// <para>
    /// Unfortunately reference and value types cannot be handled by a common generic. See https://gist.github.com/olivier-spinelli/43087e4db9d379fe7936935a23266d98
    /// </para>
    /// </summary>
    public interface IValueTypeSerializationDriver<T> : ISerializationDriver where T : struct
    {
        /// <summary>
        /// Gets the driver that serializes a nullable <typeparamref name="T"/>.
        /// </summary>
        new IValueTypeNullableSerializationDriver<T> ToNullable { get; }

        /// <summary>
        /// Gets the driver that serializes a non nullable <typeparamref name="T"/>.
        /// </summary>
        new IValueTypeNonNullableSerializationDriver<T> ToNonNullable { get; }
    }
}
