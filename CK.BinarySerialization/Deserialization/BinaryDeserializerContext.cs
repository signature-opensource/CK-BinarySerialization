using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization;

/// <summary>
/// Mutable cache of deserialization drivers and known objects/keys association that should be 
/// reused for multiple, non concurrent, deserialization sessions of the same objects family.
/// <para>
/// This is absolutely not thread safe and can be used at any time by only one <see cref="IBinaryDeserializer"/>
/// that must be disposed to free the context (otherwise a <see cref="InvalidOperationException"/> is raised).
/// </para>
/// </summary>
public class BinaryDeserializerContext
{
    readonly Dictionary<string,object> _knownObjects;
    readonly SharedBinaryDeserializerContext _shared;
    // Avoids a lookup in the shared concurrent dictionary. Is this clever?
    readonly Dictionary<Type,IDeserializationDriver> _abstractDrivers;
    readonly SimpleServiceContainer _services;
    BinaryDeserializerImpl? _deserializer;

    /// <summary>
    /// Initializes a new <see cref="BinaryDeserializerContext"/>.
    /// </summary>
    /// <param name="shared">The shared context to use. Defaults to <see cref="BinaryDeserializer.DefaultSharedContext"/>.</param>
    /// <param name="services">Optional base services.</param>
    public BinaryDeserializerContext( SharedBinaryDeserializerContext? shared = null, IServiceProvider? services = null )
    {
        _knownObjects = new Dictionary<string, object>();
        _shared = shared ?? BinaryDeserializer.DefaultSharedContext;
        _abstractDrivers = new Dictionary<Type, IDeserializationDriver>();
        _services = new SimpleServiceContainer( services );
    }

    internal void Acquire( BinaryDeserializerImpl d )
    {
        if( _deserializer != null )
        {
            Throw.InvalidOperationException( "This BinaryDeserializerContext is already used by an existing BinaryDeserializer. The existing BinaryDeserializer must be disposed first." );
        }
        _deserializer = d;
    }

    internal void Release()
    {
        _deserializer = null;
    }

    /// <summary>
    /// Gets the shared deserializer context used by this context.
    /// </summary>
    public SharedBinaryDeserializerContext Shared => _shared;

    /// <summary>
    /// Gets a mutable service container.
    /// </summary>
    public SimpleServiceContainer Services => _services;

    internal IDeserializationDriver GetAbstractDriver( Type t ) => _abstractDrivers.GetOrSet( t, SharedBinaryDeserializerContext.GetAbstractDriver );

    internal IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
    {
        return _shared.TryFindDriver( ref info );
    }

    internal object? GetKnownObject( string instanceKey )
    {
        if( !_knownObjects.TryGetValue( instanceKey, out var r ) )
        {
            r = _shared.KnownObjects.GetKnownObject( instanceKey );
            if( r != null ) _knownObjects.Add( instanceKey, r );
        }
        return r;
    }

}
