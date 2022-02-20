using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Typed deserialization driver that knows how to instantiate an instance of a <typeparamref name="T"/>
    /// that can be null and initializes it from a <see cref="IBinaryDeserializer"/>.
    /// </summary>
    public interface IValueTypeNullableSerializationDriver<T> : IValueTypeSerializationDriver<T>, INullableSerializationDriver where T : struct
    {
        new TypedWriter<T?> TypedWriter { get; }
    }
}
