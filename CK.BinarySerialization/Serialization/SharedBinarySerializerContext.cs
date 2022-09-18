using CK.BinarySerialization.Serialization;
using CK.Core;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Thread safe composite implementation for <see cref="ISerializerResolver"/>.
    /// <para>
    /// Holds a concurrent cache of Type to <see cref="ISerializationDriver"/> mappings for
    /// drivers with <see cref="ISerializationDriver.CacheLevel"/> set to <see cref="SerializationDriverCacheLevel.SharedContext"/>.
    /// </para>
    /// A singleton instance is exposed by <see cref="BinarySerializer.DefaultSharedContext"/>.
    /// <para>
    /// Unresolved serializers are NOT cached.
    /// </para>
    /// </summary>
    public sealed class SharedBinarySerializerContext : ISerializerResolver
    {
        ISerializerResolver[] _resolvers;
        readonly ConcurrentDictionary<Type, ISerializationDriver?> _typedDrivers;
        readonly ISerializerKnownObject _knownObjects;

        // Even .Net5 [ModuleInitializer] doesn't help us here.
        // A module initialization is triggered when the first type in the module is solicited.
        // To provide a true unattended initialization one would need to use 
        // RuntimeHelpers.RunModuleConstructor on modules of assemblies loaded, typically from 
        // a OnAssemblyLoad event. This doesn't seem to be a good idea: code generation may 
        // be a better way of doing this.
        // Currently, the Sliced and Poco companion should be systematically registered, so let's do
        // this rather awful trick.
        static readonly ISerializerResolver? _rSliced = (ISerializerResolver?)GetInstance( "CK.BinarySerialization.SlicedSerializerResolver, CK.BinarySerialization.Sliced" );
        static readonly ISerializerResolver? _rPoco = (ISerializerResolver?)GetInstance( "CK.BinarySerialization.PocoSerializerResolver, CK.BinarySerialization.IPoco" );

        static internal object? GetInstance( string aqn ) => Type.GetType( aqn, throwOnError: false )?
                                                                  .GetField( "Instance", BindingFlags.Static | BindingFlags.Public )?
                                                                  .GetValue( null );

        /// <summary>
        /// Initializes a new independent shared context bound to a possibly independent <see cref="SharedSerializerKnownObject"/>, 
        /// optionally with the <see cref="BasicTypesSerializerResolver.Instance"/>, <see cref="SimpleBinarySerializerResolver.Instance"/> 
        /// and <see cref="StandardGenericSerializerResolver.Instance"/> along with Sliced and Poco resolvers if they can be loaded.
        /// <para>
        /// Caution: if <see cref="SharedSerializerKnownObject.Default"/> is not used, default comparers for dictionary keys will NOT be automatically
        /// registered in the <see cref="KnownObjects"/> (they are only automatically registered in <see cref="SharedSerializerKnownObject.Default"/>).
        /// </para>
        /// </summary>
        /// <param name="useDefaultResolvers">False to include no resolvers.</param>
        /// <param name="knownObjects">By default the <see cref="SharedSerializerKnownObject.Default"/> is used.</param>
        public SharedBinarySerializerContext( bool useDefaultResolvers = true, SharedSerializerKnownObject? knownObjects = null )
        {
            _knownObjects = knownObjects ?? SharedSerializerKnownObject.Default;
            _typedDrivers = new ConcurrentDictionary<Type, ISerializationDriver?>();
            if( useDefaultResolvers )
            {
                _resolvers = _rSliced != null
                                ? (
                                    _rPoco != null
                                    ? new ISerializerResolver[] { BasicTypesSerializerResolver.Instance,
                                                                  SimpleBinarySerializerResolver.Instance,
                                                                  StandardGenericSerializerResolver.Instance,
                                                                  _rSliced,
                                                                  _rPoco
                                                                }
                                    : new ISerializerResolver[] { BasicTypesSerializerResolver.Instance,
                                                                  SimpleBinarySerializerResolver.Instance,
                                                                  StandardGenericSerializerResolver.Instance,
                                                                  _rSliced
                                                                }
                                   )
                                : (
                                    _rPoco != null
                                    ? new ISerializerResolver[] { BasicTypesSerializerResolver.Instance,
                                                                  SimpleBinarySerializerResolver.Instance,
                                                                  StandardGenericSerializerResolver.Instance,
                                                                  _rPoco
                                                                }
                                    : new ISerializerResolver[] { BasicTypesSerializerResolver.Instance,
                                                                  SimpleBinarySerializerResolver.Instance,
                                                                  StandardGenericSerializerResolver.Instance
                                                                }
                                  );
            }
            else
            {
                _resolvers = Array.Empty<ISerializerResolver>();
            }
        }

        /// <summary>
        /// Gets the known objects registry.
        /// </summary>
        public ISerializerKnownObject KnownObjects => _knownObjects;

        /// <summary>
        /// Used to mark a type for which resolver returned a <see cref="SerializationDriverCacheLevel.Never"/> or
        /// <see cref="SerializationDriverCacheLevel.Context"/>.
        /// This avoids the lock around resolution and insertion into the concurrent dictionary after the first lookup and,
        /// since this caches the ISerializerResolver that has been found, it also avoids the lookup in the _resolvers
        /// array.
        /// </summary>
        sealed class NonCacheableDriverSentinel : ISerializationDriver
        {
            public NonCacheableDriverSentinel( ISerializerResolver resolver, SerializationDriverCacheLevel cacheLevel )
            {
                Debug.Assert( cacheLevel != SerializationDriverCacheLevel.SharedContext );
                Resolver = resolver;
                CacheLevel = cacheLevel;
            }

            public ISerializerResolver Resolver { get; }

            public SerializationDriverCacheLevel CacheLevel { get; }

            public void UpdateStatistics( ref BinarySerializerContext.Statistics s )
            {
                if( CacheLevel == SerializationDriverCacheLevel.Never )
                {
                    s._driverNeverCached++;
                }
                else
                {
                    s._driverContextCached++;
                }
            }

            public string DriverName => nameof( NonCacheableDriverSentinel );

            int ISerializationDriver.SerializationVersion => throw new NotSupportedException();

            Delegate ISerializationDriver.UntypedWriter => throw new NotSupportedException();

            Delegate ISerializationDriver.TypedWriter => throw new NotSupportedException();

            ISerializationDriver ISerializationDriver.ToNullable => throw new NotSupportedException();

            ISerializationDriver ISerializationDriver.ToNonNullable => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public ISerializationDriver? TryFindDriver( BinarySerializerContext context, Type t )
        {
            Throw.CheckArgument( context?.Shared == this );
            Throw.CheckNotNullArgument( t );

            if( !_typedDrivers.TryGetValue( t, out var driver ) )
            {
                // We avoid duplicate instantiation: as soon as a driver must be resolved
                // we lock this shared context and rely on the reentrancy of the lock 
                // to allow subordinate types resolution.
                lock( _resolvers )
                {
                    // Double Check Lock.
                    if( !_typedDrivers.TryGetValue( t, out driver ) )
                    {
                        ISerializerResolver? found = null;
                        foreach( var resolver in _resolvers )
                        {
                            driver = resolver.TryFindDriver( context, t );
                            if( driver != null )
                            {
                                found = resolver;
                                break;
                            }
                        }
                        // If the driver is null, we don't register the null: this type is currently not serializable
                        // and the current session will fail but it may be thanks to future resolvers or serializers.
                        if( driver == null ) return null;
                        // If the driver cannot be cached at this level, we use the sentinel.
                        // We use GetOrAdd here so that if a concurrent AddSerializationDriver or
                        // SetNotSerializable happened, it wins.
                        // Statistics are updated regardless of such concurrent updates (they should barely happen
                        // and this is not really relevant).
                        if( driver.CacheLevel != SerializationDriverCacheLevel.SharedContext )
                        {
                            Debug.Assert( found != null );
                            var sentinel = new NonCacheableDriverSentinel( found, driver.CacheLevel );
                            sentinel.UpdateStatistics( ref context.LastStatistics );
                            var already = _typedDrivers.GetOrAdd( t, sentinel );
                            if( already != sentinel ) driver = already;
                        }
                        else
                        {
                            context.LastStatistics._driverSharedContextCached++;
                            driver = _typedDrivers.GetOrAdd( t, driver );
                        }
                    }
                }
            }
            // Always do this because of the Double Check Lock.
            if( driver is NonCacheableDriverSentinel noCache )
            {
                noCache.UpdateStatistics( ref context.LastStatistics );
                driver = noCache.Resolver.TryFindDriver( context, t );
            }
            return driver;
        }

        /// <summary>
        /// Ensures that a resolver is registered.
        /// When new, the resolver can be appended after or inserted before the existing ones.
        /// <para>
        /// Because of caching of drivers, registering should obviously be done before any serialization
        /// occurs on this shared context.
        /// </para>
        /// </summary>
        /// <param name="resolver">The resolver that must be found or added.</param>
        /// <param name="beforeExisting">True to insert the resolver before the other ones.</param>
        public void AddResolver( ISerializerResolver resolver, bool beforeExisting = false )
        {
            Util.InterlockedAddUnique( ref _resolvers, resolver, beforeExisting );
        }

        /// <summary>
        /// Registers a driver for a type. The driver will have the priority over any driver that
        /// could be resolved by the resolvers (see <see cref="AddResolver(ISerializerResolver, bool)"/>.
        /// The driver must be able to handle the type otherwise kitten will be killed.
        /// <para>
        /// The type MUST not already be associated to a driver otherwise an <see cref="InvalidOperationException"/> is raised:
        /// just like <see cref="AddResolver(ISerializerResolver, bool)"/> this should obviously be done before any serialization
        /// occurs on this shared context.
        /// </para>
        /// <para>
        /// For coherency, since the driver is cached at this level, it MUST have its <see cref="ISerializationDriver.CacheLevel"/>
        /// set to <see cref="SerializationDriverCacheLevel.SharedContext"/> otherwise an <see cref="ArgumentException"/> is thrown.
        /// </para>
        /// </summary>
        /// <param name="t">The serializable type.</param>
        /// <param name="driver">The driver that will handle the type's serialization.</param>
        public void AddSerializationDriver( Type t, ISerializationDriver driver )
        {
            Throw.CheckNotNullArgument( t );
            Throw.CheckNotNullArgument( driver );
            Throw.CheckArgument( driver.CacheLevel == SerializationDriverCacheLevel.SharedContext );
            driver = driver.ToNonNullable;
            bool done = false;
            if( _typedDrivers.TryAdd( t, driver ) )
            {
                done = true;
                if( t.IsValueType )
                {
                    if( Nullable.GetUnderlyingType( t ) != null ) Throw.ArgumentException( nameof( t ), $"Type '{t}' must not be a nullable value type." );
                    t = typeof( Nullable<> ).MakeGenericType( t );
                    done = _typedDrivers.TryAdd( t, driver.ToNullable );
                }
            }
            if( !done ) Throw.InvalidOperationException( $"A serialization driver for type '{t}' is already registered." );
        }

        /// <summary>
        /// Sets a type as a non serializable one: a null driver will always be returned.
        /// </summary>
        /// <para>
        /// The type MUST not already be associated to a driver otherwise an <see cref="InvalidOperationException"/> is raised
        /// but this method can be called multiple times for the same type.
        /// </para>
        /// <param name="t">The type that must not be serializable.</param>
        public void SetNotSerializable( Type t )
        {
            Throw.CheckNotNullArgument( t );
            bool done = false;
            if( _typedDrivers.AddOrUpdate( t, (ISerializationDriver?)null, (t, existing) => existing ) == null )
            {
                done = true;
                if( t.IsValueType )
                {
                    if( Nullable.GetUnderlyingType( t ) != null ) Throw.ArgumentException( nameof( t ), $"Type '{t}' must not be a nullable value type." );
                    t = typeof( Nullable<> ).MakeGenericType( t );
                    done = _typedDrivers.AddOrUpdate( t, (ISerializationDriver?)null, ( t, existing ) => existing ) == null;
                }
            }
            if( !done ) Throw.InvalidOperationException( $"A serialization driver for type '{t}' is already registered." );
        }


        /// <summary>
        /// Helper methods that gets the public static void Write( IBinarySerializer s, in T o ) method
        /// or throws an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="writerHost">The type that must contain the method.</param>
        /// <param name="instanceType">The type to write.</param>
        /// <returns>The writer method.</returns>
        public static MethodInfo GetStaticWriter( Type writerHost, Type instanceType )
        {
            var writer = writerHost.GetMethod( "Write", BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public, null, new[] { typeof( IBinarySerializer ), instanceType.MakeByRefType() }, null );
            if( writer == null )
            {
                Throw.InvalidOperationException( $"Type '{writerHost}' must have a 'public static void Write( IBinarySerializer s, in {instanceType.Name} o )' static writer. Beware of the 'in' modifier!" );
            }
            return writer;
        }
    }
}
