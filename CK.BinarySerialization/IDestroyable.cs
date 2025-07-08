using System;

namespace CK.BinarySerialization;

/// <summary>
/// Optional interface that exposes a <see cref="IsDestroyed"/> property that can be implemented 
/// by reference types that have a "alive" semantics (they may be <see cref="IDisposable"/> but this 
/// is not required).
/// <para>
/// <see cref="IBinarySerializer.OnDestroyedObject"/> event is raised whenever a destroyed object
/// is written: this supports tracking of "dead" objects in serialized graphs.
/// </para>
/// <para>
/// When used with "sliced serializable", this must be implemented at the root of the serializable
/// hierarchy and automatically skips calls to specialized Write methods and deserialization constructors.
/// </para>
/// <para>
/// Only reference types are supported: implementing this interface on value type is ignored.
/// </para>
/// </summary>
public interface IDestroyable
{
    /// <summary>
    /// Gets whether this object has been disposed.
    /// </summary>
    bool IsDestroyed { get; }
}
