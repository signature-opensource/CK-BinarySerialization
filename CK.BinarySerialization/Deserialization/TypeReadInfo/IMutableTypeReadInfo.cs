using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Provided to hooks registered by <see cref="SharedBinaryDeserializerContext.AddDeserializationHook"/>.
    /// </summary>
    public interface IMutableTypeReadInfo
    {
        /// <summary>
        /// Gets the externally immutable information that can be 
        /// changed through this interface.
        /// </summary>
        ITypeReadInfo ReadInfo { get; }

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
        /// Can be called multiple times (by different hooks).
        /// </para>
        /// </summary>
        /// <param name="t">The target local type.</param>
        void SetTargetType( Type t );

        /// <summary>
        /// Assigns the deserialization driver that must be used for this <see cref="ReadInfo"/>.
        /// This driver takes complete control of the deserialization.
        /// <para>
        /// Can be called multiple times (by different hooks).
        /// </para>
        /// </summary>
        /// <param name="driver">The driver to use.</param>
        void SetDriver( IDeserializationDriver driver );

        /// <summary>
        /// Sets the deserialization driver name that must be used for this <see cref="ReadInfo"/>.
        /// <para>
        /// Can be called multiple times (by different hooks).
        /// </para>
        /// </summary>
        /// <param name="driverName">The driver name to use.</param>
        void SetDriverName( string driverName );

        /// <summary>
        /// Sets the namespace of the type that must be used for this <see cref="ReadInfo"/>.
        /// <para>
        /// Can be called multiple times (by different hooks).
        /// </para>
        /// </summary>
        /// <param name="typeNamespace">The namespace to use for the type instead of <see cref="ITypeReadInfo.TypeNamespace"/>.</param>
        void SetLocalTypeNamespace( string typeNamespace );

        /// <summary>
        /// Sets the simple assembly name of the type (without version, culture, etc.) that must 
        /// be used for this <see cref="ReadInfo"/>.
        /// <para>
        /// Can be called multiple times (by different hooks).
        /// </para>
        /// </summary>
        /// <param name="assemblyName">The assembly name to use for the type instead of <see cref="ITypeReadInfo.AssemblyName"/>.</param>
        void SetLocalTypeAssemblyName( string assemblyName );

        /// <summary>
        /// Sets the type name that must be used for this <see cref="ReadInfo"/>.
        /// Parent nested simple type name are separated with a '+' and for generic type, it must be suffixed 
        /// with a backtick and the number of generic parameters.
        /// <para>
        /// Can be called multiple times (by different hooks).
        /// </para>
        /// </summary>
        /// <param name="typeName">The type name to use for the type instead of <see cref="ITypeReadInfo.TypeName"/>.</param>
        void SetLocalTypeName( string typeName );

    }
}