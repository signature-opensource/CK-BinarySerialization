using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    interface ISerializationDriverInternal : ISerializationDriver
    {
        UntypedWriter NoRefNoNullWriter { get; }
    }
}
