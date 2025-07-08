namespace CK.BinarySerialization;

interface ISerializationDriverInternal : ISerializationDriver
{
    void WriteObjectData( IBinarySerializer s, in object o );
}
