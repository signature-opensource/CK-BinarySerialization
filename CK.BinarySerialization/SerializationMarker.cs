using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
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
}
