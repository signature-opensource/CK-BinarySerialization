using System;
using System.Collections.Generic;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Provided to hooks registered by <see cref="SharedBinaryDeserializerContext.AddDeserializationHook"/>.
    /// </summary>
    public interface IMutableTypeReadInfo
    {
        /// <summary>
        /// Exposes the <see cref="WrittenInfo"/> view of the written type.
        /// </summary>
        public interface IWrittenInfo
        {
            /// <summary>
            /// Gets the kind of the written type.
            /// </summary>
            TypeReadInfoKind Kind { get; }

            /// <summary>
            /// Gets the whether the written type is a value type.
            /// </summary>
            bool IsValueType { get; }

            /// <summary>
            /// Gets whether the written type is sealed: ValueTypes and sealed classes are sealed.
            /// </summary>
            bool IsSealed { get; }

            /// <summary>
            /// Gets the rank of the array (the number of dimensions of a multidimensional array).
            /// This is 0 if this written type is not an array.
            /// </summary>
            int ArrayRank { get; }

            /// <summary>
            /// Gets the base type information if any (roots Object and ValueType are skipped).
            /// This base type is non nullable.
            /// </summary>
            ITypeReadInfo? BaseTypeReadInfo { get; }

            /// <summary>
            /// Gets the serialization's driver name that has been used to write instances of this type.
            /// <para>
            /// Null if no serialization's driver was resolved for the type.
            /// This is totally possible since a type written by <see cref="IBinarySerializer.WriteTypeInfo(Type, bool?)"/> is not 
            /// necessarily serializable and this is often the case for base types of a type that is itself serializable
            /// (like <see cref="TypeReadInfoKind.OpenGeneric"/> for instance).
            /// </para>
            /// </summary>
            string? DriverName { get; }

            /// <summary>
            /// Gets the serialization version. -1 when no version is defined.
            /// </summary>
            int Version { get; }

            /// <summary>
            /// Gets the type informations for the generic parameters if any or
            /// the element type information if this is an array, pointer or reference
            /// or the underlying type for an Enum.
            /// </summary>
            IReadOnlyList<ITypeReadInfo> SubTypes { get; }

            /// <summary>
            /// Gets the simple assembly name of the written type (without version, culture, etc.).
            /// </summary>
            string AssemblyName { get; }

            /// <summary>
            /// Gets the simple name or nested name of the written type (parent nested simple type name are separated with a '+').
            /// For generic type, it is suffixed with a backtick and the number of generic parameters.
            /// </summary>
            string TypeName { get; }

            /// <summary>
            /// Gets the namespace of the written type.
            /// </summary>
            string TypeNamespace { get; }
        }

        /// <summary>
        /// Gets the immutable type information that has been written.
        /// </summary>
        IWrittenInfo WrittenInfo { get; }

        /// <summary>
        /// Gets the current driver (set by <see cref="SetDriver(IDeserializationDriver)"/>).
        /// </summary>
        IDeserializationDriver? CurentDriver { get; }

        /// <summary>
        /// Gets the current driver name (set by <see cref="SetDriverName(string)"/>).
        /// </summary>
        string? CurentDriverName { get; }

        /// <summary>
        /// Gets the current target type (set by <see cref="SetTargetType(Type)"/>).
        /// </summary>
        Type? CurentTargetType { get; }

        /// <summary>
        /// Gets the <see cref="IWrittenInfo.AssemblyName"/> or the one set by <see cref="SetLocalTypeAssemblyName(string)"/>.
        /// </summary>
        string CurentAssemblyName { get; }

        /// <summary>
        /// Gets the <see cref="IWrittenInfo.TypeNamespace"/> or the one set by <see cref="SetLocalTypeNamespace(string)"/>.
        /// </summary>
        string CurentTypeNamespace { get; }

        /// <summary>
        /// Gets the <see cref="IWrittenInfo.TypeName"/> or the one set by <see cref="SetLocalTypeName(string)"/>.
        /// </summary>
        string CurentTypeName { get; }

        /// <summary>
        /// Sets the local type that will be resolved.
        /// <para>
        /// This type should be the same as the written one (typically renamed and/or moved 
        /// to a new namespace or assembly) or the deserialization driver should be able
        /// to deserialize this type from the written data.
        /// </para>
        /// <para>
        /// For incompatible types, <see cref="SetDriver(IDeserializationDriver)"/> or <see cref="SetDriverName(string)"/> should be used.
        /// </para>
        /// <para>
        /// May be called multiple times (by different hooks) but this may not be a good idea that different hooks impact the same type.
        /// </para>
        /// </summary>
        /// <param name="t">The target local type.</param>
        void SetTargetType( Type t );

        /// <summary>
        /// Assigns the deserialization driver that must be used for this <see cref="WrittenInfo"/>.
        /// This driver takes complete control of the deserialization.
        /// <para>
        /// May be called multiple times (by different hooks) but this may not be a good idea that different hooks impact the same type.
        /// </para>
        /// </summary>
        /// <param name="driver">The driver to use.</param>
        void SetDriver( IDeserializationDriver driver );

        /// <summary>
        /// Sets the deserialization driver name that must be used for this <see cref="WrittenInfo"/>.
        /// <para>
        /// May be called multiple times (by different hooks) but this may not be a good idea that different hooks impact the same type.
        /// </para>
        /// </summary>
        /// <param name="driverName">The driver name to use.</param>
        void SetDriverName( string driverName );

        /// <summary>
        /// Sets the namespace of the type that must be used for this <see cref="WrittenInfo"/>.
        /// <para>
        /// May be called multiple times (by different hooks) but this may not be a good idea that different hooks impact the same type.
        /// </para>
        /// </summary>
        /// <param name="typeNamespace">The namespace to use for the type instead of <see cref="ITypeReadInfo.TypeNamespace"/>.</param>
        void SetLocalTypeNamespace( string typeNamespace );

        /// <summary>
        /// Sets the simple assembly name of the type (without version, culture, etc.) that must 
        /// be used for this <see cref="WrittenInfo"/>.
        /// <para>
        /// May be called multiple times (by different hooks) but this may not be a good idea that different hooks impact the same type.
        /// </para>
        /// </summary>
        /// <param name="assemblyName">The assembly name to use for the type instead of <see cref="ITypeReadInfo.AssemblyName"/>.</param>
        void SetLocalTypeAssemblyName( string assemblyName );

        /// <summary>
        /// Sets the type name that must be used for this <see cref="WrittenInfo"/>.
        /// Parent nested simple type name are separated with a '+' and for generic type, it must be suffixed 
        /// with a backtick and the number of generic parameters.
        /// <para>
        /// May be called multiple times (by different hooks) but this may not be a good idea that different hooks impact the same type.
        /// </para>
        /// </summary>
        /// <param name="typeName">The type name to use for the type instead of <see cref="ITypeReadInfo.TypeName"/>.</param>
        void SetLocalTypeName( string typeName );

    }
}
