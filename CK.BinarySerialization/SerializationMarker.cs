using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    enum SerializationMarker : byte
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
