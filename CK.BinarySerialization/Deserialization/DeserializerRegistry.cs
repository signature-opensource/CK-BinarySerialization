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
    public class DeserializerRegistry : IDeserializerResolver
    {
        IDeserializerResolver[] _resolvers;

        /// <summary>
        /// Called by static BinaryDeserializer constructor to setup the <see cref="BinaryDeserializer.DefaultResolver"/>.
        /// Only independent resolvers can be registered here: resolvers that depend
        /// on the BinarySerializer.DefaultResolver cannot be referenced here and are 
        /// registered after the instance creation.
        /// </summary>
        /// <param name="isBinarySerializerDefault">Fake parameter for internal calls.</param>
        internal DeserializerRegistry( bool isBinarySerializerDefault )
        {
            _resolvers = new IDeserializerResolver[]
            {
                BasicTypeDeserializerRegistry.Default,
                SimpleBinaryDeserializableRegistry.Default,
            };
        }

        /// <summary>
        /// Initializes a new registry with the <see cref="BasicTypeDeserializerRegistry.Default"/>,
        /// <see cref="SimpleBinaryDeserializableRegistry.Default"/> and <see cref="CollectionDeserializableRegistry.Default"/>.
        /// </summary>
        public DeserializerRegistry()
        {
            _resolvers = new IDeserializerResolver[] 
            { 
                BasicTypeDeserializerRegistry.Default, 
                SimpleBinaryDeserializableRegistry.Default,
            };
        }

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
