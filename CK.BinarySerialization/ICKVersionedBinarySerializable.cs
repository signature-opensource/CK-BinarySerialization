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
    /// A deserialization constructor must be implemented (that accepts a <see cref="ICKBinaryReader"/> and a int version).
    /// </para>
    /// <para>
    /// This is for "simple object" serialization where "simple" means that there is no support for object graph (no reference
    /// management).
    /// </para>
    /// </summary>
    public interface ICKVersionedBinarySerializable
    {
        /// <summary>
        /// Must write the binary layout that will be read by a deserialization 
        /// constructor that takes a <see cref="ICKBinaryReader"/> and a int version.
        /// </summary>
        /// <param name="w">The writer.</param>
        void Write( ICKBinaryWriter w );
    }
}
