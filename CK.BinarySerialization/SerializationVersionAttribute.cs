using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Associates a version to a class or struct.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false )]
    public class SerializationVersionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new version attribute.
        /// </summary>
        /// <param name="version">The version. Must be positive or zero.</param>
        public SerializationVersionAttribute( int version )
        {
            if( version < 0 ) throw new ArgumentException( "Must be 0 or positive.", nameof( version ) );
            Version = version;
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        public int Version { get; }

        internal static int GetRequiredVersion( Type t )
        {
            var a = (SerializationVersionAttribute?)GetCustomAttribute( t, typeof( SerializationVersionAttribute ) );
            if( a == null ) throw new InvalidOperationException( $"Type '{t}' must be decorated with a [SerializationVersion()] attribute." );
            return a.Version;
        }
    }
}
