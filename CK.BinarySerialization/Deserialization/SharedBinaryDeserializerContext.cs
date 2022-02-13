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

        /// <summary>
        /// Called by static BinaryDeserializer constructor to setup the <see cref="BinaryDeserializer.DefaultSharedContext"/>.
        /// Only independent resolvers can be registered here: resolvers that depend
        /// on the BinarySerializer.DefaultResolver cannot be referenced here and are 
        /// registered after the instance creation.
        /// </summary>
        internal SharedBinaryDeserializerContext()
        {
            _knownObjects = SharedDeserializerKnownObject.Default;
            _resolvers = new IDeserializerResolver[]
            {
                BasicTypeDeserializerRegistry.Instance,
                SimpleBinaryDeserializableRegistry.Instance,
            };
        }

        /// <summary>
        /// Initializes a new registry with the <see cref="BasicTypeDeserializerRegistry.Instance"/>,
        /// <see cref="SimpleBinaryDeserializableRegistry.Instance"/> and <see cref="CollectionDeserializableRegistry.Default"/>.
        /// </summary>
        /// <param name="knownObjects">Required known objects registry.</param>
        /// <param name="useSharedResolvers">True to register the default resolvers.</param>
        public SharedBinaryDeserializerContext( IDeserializerKnownObject knownObjects, bool useSharedResolvers )
        {
            _knownObjects = knownObjects;
            _resolvers = useSharedResolvers 
                            ? new IDeserializerResolver[] 
                                { 
                                    BasicTypeDeserializerRegistry.Instance, 
                                    SimpleBinaryDeserializableRegistry.Instance,
                                    StandardGenericDeserializerRegistry.Default,
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
    }
}
