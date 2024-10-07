using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization;

/// <summary>
/// Associates a unique string to a unique object.
/// <para>
/// New entries can be registered during serialization: if the instance is shared
/// (like the <see cref="SharedSerializerKnownObject.Default"/>), the implementation must be thread safe.
/// </para>
/// </summary>
public interface ISerializerKnownObject
{
    /// <summary>
    /// Gets a unique key that identifies a specific object instance.
    /// If not null, the returned string is enough to restore the instance.
    /// </summary>
    /// <param name="o">The object that may be a known object.</param>
    /// <returns>A non null string that identifies the object if it's known, null otherwise.</returns>
    string? GetKnownObjectKey( object o );

    /// <summary>
    /// Associates a known object instance to its key.
    /// The key must not already be associated to another instance and the instance must not
    /// already be associated to another key otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    /// <param name="o">The object instance to register.</param>
    /// <param name="key">The unique key for this instance.</param>
    void RegisterKnownObject( object o, string key );

    /// <summary>
    /// Associates multiple known object instances to their respective keys.
    /// A key must not already be associated to another instance and an instance must not
    /// already be associated to another key otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    /// <param name="association">The object instance to key association to register.</param>
    void RegisterKnownObject( params (object o, string key)[] association );

}
