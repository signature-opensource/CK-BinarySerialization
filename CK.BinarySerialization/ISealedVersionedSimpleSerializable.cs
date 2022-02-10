using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Basic interface for simple binary serialization of sealed class or value type.
    /// The version must be defined by a <see cref="SerializationVersionAttribute"/> on the type and
    /// is written once.
    /// <para>
    /// A deserialization constructor must be implemented (that accepts a CK.Core.ICKBinaryReader and a int version).
    /// </para>
    /// <para>
    /// Simple serialization means that there is no support for object graph (no reference
    /// management), no support for polymorphism (the exact type must be known).
    /// </para>
    /// </summary>
    public interface ISealedVersionedSimpleSerializable
    {
        void Write( ICKBinaryWriter w );
    }
}
