using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{

    /// <summary>
    /// Encapsulates a <see cref="ITypeReadInfo"/> that is not null, not a nullable type,
    /// has a driver name and a valid resolved local type and has not yet a 
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
        /// Initializes a new <see cref="DeserializerResolverArg"/>.
        /// </summary>
        /// <param name="info"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public DeserializerResolverArg( ITypeReadInfo info )
        {
            if( info == null ) throw new ArgumentNullException( nameof( info ) );
            if( info.IsNullable ) throw new ArgumentException( "Type must not be nullable.", nameof( info ) );
            if( info.DriverName == null ) throw new ArgumentException( "Must have a driver name.", nameof( info ) );
            if( info.HasResolvedDeserializationDriver ) throw new ArgumentException( "Deserialization driver must not be already resolved.", nameof( info ) );
            Info = info;
            LocalType = info.ResolveLocalType();
        }

    }
}
