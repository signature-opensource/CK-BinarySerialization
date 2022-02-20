using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Categories the <see cref="ITypeReadInfo"/>.
    /// </summary>
    public enum TypeReadInfoKind
    {
        /// <summary>
        /// Regular reference or value type. Instances may be deserialized.
        /// </summary>
        Regular,

        /// <summary>
        /// Enumeration. Instances may be deserialized.
        /// </summary>
        Enum,

        /// <summary>
        /// Array, potentially with multiple dimensions. Instances may be deserialized.
        /// </summary>
        Array,

        /// <summary>
        /// "Generic" array (see <see cref="Type.ContainsGenericParameters"/> is true).
        /// Instances cannot be deserialized.
        /// <see cref="ITypeReadInfo.ResolveLocalType()"/> returns the system type <see cref="System.Array"/>.
        /// </summary>
        OpenArray,

        /// <summary>
        /// <see cref="Type.IsPointer"/> type.
        /// Instances cannot be deserialized.
        /// </summary>
        Pointer,

        /// <summary>
        /// <see cref="Type.IsByRef"/> type.
        /// Instances cannot be deserialized.
        /// </summary>
        Ref,

        /// <summary>
        /// Generic closed type. Instances may be deserialized.
        /// </summary>
        Generic,

        /// <summary>
        /// Open generic type. Instances cannot be deserialized.
        /// </summary>
        OpenGeneric
    }
}
