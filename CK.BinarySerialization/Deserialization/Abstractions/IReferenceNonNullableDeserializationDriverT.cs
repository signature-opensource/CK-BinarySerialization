using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Typed deserialization driver that knows how to instantiate an instance of a value type <typeparamref name="T"/>.
    /// </summary>
    public interface IReferenceTypeNonNullableDeserializationDriver<out T> : IDeserializationDriver, INonNullableDeserializationDriver where T : class
    {
        /// <summary>
        /// Reads the data and instantiates a new object.
        /// </summary>
        /// <param name="r">The deserializer.</param>
        /// <param name="readInfo">The type information read.</param>
        /// <returns>The new instance.</returns>
        T ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo );
    }
}
