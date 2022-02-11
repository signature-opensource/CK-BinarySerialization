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
    /// Thread safe implementation of unique association between a key string and its object.
    /// <see cref="StringComparer.Ordinal"/> and all other static comparer are preregistered.
    /// </summary>
    public class SerializerKnownObject : ISerializerKnownObject
    {
        // We use a simple array since there should not be a lot of instances.
        // Interlocked set is used to add new entries in a thread safe way (don't really care
        // of performance and there will be barely no concurrency here) so that
        // reads can be done lock free (where performance matters).
        (object O, string K)[] _objects;

        /// <summary>
        /// Gets a shared instance that can be used safely.
        /// <para>
        /// If dynamic registration is used (during serialization), it's more efficient to first
        /// call <see cref="GetKnownObjectKey(object)"/> with the object and if null is returned
        /// then call <see cref="RegisterKnownObject(object, string)"/>.
        /// </para>
        /// <para>
        /// It is recommended to register the known objects once for all in static constructors whenever possible.
        /// </para>
        /// </summary>
        public static readonly ISerializerKnownObject Default = new SerializerKnownObject();

        /// <summary>
        /// Initializes a new empty <see cref="SerializerKnownObject"/>.
        /// </summary>
        public SerializerKnownObject()
        {
            _objects = new (object O, string K)[]
            {
                (StringComparer.Ordinal, nameof(StringComparer.Ordinal) ),
                (StringComparer.OrdinalIgnoreCase, nameof(StringComparer.OrdinalIgnoreCase) ),
                (StringComparer.InvariantCulture, nameof(StringComparer.InvariantCulture) ),
                (StringComparer.InvariantCultureIgnoreCase, nameof(StringComparer.InvariantCultureIgnoreCase) ),
                (StringComparer.CurrentCulture, nameof(StringComparer.CurrentCulture) ),
                (StringComparer.CurrentCultureIgnoreCase, nameof(StringComparer.CurrentCultureIgnoreCase) )
            };
        }

        /// <inheritdoc />
        public void RegisterKnownObject( object o, string key )
        {
            Util.InterlockedSet( ref _objects, t => Add( new List<(object, string)>( t ), (o, key) ).ToArray() );
        }

        /// <inheritdoc />
        public void RegisterKnownObject( params (object o, string key)[] association )
        {
            Util.InterlockedSet( ref _objects, t =>
            {
                var l = new List<(object, string)>( t );
                foreach( var o in association ) Add( l, o );
                return l.ToArray();
            } );
        }

        static List<(object O, string K)> Add( List<(object O, string K)> l, (object O, string K) a )
        {
            foreach( var e in l )
            {
                if( e.K == a.K )
                {
                    if( e.O == a.O ) return l;
                    throw new InvalidOperationException( $"Known Object key '{e.K}' is already associated to another instance (of type '{e.O.GetType()}')." );
                }
                if( e.O == a.O )
                {
                    if( e.K == a.K ) return l;
                    throw new InvalidOperationException( $"Known Object instance (of type '{e.O.GetType()}') cannot be associated to key '{a.K}' since it is already associated to key '{e.K}'." );
                }
            }
            l.Add( a );
            return l;
        }

        /// <inheritdoc />
        public string? GetKnownObjectKey( object o )
        {
            var l = _objects;
            foreach( var e in _objects )
            {
                if( e.O == o ) return e.K;
            }
            return null;
        }

    }
}
