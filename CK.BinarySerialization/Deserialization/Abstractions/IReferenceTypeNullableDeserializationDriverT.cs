using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Typed deserialization driver for nullable reference type <typeparamref name="T"/>
    /// </summary>
    public interface IReferenceTypeNullableDeserializationDriver<out T> : INullableDeserializationDriver where T : class
    {
        /// <summary>
        /// Reads null or the data and instantiates a new object.
        /// </summary>
        /// <param name="r">The deserializer.</param>
        /// <param name="readInfo">The type information read.</param>
        /// <returns>The new instance or null.</returns>
        T? ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo );
    }
}
