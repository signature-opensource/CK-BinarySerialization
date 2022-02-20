using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Typed serialization driver for non nullable instance of a <typeparamref name="T"/>.
    /// </summary>
    public interface IValueTypeNonNullableSerializationDriver<T> : IValueTypeSerializationDriver<T>, INonNullableSerializationDriver where T : struct
    {
        new TypedWriter<T> TypedWriter { get; }  
    }
}
