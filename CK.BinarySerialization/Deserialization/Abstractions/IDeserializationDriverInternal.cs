namespace CK.BinarySerialization
{
    /// <summary>
    /// </summary>
    interface IDeserializationDriverInternal : IDeserializationDriver
    {
        /// <summary>
        /// Symmetric of the <see cref="ISerializationDriverInternal.WriteObjectData(IBinarySerializer, in object)"/>.
        /// </summary>
        /// <param name="d">The deserializer.</param>
        /// <param name="readInfo">The type information read.</param>
        /// <returns>The new instance.</returns>
        object ReadObjectData( IBinaryDeserializer d, ITypeReadInfo readInfo );
    }
}
