using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    interface ISerializationDriverInternal : ISerializationDriver
    {
        void WriteObjectData( IBinarySerializer w, in object o );
    }
}
