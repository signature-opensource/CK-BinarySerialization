using CK.Core;
using System;
using System.Collections.Concurrent;

namespace CK.BinarySerialization;

/// <summary>
/// Thread safe composite implementation for <see cref="IDeserializerResolver"/>.
/// A singleton instance is exposed by <see cref="BinaryDeserializer.DefaultSharedContext"/>.
/// <para>
/// Caching deserialization drivers is not as easy as caching serializers: two serialized types
/// can perfectly be deserialized into the same local type. The actual cache is the <see cref="ITypeReadInfo"/>
/// itself that is resolved once per deserialization session.
/// </para>
/// </summary>
public sealed class SharedBinaryDeserializerContext : IDeserializerResolver
{
    IDeserializerResolver[] _resolvers;
    readonly IDeserializerKnownObject _knownObjects;
    readonly ConcurrentDictionary<Type, IDeserializationDriver> _localTypedDrivers;
    Action<IMutableTypeReadInfo>[] _hooks;

    /// <summary>
    /// See <see cref="SharedBinarySerializerContext._rSliced"/> for rationales.
    /// </summary>
    static readonly IDeserializerResolver? _rSliced = (IDeserializerResolver?)SharedBinarySerializerContext.GetInstance( "CK.BinarySerialization.SlicedDeserializerResolver, CK.BinarySerialization.Sliced" );
    static readonly IDeserializerResolver? _rPoco = (IDeserializerResolver?)SharedBinarySerializerContext.GetInstance( "CK.BinarySerialization.PocoDeserializerResolver, CK.BinarySerialization.IPoco" );

    /// <summary>
    /// Abstract drivers are statically cached once for all.
    /// Note that the local type here may itself be sealed: it's the way it has been written that matters.
    /// </summary>
    static readonly ConcurrentDictionary<Type, IDeserializationDriver> _abstractDrivers = new();

    /// <summary>
    /// Public cache for drivers that depend only on the local type to deserialize.
    /// This is the case for <see cref="ICKSimpleBinarySerializable"/>, <see cref="ICKVersionedBinarySerializable"/>
    /// and may be the case for others like the "Sliced" serialization.
    /// Since deserialization context is irrelevant for these drivers, this dictionary is exposed to avoid 
    /// creating too much concurrent dictionaries.
    /// </summary>
    static public readonly ConcurrentDictionary<Type, IDeserializationDriver> PureLocalTypeDependentDrivers = new();

    /// <summary>
    /// Initializes a new registry bound to a possibly independent <see cref="SharedDeserializerKnownObject"/> and
    /// the <see cref="BasicTypesDeserializerResolver.Instance"/>, <see cref="SimpleBinaryDeserializerResolver.Instance"/> 
    /// and <see cref="StandardGenericDeserializerResolver"/>.
    /// <para>
    /// Caution: if <see cref="SharedDeserializerKnownObject.Default"/> is not used, default comparers for dictionary keys will NOT be automatically
    /// registered in the <see cref="KnownObjects"/> (they are only automatically registered in <see cref="SharedDeserializerKnownObject.Default"/>).
    /// </para>
    /// </summary>
    /// <param name="useDefaultResolvers">True to register the default resolvers.</param>
    /// <param name="knownObjects">By default the <see cref="SharedDeserializerKnownObject.Default"/> is used.</param>
    public SharedBinaryDeserializerContext( bool useDefaultResolvers = true, SharedDeserializerKnownObject? knownObjects = null )
    {
        _knownObjects = knownObjects ?? SharedDeserializerKnownObject.Default;
        _localTypedDrivers = new ConcurrentDictionary<Type, IDeserializationDriver>();
        _hooks = Array.Empty<Action<IMutableTypeReadInfo>>();

        if( useDefaultResolvers )
        {
            _resolvers = _rSliced != null
                            ? (
                                _rPoco != null
                                ? new IDeserializerResolver[] { BasicTypesDeserializerResolver.Instance,
                                                                SimpleBinaryDeserializerResolver.Instance,
                                                                new StandardGenericDeserializerResolver( this ),
                                                                _rSliced,
                                                                _rPoco
                                                          }
                                : new IDeserializerResolver[] { BasicTypesDeserializerResolver.Instance,
                                                                SimpleBinaryDeserializerResolver.Instance,
                                                                new StandardGenericDeserializerResolver( this ),
                                                                _rSliced
                                                              }
                                )
                            : (
                                _rPoco != null
                                ? new IDeserializerResolver[] { BasicTypesDeserializerResolver.Instance,
                                                                SimpleBinaryDeserializerResolver.Instance,
                                                                new StandardGenericDeserializerResolver( this ),
                                                                _rPoco
                                                              }
                                : new IDeserializerResolver[] { BasicTypesDeserializerResolver.Instance,
                                                                SimpleBinaryDeserializerResolver.Instance,
                                                                new StandardGenericDeserializerResolver( this )
                                                              }

                              );
        }
        else
        {
            _resolvers = Array.Empty<IDeserializerResolver>();
        }
    }

