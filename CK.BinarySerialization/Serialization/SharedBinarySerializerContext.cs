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
        readonly ISerializerKnownObject _knownObjects;

        /// <summary>
        /// Called by static BinarySerializer constructor to setup the <see cref="BinarySerializer.DefaultSharedContext"/>.
        /// Only independent resolvers can be registered in this constructor: resolvers that depend
        /// on the BinarySerializer.DefaultSharedContext cannot be referenced here and are 
        /// registered after the instance creation.
        /// </summary>
        internal SharedBinarySerializerContext()
        {
            _knownObjects = SharedSerializerKnownObject.Default;
            _resolvers = new ISerializerResolver[]
            {
                BasicTypeSerializerRegistry.Instance,
                SimpleBinarySerializableRegistry.Instance,
            };
        }

        /// <summary>
        /// Initializes a new shared context bound to a <see cref="ISerializerKnownObject"/>, optionally with 
        /// the <see cref="BasicTypeSerializerRegistry.Instance"/>, <see cref="SimpleBinarySerializableRegistry.Instance"/> 
        /// and <see cref="StandardGenericSerializerRegistry.Default"/>.
        /// </summary>
        /// <param name="knownObjects">Required known objects registry.</param>
        /// <param name="useSharedResolvers">True to register the default resolvers.</param>
        public SharedBinarySerializerContext( ISerializerKnownObject knownObjects, bool useSharedResolvers )
        {
            _knownObjects = knownObjects;
            _resolvers = useSharedResolvers
                            ? new ISerializerResolver[] 
                                { 
                                    BasicTypeSerializerRegistry.Instance,
                                    SimpleBinarySerializableRegistry.Instance,
                                    StandardGenericSerializerRegistry.Default
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
            foreach( var resolver in _resolvers )
            {
                var r = resolver.TryFindDriver( t );
                if( r != null ) return r;
            }
            return null;
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
    }
}
