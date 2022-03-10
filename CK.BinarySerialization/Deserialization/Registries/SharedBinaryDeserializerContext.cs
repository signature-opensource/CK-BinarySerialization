using CK.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Thread safe composite implementation for <see cref="IDeserializerResolver"/>.
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
        readonly ConcurrentDictionary<Type, IDeserializationDriver> _typedDrivers;
        Action<IMutableTypeReadInfo>[] _hooks;

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
        /// the <see cref="BasicTypeDeserializerRegistry.Instance"/>, <see cref="SimpleBinaryDeserializableRegistry.Instance"/> 
        /// and a <see cref="StandardGenericDeserializerRegistry"/>.
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
            _typedDrivers = new ConcurrentDictionary<Type, IDeserializationDriver>();
            _hooks = Array.Empty<Action<IMutableTypeReadInfo>>();
            _resolvers = useDefaultResolvers
                            ? new IDeserializerResolver[]
                                {
                                    BasicTypeDeserializerRegistry.Instance,
                                    SimpleBinaryDeserializableRegistry.Instance,
                                    new StandardGenericDeserializerRegistry( this ),
                                }
                            : Array.Empty<IDeserializerResolver>();
        }

        internal IDeserializationDriver GetAbstractDriver( Type t )
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
            if( _typedDrivers.TryGetValue( info.TargetType, out var d ) )
            {
                return d;
            }
            // Do not cache TargetType in _typedDrivers here: the same type may be
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
        /// When new, the resolver can be inserted before or after the existing ones.
        /// </summary>
        /// <param name="resolver">The resolver that must be found or added.</param>
        /// <param name="beforeExisting">Whether to register the resolver before the existing ones or after them.</param>
        public void Register( IDeserializerResolver resolver, bool beforeExisting )
        {
            Util.InterlockedAddUnique( ref _resolvers, resolver, beforeExisting );
        }

        /// <summary>
        /// Registers an explicit deserialization driver that will be used 
        /// when <see cref="ITypeReadInfo.TryResolveLocalType()"/> is its <see cref="IDeserializationDriver.ResolvedType"/>.
        /// <para>
        /// The local type MUST not already be mapped otherwise an <see cref="InvalidOperationException"/> is raised.
        /// </para>
        /// <para>
        /// These explicitly registered drivers take precedence over all other resolvers.
        /// </para>
        /// </summary>
        /// <param name="driver">The driver to register.</param>
        public void AddLocalTypeDeserializer( IDeserializationDriver driver )
        {
            var n = driver.ToNullable.ResolvedType;
            bool done = false;
            if( _typedDrivers.TryAdd( n, driver.ToNullable ) )
            {
                done = true;
                var nn = driver.ToNonNullable.ResolvedType;
                if( nn != n )
                {
                    done = _typedDrivers.TryAdd( nn, driver.ToNonNullable );
                    n = nn;
                }
            }
            if( !done ) throw new InvalidOperationException( $"A deserialization driver for type '{n}' is already registered." );
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
            if( hook == null ) throw new ArgumentNullException( nameof( hook ) );
            Util.InterlockedAdd( ref _hooks, hook );
        }

    }
}
