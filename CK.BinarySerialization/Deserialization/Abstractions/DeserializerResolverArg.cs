using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{

    /// <summary>
    /// Encapsulates a <see cref="ITypeReadInfo"/> that is not null, not a nullable type,
    /// has a driver name and a valid resolved concrete local type and has not yet a 
    /// resolved <see cref="ISerializationDriver"/>.
    /// </summary>
    public readonly ref struct DeserializerResolverArg
    {
        /// <summary>
        /// Gets the non nullable <see cref="ITypeReadInfo"/>.
        /// </summary>
        public readonly ITypeReadInfo ReadInfo;

        /// <summary>
        /// Gets the local type.
        /// </summary>
        public readonly Type TargetType;

        /// <summary>
        /// Gets the driver name.
        /// </summary>
        public string DriverName => ReadInfo.DriverName!;

        /// <summary>
        /// Gets the shared context. 
        /// This is used only to detect mismatch of resolution context.
        /// </summary>
        public readonly SharedBinaryDeserializerContext Context;

        /// <summary>
        /// True if the <see cref="TargetType"/> is the same as the <see cref="ITypeReadInfo.TryResolveLocalType()"/>
        /// and <see cref="ITypeReadInfo.IsDirtyInfo"/> is false.
        /// <para>
        /// Whether the resolved driver is eventually cached (<see cref="IDeserializationDriver.IsCached"/>) is up to
        /// the resolvers.
        /// </para>
        /// </summary>
        public readonly bool IsPossibleNominalDeserialization;

        /// <summary>
        /// Initializes a new <see cref="DeserializerResolverArg"/>.
        /// </summary>
        /// <param name="info">The type info for which a deserialization driver must be resolved.</param>
        /// <param name="context">The shared context is used only to detect mismatch of resolution context.</param>
        /// <param name="targetType">Optional target local type. When not null, overrides <see cref="ITypeReadInfo.TargetType"/>.</param>
        public DeserializerResolverArg( ITypeReadInfo info, SharedBinaryDeserializerContext context, Type? targetType = null )
        {
            if( info == null ) throw new ArgumentNullException( nameof( info ) );
            if( info.IsNullable ) throw new ArgumentException( "Type must not be nullable.", nameof( info ) );
            if( info.DriverName == null ) throw new ArgumentException( "Must have a driver name.", nameof( info ) );
            if( context == null ) throw new ArgumentNullException( nameof(context) );
            ReadInfo = info;
            TargetType = targetType ?? info.TargetType ?? info.ResolveLocalType();
            if( TargetType.IsAbstract )
            {
                throw new ArgumentException( $"Cannot deserialize an abstract type '{TargetType}'.", nameof( info ) );
            }
            if( TargetType == info.TargetType && info.HasResolvedConcreteDriver )
            {
                throw new ArgumentException( "Deserialization driver must not be already resolved.", nameof( info ) );
            }
            IsPossibleNominalDeserialization = TargetType == info.TryResolveLocalType() && !info.IsDirtyInfo;
            Context = context;
        }

    }
}
