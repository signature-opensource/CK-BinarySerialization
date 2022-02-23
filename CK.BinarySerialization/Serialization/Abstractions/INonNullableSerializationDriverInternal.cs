using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    interface INonNullableSerializationDriverInternal : ISerializationDriver
    {
        UntypedWriter NoRefNoNullWriter { get; }
    }
}
