using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Optional interface that exposes a <see cref="IsDestroyed"/> property that can be implemented 
    /// by reference types that have a "alive" semantics (they may be <see cref="IDisposable"/>).
    /// <para>
    /// <see cref="IBinarySerializer.OnDestroyedObject"/> event is raised whenever a destroyed object
    /// is written: this supports tracking of "dead" objects in serialized graphs.
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
}
