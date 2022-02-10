using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Typed deserialization driver that knows how to instantiate an instance of a <typeparamref name="T"/> 
    /// that cannot be null and initializes it from a <see cref="IBinaryDeserializer"/>.
    /// </summary>
    public interface INonNullableDeserializationDriver<out T> : IDeserializationDriver<T>, INonNullableDeserializationDriver where T : notnull
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
