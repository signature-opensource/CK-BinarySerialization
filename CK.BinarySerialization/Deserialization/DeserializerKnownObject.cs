using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

// These are fixed in Net6.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8604 // Possible null reference argument.

namespace CK.BinarySerialization
{
    /// <summary>
    /// Thread safe implementation of association between a unique key string and a known object.
    /// <see cref="StringComparer.Ordinal"/> and all other static comparer are preregistered.
    /// </summary>
    public class DeserializerKnownObject : IDeserializerKnownObject
    {
        // Same as the one in SerializerRegistry except that 2 different keys can be
        // associated to the same object.
        (string K, object O)[] _knownKeys;

        /// <summary>
        /// Initializes a new empty <see cref="DeserializerKnownObject"/>.
        /// </summary>
        public DeserializerKnownObject()
        {
            _knownKeys = new (string K, object O)[]
            {
                (nameof(StringComparer.Ordinal), StringComparer.Ordinal ),
                (nameof(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase ),
                (nameof(StringComparer.InvariantCulture), StringComparer.InvariantCulture ),
                (nameof(StringComparer.InvariantCultureIgnoreCase), StringComparer.InvariantCultureIgnoreCase ),
                (nameof(StringComparer.CurrentCulture), StringComparer.CurrentCulture ),
                (nameof(StringComparer.CurrentCultureIgnoreCase), StringComparer.CurrentCultureIgnoreCase )
            };
        }

        /// <inheritdoc />
        public void RegisterKnownKey( string key, object o )
        {
            Util.InterlockedSet( ref _knownKeys, t => Add( new List<(string, object)>( t ), (key, o) ).ToArray() );
        }

        /// <inheritdoc />
        public void RegisterKnownKey( params (string key, object o)[] mapping )
        {
            Util.InterlockedSet( ref _knownKeys, t =>
            {
                var l = new List<(string, object)>( t );
                foreach( var o in mapping ) Add( l, o );
                return l.ToArray();
            } );
        }

        static List<(string K, object O)> Add( List<(string K, object O)> l, (string K, object O) a )
        {
            foreach( var e in l )
            {
                if( e.K == a.K )
                {
                    if( e.O == a.O ) return l;
                    throw new InvalidOperationException( $"Known Object key '{e.K}' is already associated to another instance (of type '{e.O.GetType()}')." );
                }
            }
            l.Add( a );
            return l;
        }

        /// <inheritdoc />
        public object? GetKnownObject( string instanceKey )
        {
            var l = _knownKeys;
            foreach( var e in l )
            {
                if( e.K == instanceKey ) return e.O;
            }
            return null;
        }

    }
}
