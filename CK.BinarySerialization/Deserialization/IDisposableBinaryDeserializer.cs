using CK.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Disposable deserializer returned by <see cref="BinaryDeserializer.Create(System.IO.Stream, bool, BinaryDeserializerContext)"/>.
    /// </summary>
    public interface IDisposableBinaryDeserializer : IBinaryDeserializer, IDisposable
    {
    }
}
