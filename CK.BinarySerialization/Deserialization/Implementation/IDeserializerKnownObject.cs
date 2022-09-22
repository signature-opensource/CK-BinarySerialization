using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Associates a unique string to a known object.
    /// <para>
    /// New entries can be registered during serialization: if the instance is shared
    /// (like the <see cref="SharedDeserializerKnownObject.Default"/>), the implementation must be thread safe.
    /// </para>
    /// </summary>
    public interface IDeserializerKnownObject
    {
        /// <summary>
        /// Tries to get a known object instance from its key.
        /// </summary>
        /// <param name="instanceKey">The instance key.</param>
        /// <returns>The known instance or null if not known.</returns>
        object? GetKnownObject( string instanceKey );

        /// <summary>
        /// Associates a key to a known objects.
        /// The key must not already be associated to another instance (otherwise an <see cref="InvalidOperationException"/> is thrown) 
        /// but the instance can perfectly be already associated to another key.
        /// </summary>
        /// <param name="o">The object instance to register.</param>
        /// <param name="key">A key for this instance.</param>
        void RegisterKnownKey( string key, object o );

        /// <summary>
        /// Associates multiple key to instance mappings.
        /// A key must not already be associated to another instance (otherwise an <see cref="InvalidOperationException"/> is thrown) 
        /// but the same instance can perfectly have multiple keys.
        /// </summary>
        /// <param name="mapping">One or more mapping.</param>
        void RegisterKnownKey( params (string key, object o)[] mapping );

    }
}
