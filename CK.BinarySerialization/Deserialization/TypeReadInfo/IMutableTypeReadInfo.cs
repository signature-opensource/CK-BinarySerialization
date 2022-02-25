using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Exposed by <see cref="IBinaryDeserializer.OnTypeReadInfo"/> event.
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
        /// This type should be the same as the written once (typically renamed and/or moved 
        /// to a new namespace or assembly) or the deserialization driver should be able
        /// to deserialize this type from the written data.
        /// </para>
        /// <para>
        /// For incompatible types, <see cref="SetDriver(IDeserializationDriver)"/> should be used.
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

    }
}