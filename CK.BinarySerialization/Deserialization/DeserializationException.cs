using System;
using System.Diagnostics;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Exception that decorates (only once) an unexpected exception during a serialization.
    /// </summary>
    public class DeserializationException : Exception
    {
        internal DeserializationException( string message )
            : base( message )
        {
            Debug.Assert( message != null );
        }

        internal DeserializationException( string message, Exception inner )
            : base( message, inner )
        {
            Debug.Assert( message != null );
            Debug.Assert( inner != null );
        }
    }
}
