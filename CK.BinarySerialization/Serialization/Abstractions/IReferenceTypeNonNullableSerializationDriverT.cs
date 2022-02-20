using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Typed serialization driver for non nullable instance of a <typeparamref name="T"/>.
    /// </summary>
    public interface IReferenceTypeNonNullableSerializationDriver<T> : IReferenceTypeSerializationDriver<T>, INonNullableSerializationDriver where T : class
    {
        new TypedWriter<T> TypedWriter { get; }
    }
}
