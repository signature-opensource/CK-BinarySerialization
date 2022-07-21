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
    public sealed class SharedDeserializerKnownObject : IDeserializerKnownObject
    {
        // Same as the one in SerializerRegistry except that 2 different keys can be
        // associated to the same object.
        (string K, object O)[] _knownKeys;

        /// <summary>
        /// Gets a shared thread safe instance of <see cref="IDeserializerKnownObject"/>.
        /// <para>
        /// If dynamic registration is used (during deserialization), it's more efficient to first
        /// call <see cref="GetKnownObject(string)"/> with the key and if null is returned
        /// then call <see cref="RegisterKnownKey(string, object)"/>.
        /// </para>
        /// <para>
        /// It is recommended to register the known objects once for all in static constructors whenever possible.
        /// </para>
        /// </summary>
        public static readonly SharedDeserializerKnownObject Default = new SharedDeserializerKnownObject();

        /// <summary>
        /// Initializes a new <see cref="SharedDeserializerKnownObject"/> with 
        /// preregistered <see cref="StringComparer"/> singletons.
        /// </summary>
        public SharedDeserializerKnownObject()
        {
            _knownKeys = new (string K, object O)[]
            {
                ("DBNull.Value", DBNull.Value),
                ("Type.Missing", Type.Missing),
                ("StringComparer.Ordinal", StringComparer.Ordinal ),
                ("StringComparer.OrdinalIgnoreCase", StringComparer.OrdinalIgnoreCase ),
                ("StringComparer.InvariantCulture", StringComparer.InvariantCulture ),
                ("StringComparer.InvariantCultureIgnoreCase", StringComparer.InvariantCultureIgnoreCase ),
                ("StringComparer.CurrentCulture", StringComparer.CurrentCulture ),
                ("StringComparer.CurrentCultureIgnoreCase", StringComparer.CurrentCultureIgnoreCase )
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
                    ThrowOnDuplicateKey( e.K, e.O );
                }
            }
            l.Add( a );
            return l;
        }

        static void ThrowOnDuplicateKey( string key, object o )
        {
            throw new InvalidOperationException( $"Known Object key '{key}' is already associated to another instance (of type '{o.GetType()}')." );
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
