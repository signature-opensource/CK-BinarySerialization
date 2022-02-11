using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    public enum SerializationMarker : byte
    {
        Null,
        Object,
        Struct,
        ObjectRef,
        Type,
        DeferredObject,
        EmptyObject,
        KnownObject,
    }
}
