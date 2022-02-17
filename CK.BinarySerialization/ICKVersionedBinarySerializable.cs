using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Interface for versioned binary serialization of sealed class or value type.
    /// The version must be defined by a <see cref="SerializationVersionAttribute"/> on the type and
    /// is written once.
    /// <para>
    /// A deserialization constructor must be implemented (that accepts a CK.Core.ICKBinaryReader and a int version).
    /// </para>
    /// <para>
    /// This is for "simple" serialization where "Simple" means that there is no support for object graph (no reference
    /// management).
    /// </para>
    /// </summary>
    public interface ICKVersionedBinarySerializable
    {
        void Write( ICKBinaryWriter w );
    }
}
