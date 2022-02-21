using System;
using System.Collections.Generic;

namespace CK.BinarySerialization
{

    /// <summary>
    /// Immutable neutral description of a Type that has been written.
    /// </summary>
    public interface ITypeReadInfo
    {
        /// <summary>
        /// Gets whether this type information describes a nullable type.
        /// Reference or value type are handles uniformly: the <see cref="Nullable{T}"/> doesn't
        /// appear: type informations here are the ones of the non nullable type except for the 
        /// <see cref="TryResolveLocalType()"/> or <see cref="ResolveLocalType()"/> that synthesize
        /// a <see cref="Nullable{T}"/> if this is a nullable value type. 
        /// </summary>
        bool IsNullable { get; }
        
        /// <summary>
        /// Gets the non nullable type info. This object if <see cref="IsNullable"/> is false.
        /// </summary>
        ITypeReadInfo ToNonNullable { get; }

        /// <summary>
        /// Gets the kind of this type.
        /// </summary>
        TypeReadInfoKind Kind { get; }

        /// <summary>
        /// Gets the rank of the array (the number of dimensions of a multidimensional array) 
        /// if this is an array.
        /// </summary>
        int ArrayRank { get; }

        /// <summary>
        /// Gets the base type information if any (root object and ValueType are skipped).
        /// This base type is non nullable.
        /// </summary>
        ITypeReadInfo? BaseTypeReadInfo { get; }

        /// <summary>
        /// Gets the serialization's driver name that has been resolved and potentially 
        /// used to write instance of this type.
        /// <para>
        /// Null if no serialization's driver was resolved for the type.
        /// This is totally possible since a type written by <see cref="IBinarySerializer.WriteTypeInfo(Type)"/> is not 
        /// necessarily serializable and this is often the case for base types of a type that is itself serializable
        /// (like <see cref="TypeReadInfoKind.OpenGeneric"/> for instance).
        /// </para>
        /// </summary>
        string? DriverName { get; }

        /// <summary>
        /// Gets the serialization version. -1 when no version is defined.
        /// </summary>
        int SerializationVersion { get; }

        /// <summary>
        /// Gets the type informations for the generic parameters if any or
        /// the element type information if this is an array, pointer or reference
        /// or the underlying type for an Enum.
        /// </summary>
        IReadOnlyList<ITypeReadInfo> SubTypes { get; }
        
        /// <summary>
        /// Gets the simple assembly name of the type (without version, culture, etc.).
        /// </summary>
        string AssemblyName { get; }

        /// <summary>
        /// Gets the simple name or nested name of the type (parent nested simple type name are separated with a '+').
        /// For generic type, it is suffixed with a backtick and the number of generic parameters.
        /// </summary>
        string TypeName { get; }
        
        /// <summary>
        /// Gets the namespace of the type.
        /// </summary>
        string TypeNamespace { get; }

        /// <summary>
        /// Tries to resolve the local type.
        /// <para>
        /// Note that <see cref="TypeReadInfoKind.OpenArray"/> is bound to the system typeof( <see cref="Array"/> ).
        /// </para>
        /// </summary>
        /// <returns>The local type if it can be resolved, null otherwise.</returns>
        Type? TryResolveLocalType();

        /// <summary>
        /// Resolves the local type or throws a <see cref="TypeLoadException"/>.
        /// </summary>
        /// <returns>The local type.</returns>
        Type ResolveLocalType();

        /// <summary>
        /// Gets the type's path from the first base that is not Object nor ValueType
        /// up to and including this one.
        /// Base types are non nullable.
        /// </summary>
        IReadOnlyList<ITypeReadInfo> TypePath { get; }

        /// <summary>
        /// Gets whether a deserialization driver has been resolved.
        /// </summary>
        bool HasResolvedDeserializationDriver { get; }

        /// <summary>
        /// Gets the deserialization driver. If the type cannot be 
        /// locally resolved or a driver cannot be resolved, an exception is thrown.
        /// <para>
        /// This should be called only when attempting to instantiate a driver or during 
        /// the serialization process itself.
        /// </para>
        /// </summary>
        IDeserializationDriver GetDeserializationDriver();
    }
}