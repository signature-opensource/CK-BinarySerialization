namespace CK.BinarySerialization
{

    public interface ITypeSerializationDriver
    {
        /// <summary>
        /// Gets the name that will be serialized.
        /// </summary>
        string DriverName { get; }

        /// <summary>
        /// Gets the serialization version if <see cref="SerializationVersionAttribute"/> is on the type.
        /// This is -1 when no version is defined.
        /// </summary>
        int SerializationVersion { get; }

        /// <summary>
        /// Writes the untyped object's data.
        /// </summary>
        /// <param name="w">The serializer.</param>
        /// <param name="o">The object instance.</param>
        void WriteData( IBinarySerializer w, object o );
    }
}