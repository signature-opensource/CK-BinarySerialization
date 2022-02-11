namespace CK.BinarySerialization
{
    /// <summary>
    /// Untyped serialization driver that handles nullable.
    /// </summary>
    public interface INullableSerializationDriver : ISerializationDriver
    {
        /// <summary>
        /// Writes the untyped object that may be null.
        /// </summary>
        /// <param name="w">The serializer.</param>
        /// <param name="o">The nullable object instance.</param>
        void WriteNullableObject( IBinarySerializer w, in object? o );
    }
}
