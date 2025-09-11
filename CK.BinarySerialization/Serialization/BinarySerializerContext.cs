using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CK.BinarySerialization;

/// <summary>
/// Mutable cache of serialization drivers and known objects/keys association that should be 
/// reused for multiple, non concurrent, serialization sessions of the same objects family.
/// <para>
/// This is absolutely not thread safe and can be used at any time by only one <see cref="IBinarySerializer"/>
/// that must be disposed before reusing this context (otherwise a <see cref="InvalidOperationException"/> is raised).
/// </para>
/// </summary>
public class BinarySerializerContext
{
    readonly Dictionary<Type, ISerializationDriver?> _cache;
    readonly Dictionary<object, string> _knownObjects;
    readonly SharedBinarySerializerContext _shared;
    readonly SimpleServiceContainer _services;
    int _maxRecurse;
    Statistics _stats;

    bool _inUse;

    /// <summary>
    /// Captures cache hit statistics from the last session.
    /// </summary>
    public struct Statistics
    {
        internal int _driverLookup;
        internal int _driverSharedLookup;
        internal int _driverSharedContextCached;
        internal int _driverContextCached;
        internal int _driverNeverCached;
        internal int _driverNonSealed;

        /// <summary>
        /// Gets the number of drivers lookup.
        /// </summary>
        public int DriverLookup => _driverLookup;

        /// <summary>
        /// Gets the number of drivers lookup in the <see cref="SharedBinarySerializerContext"/>.
        /// </summary>
        public int DriverSharedLookup => _driverSharedLookup;

        /// <summary>
        /// Gets the number of drivers that have been instantiated during this session
        /// and cached in the <see cref="SharedBinarySerializerContext"/>.
        /// </summary>
        public int DriverSharedContextCached => _driverSharedContextCached;

        /// <summary>
        /// Gets the number of drivers that have been instantiated during this session
        /// and cached in the <see cref="BinarySerializerContext"/> but not in the <see cref="SharedBinarySerializerContext"/>.
        /// </summary>
        public int DriverContextCached => _driverContextCached;

        /// <summary>
        /// Gets the number of drivers that have been instantiated during this session
        /// and not cached (their <see cref="ISerializationDriver.CacheLevel"/> is <see cref="SerializationDriverCacheLevel.Never"/>).
        /// </summary>
        public int DriverNeverCached => _driverNeverCached;

        /// <summary>
        /// Gets the number of non sealed classes lookup. These non sealed classes use a generic abstract driver
        /// that lookups the actual driver based on the runtime type (<see cref="Object.GetType()"/>).
        /// </summary>
        public int DriverNonSealed => _driverNonSealed;

        internal void Reset() => this = default;

    }


    /// <summary>
    /// Initializes a new <see cref="BinarySerializerContext"/>.
    /// </summary>
    /// <param name="shared">The shared context to use. Defaults to <see cref="BinarySerializer.DefaultSharedContext"/>.</param>
    /// <param name="services">Optional base services.</param>
    public BinarySerializerContext( SharedBinarySerializerContext? shared = null, IServiceProvider? services = null )
    {
        _cache = new Dictionary<Type, ISerializationDriver?>();
        _knownObjects = new Dictionary<object, string>();
        _shared = shared ?? BinarySerializer.DefaultSharedContext;
        _maxRecurse = 100;
        _services = new SimpleServiceContainer( services );
    }

    internal void Acquire()
    {
        if( _inUse )
        {
            Throw.InvalidOperationException( "This BinarySerializerContext is already used by an existing BinarySerializer. The existing BinarySerializer must be disposed first." );
        }
        _inUse = true;
        _stats.Reset();
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
    /// Gets a mutable service container.
    /// </summary>
    public SimpleServiceContainer Services => _services;

    /// <summary>
    /// Gets whether a type is serializable: a <see cref="ISerializationDriver"/> is available.
    /// </summary>
    /// <param name="t">The type.</param>
    /// <returns>True if a driver is available, false otherwise.</returns>
    public bool IsSerializable( Type t ) => TryFindDriver( t ) != null;

    /// <summary>
    /// Gets or sets the maximal recursion depth before deferring the write of a reference type.
    /// (see <see cref="ISerializationDriverAllowDeferredRead"/> and <see cref="IDeserializationDeferredDriver"/>).
    /// Defaults to 100 and must be greater than or equal to 0.
    /// <para>
    /// There is no real reason to change this parameter but this can be done freely as long as it remains not too big
    /// otherwise a stack overflow may occur.
    /// </para>
    /// <para>
    /// A small value mean slightly more bigger serialized data and more chance to require a second pass of deserialization
    /// (second pass is required when a class has been mutated to a struct and its write has been deferred).
    /// </para>
    /// </summary>
    public int MaxRecursionDepth
    {
        get => _maxRecurse;
        set
        {
            Throw.CheckOutOfRangeArgument( _maxRecurse >= 0 );
            _maxRecurse = value;
        }
    }

    /// <summary>
    /// Gets the last serialization session statistics.
    /// </summary>
    public ref Statistics LastStatistics => ref _stats;

    /// <inheritdoc />
    public ISerializationDriver? TryFindDriver( Type t )
    {
        ++_stats._driverLookup;
        if( !_cache.TryGetValue( t, out var r ) )
        {
            ++_stats._driverSharedLookup;
            r = _shared.TryFindDriver( this, t );
            // If the driver is null, we don't register the null: this type is currently not serializable
            // and the current session will fail but it may be thanks to future resolvers or serializers.
            if( r == null ) return null;
            if( r.CacheLevel != SerializationDriverCacheLevel.Never )
            {
                _cache.Add( t, r );
            }
        }
        return r;
    }

    /// <summary>
    /// If the type is not sealed (it's necessarily a class or an interface) then returns a
    /// generic driver that relies on the actual runtime type. Otherwise calls <see cref="TryFindDriver(Type)"/>.
    /// </summary>
    /// <param name="t">The type for which a driver is needed.</param>
    /// <returns>The driver or null.</returns>
    public ISerializationDriver? TryFindPossiblyAbstractDriver( Type t )
    {
        if( !t.IsSealed )
        {
            Debug.Assert( t.IsClass || t.IsInterface, "Non sealed is a class or an interface." );
            _stats._driverNeverCached++;
            return Serialization.DAbstract.Instance.Nullable;
        }
        return TryFindDriver( t );
    }

    /// <summary>
    /// Finds a driver or throws a <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <param name="t">The type for which a driver must be resolved.</param>
    /// <returns>The driver.</returns>
    public ISerializationDriver FindDriver( Type t )
    {
        var d = TryFindDriver( t );
        if( d == null )
        {
            Throw.InvalidOperationException( $"Unable to find a serialization driver for type '{t}'." );
        }
        return d;
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
