using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Categories the <see cref="ITypeReadInfo"/>.
    /// </summary>
    public enum TypeReadInfoKind
    {
        /// <summary>
        /// Missing, unknown or invalid type .
        /// </summary>
        None,

        /// <summary>
        /// Value type. Instances may be deserialized.
        /// </summary>
        ValueType,

        /// <summary>
        /// Generic value type. Instances may be deserialized.
        /// </summary>
        GenericValueType,

        /// <summary>
        /// Sealed class. Instances may be deserialized.
        /// </summary>
        SealedClass,

        /// <summary>
        /// Generic sealed class. Instances may be deserialized.
        /// </summary>
        GenericSealedClass,

        /// <summary>
        /// Regular, non sealed, class. Instances may be deserialized.
        /// </summary>
        Class,

        /// <summary>
        /// Regular, non sealed, generic class. Instances may be deserialized.
        /// </summary>
        GenericClass,

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
        /// Open generic type. Instances cannot be deserialized.
        /// </summary>
        OpenGeneric
    }
}
