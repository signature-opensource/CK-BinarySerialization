namespace CK.BinarySerialization;

enum SerializationMarker : byte
{
    Null,
    ObjectData,
    ObjectRef,
    Type,
    DeferredObject,
    EmptyObject,
    KnownObject,
}
