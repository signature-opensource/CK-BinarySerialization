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
        public readonly ITypeReadInfo Info;

        /// <summary>
        /// Gets the local type.
        /// </summary>
        public readonly Type LocalType;

        /// <summary>
        /// Gets the driver name.
        /// </summary>
        public string DriverName => Info.DriverName!;

        /// <summary>
        /// Gets the shared context. 
        /// This is used only to detect mismatch of resolution context.
        /// </summary>
        public readonly SharedBinaryDeserializerContext Context;

        /// <summary>
        /// Initializes a new <see cref="DeserializerResolverArg"/>.
        /// </summary>
        /// <param name="info">The type info for which a deserialization driver must be resolved.</param>
        /// <param name="context">The shared context is used only to detect mismatch of resolution context.</param>
        public DeserializerResolverArg( ITypeReadInfo info, SharedBinaryDeserializerContext context )
        {
            if( info == null ) throw new ArgumentNullException( nameof( info ) );
            if( info.IsNullable ) throw new ArgumentException( "Type must not be nullable.", nameof( info ) );
            if( info.DriverName == null ) throw new ArgumentException( "Must have a driver name.", nameof( info ) );
            if( info.HasResolvedDeserializationDriver ) throw new ArgumentException( "Deserialization driver must not be already resolved.", nameof( info ) );
            if( context == null ) throw new ArgumentNullException( nameof(context) );
            Info = info;
            LocalType = info.ResolveLocalType();
            if( LocalType.IsAbstract ) throw new ArgumentException( $"Cannot deserialize an abstract type '{LocalType}'.", nameof( info ) );
            Context = context;
        }

    }
}
