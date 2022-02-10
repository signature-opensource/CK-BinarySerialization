namespace CK.BinarySerialization
{
    /// <summary>
    /// Untyped driver that serializes non null instances.
    /// </summary>
    public interface INonNullableSerializationDriver : ISerializationDriver
    {
        /// <summary>
        /// Writes the untyped object.
        /// </summary>
        /// <param name="w">The serializer.</param>
        /// <param name="o">The object instance.</param>
        void WriteObject( IBinarySerializer w, object o );
    }
}
