using CK.Core;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Thread safe composite implementation for <see cref="ISerializerResolver"/> and concurrent 
    /// cache of type to <see cref="ISerializationDriver"/> mappings.
    /// <para>
    /// Unresolved serializers are cached (by definitely storing a null driver): a driver must be resolved
    /// the first time or it will never be by this shared context.
    /// </para>
    /// </summary>
    public class SharedBinarySerializerContext : ISerializerResolver
    {
        ISerializerResolver[] _resolvers;
        readonly ConcurrentDictionary<Type, ISerializationDriver?> _typedDrivers;
        readonly ISerializerKnownObject _knownObjects;

        /// <summary>
        /// Initializes a new independent shared context bound to a possibly independent <see cref="SharedSerializerKnownObject"/>, 
        /// optionally with the <see cref="BasicTypeSerializerRegistry.Instance"/>, <see cref="SimpleBinarySerializableFactory.Instance"/> 
        /// and a <see cref="StandardGenericSerializerFactory"/>.
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
            _resolvers = useDefaultResolvers
                            ? new ISerializerResolver[]
                                {
                                    BasicTypeSerializerRegistry.Instance,
                                    SimpleBinarySerializableFactory.Instance,
                                    new StandardGenericSerializerFactory( this )
                                }
                            : Array.Empty<ISerializerResolver>();
        }

        /// <summary>
        /// Gets the known objects registry.
        /// </summary>
        public ISerializerKnownObject KnownObjects => _knownObjects;

        /// <inheritdoc />
        public ISerializationDriver? TryFindDriver( Type t )
        {
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
                        foreach( var resolver in _resolvers )
                        {
                            driver = resolver.TryFindDriver( t );
                            if( driver != null ) break;
                        }
                        _typedDrivers.TryAdd( t, driver );
                    }
                }
            }
            return driver;
        }

        public ISerializationDriver? TryFindPossiblyAbstractDriver( Type t )
        {
            if( !t.IsSealed )
            {
                Debug.Assert( t.IsClass );
                return Serialization.DAbstract.Instance;
            }
            return TryFindDriver( t );
        }

        /// <summary>
        /// Ensures that a resolver is registered.
        /// When new, the resolver is appended after or inserted before the existing ones.
        /// </summary>
        /// <param name="resolver">The resolver that must be found or added.</param>
        /// <param name="beforeExisting">True to insert the resolver before the other ones, false to append it.</param>
        public void Register( ISerializerResolver resolver, bool beforeExisting )
        {
            Util.InterlockedAddUnique( ref _resolvers, resolver, beforeExisting );
        }

        /// <summary>
        /// Registers a driver for a type.
        /// The driver must be able to handle the type otherwise kitten will be killed.
        /// <para>
        /// The type MUST not already be associated to a driver otherwise an <see cref="InvalidOperationException"/> is raised.
        /// </para>
        /// </summary>
        /// <param name="t">The serializable type.</param>
        /// <param name="driver">The driver that will handle the type's serialization.</param>
        public void AddSerializationDriver( Type t, ISerializationDriver driver )
        {
            if( t == null ) throw new ArgumentNullException( nameof( t ) );
            if( driver == null ) throw new ArgumentNullException( nameof( driver ) );
            driver = driver.ToNonNullable;
            bool done = false;
            if( _typedDrivers.TryAdd( t, driver ) )
            {
                done = true;
                if( t.IsValueType )
                {
                    if( Nullable.GetUnderlyingType( t ) != null ) throw new ArgumentException( "Type must not be a nullable value type.", nameof( t ) );
                    t = typeof( Nullable<> ).MakeGenericType( t );
                    done = _typedDrivers.TryAdd( t, driver.ToNullable );
                }
            }
            if( !done ) throw new InvalidOperationException( $"A serialization driver for type '{t}' is already registered." );
        }

        /// <summary>
        /// Helper methods that gets the public static void Write( IBinarySerializer s, in T o ) method
        /// or throws an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="writerHost">The type that must contain the method.</param>
        /// <returns>The writer method.</returns>
        public static MethodInfo GetStaticWriter( Type writerHost, Type instanceType )
        {
            var writer = writerHost.GetMethod( "Write", BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public, null, new[] { typeof( IBinarySerializer ), instanceType.MakeByRefType() }, null );
            if( writer == null )
            {
                throw new InvalidOperationException( $"Type '{writerHost}' must have a public static void Write( IBinarySerializer s, in {instanceType.Name} o ) static writer." );
            }
            return writer;
        }
    }
}
