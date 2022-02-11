using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Typed serialization driver for non nullable instance of a <typeparamref name="T"/>.
    /// </summary>
    public interface IValueTypeNonNullableSerializationDriver<T> : IValueTypeSerializationDriver<T>, INonNullableSerializationDriver where T : struct
    {
        /// <summary>
        /// Writes an instance.
        /// </summary>
        /// <param name="w">The serializer.</param>
        /// <param name="o">The instance.</param>
        void Write( IBinarySerializer w, in T o );

        new TypedWriter<T> TypedWriter { get; }  
    }
}
