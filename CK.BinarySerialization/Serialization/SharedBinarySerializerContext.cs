using CK.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Thread safe composite implementation for <see cref="ISerializerResolver"/>.
    /// </summary>
    public class SharedBinarySerializerContext : ISerializerResolver
    {
        ISerializerResolver[] _resolvers;
        readonly ConcurrentDictionary<Type, ISerializationDriver> _typedDrivers;
        readonly ISerializerKnownObject _knownObjects;

        /// <summary>
        /// Called by static BinarySerializer constructor to setup the <see cref="BinarySerializer.DefaultSharedContext"/>.
        /// Only independent resolvers can be registered in this constructor: resolvers that depend
        /// on the BinarySerializer.DefaultSharedContext cannot be referenced here and are 
        /// registered after the instance creation.
        /// </summary>
        internal SharedBinarySerializerContext( int _ )
        {
            _knownObjects = SharedSerializerKnownObject.Default;
            _typedDrivers = new ConcurrentDictionary<Type, ISerializationDriver>();
            _resolvers = new ISerializerResolver[]
            {
                BasicTypeSerializerRegistry.Instance,
                SimpleBinarySerializableRegistry.Instance,
            };
        }

        /// <summary>
        /// Initializes a new independent shared context bound to an independent <see cref="SharedSerializerKnownObject"/>, 
        /// optionally with the <see cref="BasicTypeSerializerRegistry.Instance"/>, <see cref="SimpleBinarySerializableRegistry.Instance"/> 
        /// and an independent <see cref="StandardGenericSerializerRegistry"/>.
        /// </summary>
        public SharedBinarySerializerContext( bool useDefaultResolvers = true )
        {
            _knownObjects = new SharedSerializerKnownObject();
            _typedDrivers = new ConcurrentDictionary<Type, ISerializationDriver>();
            _resolvers = useDefaultResolvers
                            ? new ISerializerResolver[]
                                {
                                    BasicTypeSerializerRegistry.Instance,
                                    SimpleBinarySerializableRegistry.Instance,
                                    new StandardGenericSerializerRegistry( this )
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
                foreach( var resolver in _resolvers )
                {
                    driver = resolver.TryFindDriver( t );
                    if( driver != null )
                    {
                        _typedDrivers[ t ] = driver;
                        break;
                    }
                }
            }
            return driver;
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
        /// <para>
        /// The type MUST not already be associated to a driver otherwise an <see cref="InvalidOperationException"/> is raised.
        /// </para>
        /// </summary>
        /// <param name="t">The serializable type.</param>
        /// <param name="driver">The driver that will handle the type's serialization.</param>
        public void AddSerializationDriver( Type t, ISerializationDriver driver )
        {
            var n = driver.ToNullable.Type;
            bool done = false;
            if( _typedDrivers.TryAdd( n, driver.ToNullable ) )
            {
                done = true;
                var nn = driver.ToNonNullable.Type;
                if( nn != n ) done = _typedDrivers.TryAdd( nn, driver.ToNonNullable );
            }
            if( !done ) throw new InvalidOperationException( $"A serialization driver for type '{n}' is already registered." );
        }

    }
}
