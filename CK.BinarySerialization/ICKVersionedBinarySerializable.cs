using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Interface for versioned binary serialization that uses an externally 
    /// stored or known version number. This should be used only on sealed classes or value types 
    /// (inheritance or any other traits or composite objects will have to share the same version).
    /// <para>
    /// The version must be defined by a <see cref="SerializationVersionAttribute"/> on the type and
    /// should be written once for all instances of the type.
    /// </para>
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
        /// Must write the binary layout only, without the version number that must be handled externally.
        /// This binary layout will be read by a deserialization constructor that takes a <see cref="ICKBinaryReader"/> 
        /// and a int version.
        /// </summary>
        /// <param name="w">The writer.</param>
        void Write( ICKBinaryWriter w );
    }
}
