using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Mutable cache of serialization drivers and known objects/keys association.
    /// <para>
    /// This is absolutely not thread safe and should be registered in a non shared registry.
    /// </para>
    /// </summary>
    public class BinarySerializerContext : ISerializerResolver, ISerializerKnownObject
    {
        readonly Dictionary<Type, ISerializationDriver?> _registry;
        readonly Dictionary<object, string> _knownObjects;
        readonly ISerializerResolver? _backSerializer;
        readonly ISerializerKnownObject? _backKnownObject;
        bool _inUse;

        public BinarySerializerContext( ISerializerResolver? backSerializer, ISerializerKnownObject? backKnownObject )
        {
            _registry = new Dictionary<Type, ISerializationDriver?>();
            _knownObjects = new Dictionary<object, string>();
            _backSerializer = backSerializer;
            _backKnownObject = backKnownObject;
        }

        public BinarySerializerContext()
            : this( BinarySerializer.DefaultResolver, BinarySerializer.DefaultKnownObjects )
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

        public ISerializationDriver? TryFindDriver( Type t )
        {
            if( !_registry.TryGetValue( t, out var r ) )
            {
                r = _backSerializer?.TryFindDriver( t );
                _registry.Add( t, r );
            }
            return r;
        }

        public void Add( Type t, ISerializationDriver driver )
        {
            _registry.Add( t, driver );
        }

        public string? GetKnownObjectKey( object o )
        {
            if( !_knownObjects.TryGetValue( o, out var r ) )
            {
                r = _backKnownObject?.GetKnownObjectKey( o );
                if( r != null ) _knownObjects.Add( o, r );
            }
            return r;
        }

        public void RegisterKnownObject( object o, string key )
        {
            if( _knownObjects.TryGetValue( o, out var kExist ) )
            {
                SerializerKnownObject.ThrowOnDuplicateObject( o, kExist, key );
            }
            else
            {
                foreach( var kv in _knownObjects )
                {
                    if( kv.Value == key )
                    {
                        SerializerKnownObject.ThrowOnDuplicateKnownKey( kv.Key, key );
                    }
                }
            }
            _knownObjects.Add( o, key );
        }

        public void RegisterKnownObject( params (object o, string key)[] association )
        {
            foreach( var a in association )
            {
                RegisterKnownObject( a.o, a.key );
            }
        }
    }
}
