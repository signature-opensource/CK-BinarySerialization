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
    /// that must be disposed before reusing this context (otherwise a <see cref="InvalidOperationException"/> is raised).
    /// </para>
    /// </summary>
    public class BinarySerializerContext : ISerializerResolver
    {
        readonly Dictionary<Type, ISerializationDriver?> _cache;
        readonly Dictionary<object, string> _knownObjects;
        readonly SharedBinarySerializerContext _shared;
        bool _inUse;

        /// <summary>
        /// Initializes a new <see cref="BinarySerializerContext"/>.
        /// </summary>
        /// <param name="shared">The shared context to use.</param>
        public BinarySerializerContext( SharedBinarySerializerContext shared )
        {
            _cache = new Dictionary<Type, ISerializationDriver?>();
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

        /// <summary>
        /// Gets the shared serializer context used by this context.
        /// </summary>
        public SharedBinarySerializerContext Shared => _shared;

        /// <summary>
        /// Gets whether a type is serializable: a <see cref="ISerializationDriver"/> is available.
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>True if a driver is available, false otherwise.</returns>
        public bool IsSerializable( Type t ) => TryFindDriver( t ) != null;

        /// <inheritdoc />
        public ISerializationDriver? TryFindDriver( Type t )
        {
            if( !_cache.TryGetValue( t, out var r ) )
            {
                r = _shared.TryFindDriver( t );
                _cache.Add( t, r );
            }
            return r;
        }

        /// <inheritdoc cref="ISerializerKnownObject.GetKnownObjectKey(object)"/>
        public string? GetKnownObjectKey( object o )
        {
            if( !_knownObjects.TryGetValue( o, out var r ) )
            {
                r = _shared.KnownObjects.GetKnownObjectKey( o );
                if( r != null ) _knownObjects.Add( o, r );
            }
            return r;
        }
    }
}
