using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Typed serialization driver for non nullable instance of a <typeparamref name="T"/>.
    /// </summary>
    public interface INonNullableSerializationDriver<T> : ISerializationDriver<T>, INonNullableSerializationDriver where T : notnull
    {
        /// <summary>
        /// Writes an instance.
        /// </summary>
        /// <param name="w">The serializer.</param>
        /// <param name="o">The instance.</param>
        void Write( IBinarySerializer w, in T o );
    }
}
