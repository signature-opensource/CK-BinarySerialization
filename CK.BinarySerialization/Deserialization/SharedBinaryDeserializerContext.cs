using CK.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Thread safe composite implementation for <see cref="IDeserializerResolver"/>.
    /// </summary>
    public class SharedBinaryDeserializerContext : IDeserializerResolver
    {
        IDeserializerResolver[] _resolvers;
        readonly IDeserializerKnownObject _knownObjects;
        readonly ConcurrentDictionary<Type, IDeserializationDriver> _typedDrivers;
        Action<IMutableTypeReadInfo>[] _hooks;

        /// <summary>
        /// Called by static BinaryDeserializer constructor to setup the <see cref="BinaryDeserializer.DefaultSharedContext"/>.
        /// Only independent resolvers can be registered here: resolvers that depend
        /// on the BinarySerializer.DefaultResolver cannot be referenced here and are 
        /// registered after the instance creation.
        /// </summary>
        internal SharedBinaryDeserializerContext( int _ )
        {
            _knownObjects = SharedDeserializerKnownObject.Default;
            _typedDrivers = new ConcurrentDictionary<Type, IDeserializationDriver>();
            _hooks = Array.Empty<Action<IMutableTypeReadInfo>>();
            _resolvers = new IDeserializerResolver[]
            {
                BasicTypeDeserializerRegistry.Instance,
                SimpleBinaryDeserializableRegistry.Instance,
            };
        }

        /// <summary>
        /// Initializes a new registry bound to an independent <see cref="SharedDeserializerKnownObject"/> with 
        /// the <see cref="BasicTypeDeserializerRegistry.Instance"/>, <see cref="SimpleBinaryDeserializableRegistry.Instance"/> 
        /// and an independent <see cref="StandardGenericDeserializerRegistry.Default"/>.
        /// <para>
        /// Caution: This is a completely independent shared cache: default comparers for dictionary keys will NOT be automatically
        /// registered in the <see cref="KnownObjects"/> (they are only automatically registered in <see cref="SharedDeserializerKnownObject.Default"/>).
        /// </para>
        /// </summary>
        /// <param name="useDefaultResolvers">True to register the default resolvers.</param>
        public SharedBinaryDeserializerContext( bool useDefaultResolvers = true )
        {
            _knownObjects = new SharedDeserializerKnownObject();
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

        /// <summary>
        /// Gets the known objects registry.
        /// </summary>
        public IDeserializerKnownObject KnownObjects => _knownObjects;


        /// <inheritdoc />
        public IDeserializationDriver? TryFindDriver( TypeReadInfo info )
        {
            foreach( var h in _hooks )
            {
                h( info.CreateMutation() );
                IDeserializationDriver? d = info.CloseMutation();
                if( d != null ) return d;
            }
            var localType = info.TryResolveLocalType();
            if( localType != null && _typedDrivers.TryGetValue( localType, out var localDriver ) )
            {
                return localDriver;
            }
            // Do not cache ResolvedType in _typedDrivers here: a TypeReadInfo use
            // its ResolvedType's deserializer only if it's explicitly added by AddLocalTypeDeserializer.
            foreach( var resolver in _resolvers )
            {
                var r = resolver.TryFindDriver( info );
                if( r != null ) return r;
            }
            return null;
        }

        /// <summary>
        /// Ensures that a resolver is registered.
        /// When new, the resolver can be inserted before or after the existing ones.
        /// </summary>
        /// <param name="resolver">The resolver that must be found or added.</param>
        public void Register( IDeserializerResolver resolver, bool beforeExisting )
        {
            Util.InterlockedAddUnique( ref _resolvers, resolver, beforeExisting );
        }

        /// <summary>
        /// Registers an explicit deserialization driver that will be used 
        /// when <see cref="TypeReadInfo.TryResolveLocalType()"/> is its <see cref="IDeserializationDriver.ResolvedType"/>.
        /// <para>
        /// The local type MUST not already exists otherwise an <see cref="InvalidOperationException"/> is raised.
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
                if( nn != n ) done = _typedDrivers.TryAdd( nn, driver.ToNonNullable );
            }
            if( !done ) throw new InvalidOperationException( $"A deserialization driver for type '{n}' is already registered." );
        }

        /// <summary>
        /// Registers a deserialization hook that will called each time a <see cref="TypeReadInfo"/> is read
        /// and a deserialization driver must be resolved. See <see cref="IMutableTypeReadInfo"/>.
        /// <para>
        /// This hook enables setting the local type to deserialize or the driver name or the <see cref="IDeserializationDriver"/> 
        /// to use instead of calling <see cref="IDeserializerResolver.TryFindDriver(TypeReadInfo)"/>.
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
