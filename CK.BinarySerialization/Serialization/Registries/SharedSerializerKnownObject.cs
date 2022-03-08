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
            // In NetCoreApp3.1 the System.Collections.Generic.NonRandomizedStringEqualityComparer is not exposed :(.
            // See it here: https://source.dot.net/#System.Private.CoreLib/NonRandomizedStringEqualityComparer.cs
            // And the dictionary constructor here: https://source.dot.net/#System.Private.CoreLib/Dictionary.cs,65
            // This trick implies that the Comparer is not the real one.
            // We need to retrieve the 3 actual singletons that wraps the EqualityComparer<string>.Default, StringComparer.Ordinal, and StringComparer.OrdinalIgnoreCase.
            // In Net6 this won't be an issue: we'll call the public GetStringComparer(object? comparer) with null, StringComparer.Ordinal, and StringComparer.OrdinalIgnoreCase.
            // In NetCoreApp3.1 it appears that only one static field exists. Take it.
            var tHidden = typeof( int ).Assembly.GetType( "System.Collections.Generic.NonRandomizedStringEqualityComparer", true );
            var single = tHidden!.GetField( "<Default>k__BackingField", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic )?.GetValue( null );
            if( single == null ) throw new CKException( "Unable to retrieve the default wrapper Comparer from NonRandomizedStringEqualityComparer." );

            _objects = new (object O, string K)[]
            {
                (StringComparer.Ordinal, "StringComparer.Ordinal" ),
                (StringComparer.OrdinalIgnoreCase, "StringComparer.OrdinalIgnoreCase" ),
                (StringComparer.InvariantCulture, "StringComparer.InvariantCulture" ),
                (StringComparer.InvariantCultureIgnoreCase, "StringComparer.InvariantCultureIgnoreCase" ),
                (StringComparer.CurrentCulture, "StringComparer.CurrentCulture" ),
                (StringComparer.CurrentCultureIgnoreCase, "StringComparer.CurrentCultureIgnoreCase" ),
                (single, "StringComparer.WrappedAroundDefaultComparer" )
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

        static void ThrowOnDuplicateObject( object oExist, string kExist, string kNew )
        {
            throw new InvalidOperationException( $"Known Object instance (of type '{oExist.GetType()}') cannot be associated to key '{kNew}' since it is already associated to key '{kExist}'." );
        }

        static void ThrowOnDuplicateKnownKey( object o, string key )
        {
            throw new InvalidOperationException( $"Known Object key '{key}' is already associated to another instance (of type '{o.GetType()}')." );
        }

        /// <inheritdoc />
        public string? GetKnownObjectKey( object o )
        {
            foreach( var e in _objects )
            {
                if( e.O == o ) return e.K;
            }
            return null;
        }

    }
}