    internal static IDeserializationDriver GetAbstractDriver( Type t )
    {
        return _abstractDrivers.GetOrAdd( t, Create );

        static IDeserializationDriver Create( Type t )
        {
            var tD = typeof( Deserialization.DAbstract<> ).MakeGenericType( t );
            return (IDeserializationDriver)Activator.CreateInstance( tD )!;
        }
    }

    /// <summary>
    /// Gets the known objects registry.
    /// </summary>
    public IDeserializerKnownObject KnownObjects => _knownObjects;

    internal void CallHooks( IMutableTypeReadInfo m )
    {
        foreach( var h in _hooks )
        {
            h( m );
        }
    }

    /// <inheritdoc />
    public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
    {
        // The added local type drivers have the priority.
        if( _localTypedDrivers.TryGetValue( info.ExpectedType, out var d ) )
        {
            return d;
        }
        // Do not cache TargetType in _localTypedDrivers here: the same type may be
        // built by more than one driver.
        // We must not lookup the PureLocalTypeDependentDrivers here: some drivers may be resolved
        // based on different informations from the TypeReadInfo (typically the DriverName) that 
        // handle a Type that has such an associated PureLocalTypeDependentDriver.
        // It's up to the resolver to decide.
        foreach( var resolver in _resolvers )
        {
            var r = resolver.TryFindDriver( ref info );
            if( r != null ) return r;
        }
        return null;
    }

    /// <summary>
    /// Ensures that a resolver is registered.
    /// When new, the resolver can be appended after or inserted before the existing ones.
    /// <para>
    /// Deserialization drivers are not cached by the shared context (as opposed to the serialization
    /// drivers). However, deserialization registrations should be done before any deserialization occur.
    /// </para>
    /// </summary>
    /// <param name="resolver">The resolver that must be found or added.</param>
    /// <param name="beforeExisting">Whether to register the resolver before the existing ones.</param>
    public void AddResolver( IDeserializerResolver resolver, bool beforeExisting = false )
    {
        Util.InterlockedAddUnique( ref _resolvers, resolver, beforeExisting );
    }

    /// <summary>
    /// Registers an explicit deserialization driver for a type.
    /// <para>
    /// This <see cref="IDeserializationDriver.ResolvedType"/> MUST not already be mapped otherwise
    /// an <see cref="InvalidOperationException"/> is raised.
    /// </para>
    /// <para>
    /// These explicitly registered drivers take precedence over all other resolvers.
    /// </para>
    /// </summary>
    /// <param name="driver">The driver to register.</param>
    public void AddDeserializerDriver( IDeserializationDriver driver )
    {
        var n = driver.Nullable.ResolvedType;
        bool done = false;
        if( _localTypedDrivers.TryAdd( n, driver.Nullable ) )
        {
            done = true;
            var nn = driver.NonNullable.ResolvedType;
            if( nn != n )
            {
                done = _localTypedDrivers.TryAdd( nn, driver.NonNullable );
                n = nn;
            }
        }
        if( !done ) Throw.InvalidOperationException( $"A deserialization driver for type '{n}' is already registered." );
    }

    /// <summary>
    /// Registers a deserialization hook that will called each time a <see cref="ITypeReadInfo"/> is read
    /// and a deserialization driver must be resolved. See <see cref="IMutableTypeReadInfo"/>.
    /// <para>
    /// This hook enables setting the local type to deserialize or the driver name or the <see cref="IDeserializationDriver"/> 
    /// to use instead of calling <see cref="IDeserializerResolver.TryFindDriver(ref DeserializerResolverArg)"/>.
    /// </para>
    /// </summary>
    /// <param name="hook">The hook to register.</param>
    public void AddDeserializationHook( Action<IMutableTypeReadInfo> hook )
    {
        Throw.CheckNotNullArgument( hook );
        Util.InterlockedAdd( ref _hooks, hook );
    }

}
