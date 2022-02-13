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
    public class SharedSerializerKnownObject : ISerializerKnownObject
    {
        /// <summary>
        /// Gets a shared thread safe instance.
        /// <para>
        /// If dynamic registration is used (during serialization), it's more efficient to first
        /// call <see cref="ISerializerKnownObject.GetKnownObjectKey(object)"/> with the object and if null is returned
        /// then call <see cref="ISerializerKnownObject.RegisterKnownObject(object, string)"/>.
        /// </para>
        /// <para>
        /// It is recommended to register the known objects once for all in static constructors whenever possible.
        /// </para>
        /// </summary>
        public static readonly SharedSerializerKnownObject Default = new SharedSerializerKnownObject();

        // We use a simple array since there should not be a lot of instances.
        // Interlocked set is used to add new entries in a thread safe way (don't really care
        // of performance and there will be barely no concurrency here) so that
        // reads can be done lock free (where performance matters).
        (object O, string K)[] _objects;

        /// <summary>
        /// Initializes a new <see cref="SharedSerializerKnownObject"/> that 
        /// contains preregistered <see cref="StringComparer"/> singletons (like <see cref="StringComparer.OrdinalIgnoreCase"/>).
        /// </summary>
        public SharedSerializerKnownObject()
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
                    ThrowOnDuplicateKnownKey( e.O, e.K );
                }
                if( e.O == a.O )
                {
                    if( e.K == a.K ) return l;
                    ThrowOnDuplicateObject( e.O, e.K, a.K );
                }
            }
            l.Add( a );
            return l;
        }

        public static void ThrowOnDuplicateObject( object oExist, string kExist, string kNew )
        {
            throw new InvalidOperationException( $"Known Object instance (of type '{oExist.GetType()}') cannot be associated to key '{kNew}' since it is already associated to key '{kExist}'." );
        }

        public static void ThrowOnDuplicateKnownKey( object o, string key )
        {
            throw new InvalidOperationException( $"Known Object key '{key}' is already associated to another instance (of type '{o.GetType()}')." );
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
