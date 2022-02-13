using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Mutable cache of serialization drivers and known objects/keys association that should be 
    /// reused for multiple, non concurrent, serialization sessions of the same objects family.
    /// <para>
    /// This is absolutely not thread safe and can be used at any time by only one <see cref="IBinarySerializer"/>
    /// that must be disposed to free the context (otherwise a <see cref="InvalidOperationException"/> is raised).
    /// </para>
    /// </summary>
    public class BinarySerializerContext : ISerializerResolver, ISerializerKnownObject
    {
        readonly Dictionary<Type, ISerializationDriver?> _resolvers;
        readonly Dictionary<object, string> _knownObjects;
        readonly SharedBinarySerializerContext _shared;
        bool _inUse;

        /// <summary>
        /// Initializes a new <see cref="BinarySerializerContext"/>.
        /// </summary>
        /// <param name="shared">The shared context to use.</param>
        public BinarySerializerContext( SharedBinarySerializerContext shared )
        {
            _resolvers = new Dictionary<Type, ISerializationDriver?>();
            _knownObjects = new Dictionary<object, string>();
            _shared = shared;
        }

        /// <summary>
        /// Initializes a new <see cref="BinarySerializerContext"/> bound to the <see cref="BinarySerializer.DefaultSharedContext"/>.
        /// </summary>
        public BinarySerializerContext()
            : this( BinarySerializer.DefaultSharedContext )
        {
        }

        internal void Acquire()
        {
            if( _inUse )
            {
                throw new InvalidOperationException( "This BinarySerializerContext is already used by an existing BinarySerializer. The existing BinarySerializer must be disposed first." );
            }
            _inUse = true;
        }

        internal void Release()
        {
            _inUse = false;
        }

        /// <inheritdoc />
        public ISerializationDriver? TryFindDriver( Type t )
        {
            if( !_resolvers.TryGetValue( t, out var r ) )
            {
                r = _shared.TryFindDriver( t );
                _resolvers.Add( t, r );
            }
            return r;
        }

        /// <summary>
        /// Overrides any existing driver for a type.
        /// </summary>
        /// <param name="t">The serializable type.</param>
        /// <param name="driver">The driver that will handle the type's serialization.</param>
        public void EnsureDriver( Type t, ISerializationDriver driver )
        {
            _resolvers[t] = driver;
        }

        /// <inheritdoc />
        public string? GetKnownObjectKey( object o )
        {
            if( !_knownObjects.TryGetValue( o, out var r ) )
            {
                r = _shared.KnownObjects.GetKnownObjectKey( o );
                if( r != null ) _knownObjects.Add( o, r );
            }
            return r;
        }

        /// <inheritdoc />
        public void RegisterKnownObject( object o, string key )
        {
            if( _knownObjects.TryGetValue( o, out var kExist ) )
            {
                SharedSerializerKnownObject.ThrowOnDuplicateObject( o, kExist, key );
            }
            else
            {
                foreach( var kv in _knownObjects )
                {
                    if( kv.Value == key )
                    {
                        SharedSerializerKnownObject.ThrowOnDuplicateKnownKey( kv.Key, key );
                    }
                }
            }
            _knownObjects.Add( o, key );
        }

        /// <inheritdoc />
        public void RegisterKnownObject( params (object o, string key)[] association )
        {
            foreach( var a in association )
            {
                RegisterKnownObject( a.o, a.key );
            }
        }
    }
}
