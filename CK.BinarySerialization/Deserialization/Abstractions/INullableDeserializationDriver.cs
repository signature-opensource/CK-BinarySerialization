namespace CK.BinarySerialization
{
    /// <summary>
    /// Untyped deserialization driver that knows how to instantiate an instance of a <see cref="IDeserializationDriver.ResolvedType"/> 
    /// that can be null and initializes it from a <see cref="IBinaryDeserializer"/>.
    /// </summary>
    public interface INullableDeserializationDriver : IDeserializationDriver
    {
        /// <summary>
        /// Reads a null or the data and instantiates a new object.
        /// </summary>
        /// <param name="r">The deserializer.</param>
        /// <param name="readInfo">The type information read.</param>
        /// <returns>The new instance or null.</returns>
        object? ReadAsObject( IBinaryDeserializer r, TypeReadInfo readInfo );
    }
}
