using CK.Core;
using System;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization;

/// <summary>
/// Disposable serializer returned by <see cref="BinarySerializer.Create(System.IO.Stream, BinarySerializerContext)"/>
/// </summary>
public interface IDisposableBinarySerializer : IBinarySerializer, IDisposable
{
}
