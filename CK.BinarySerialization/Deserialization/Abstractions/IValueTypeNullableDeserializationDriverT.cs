using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Typed deserialization driver that knows how to instantiate an instance of a <typeparamref name="T"/>
    /// that can be null and initializes it from a <see cref="IBinaryDeserializer"/>.
    /// </summary>
    public interface IValueTypeNullableDeserializationDriver<T> : IDeserializationDriver where T : struct
    {
        /// <summary>
        /// Reads null or the data and instantiates a new object.
        /// </summary>
        /// <param name="d">The deserializer.</param>
        /// <param name="readInfo">The type information read.</param>
        /// <returns>The new instance or null.</returns>
        T? ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo );
    }
}
