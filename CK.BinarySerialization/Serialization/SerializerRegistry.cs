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
    public class SerializerRegistry : ISerializerResolver
    {
        ISerializerResolver[] _resolvers;

        /// <summary>
        /// Called by static BinarySerializer constructor to setup the <see cref="BinarySerializer.DefaultResolver"/>.
        /// Only independent resolvers can be registered here: resolvers that depend
        /// on the BinarySerializer.DefaultResolver cannot be referenced here and are 
        /// registered after the instance creation.
        /// </summary>
        /// <param name="isBinarySerializerDefault">Fake parameter for internal calls.</param>
        internal SerializerRegistry( bool isBinarySerializerDefault )
        {
            _resolvers = new ISerializerResolver[]
            {
                BasicTypeSerializerRegistry.Default,
                SimpleBinarySerializableRegistry.Default,
            };
        }

        /// <summary>
        /// Initializes a new registry with the <see cref="BasicTypeSerializerRegistry.Default"/>,
        /// <see cref="SimpleBinarySerializableRegistry.Default"/> and <see cref="CollectionSerializerRegistry.Default"/>.
        /// </summary>
        public SerializerRegistry()
        {
            _resolvers = new ISerializerResolver[] 
            { 
                BasicTypeSerializerRegistry.Default,
                SimpleBinarySerializableRegistry.Default,
                CollectionSerializerRegistry.Default
            };
        }

        /// <inheritdoc />
        public ISerializationDriver<T>? TryFindDriver<T>()
        {
            return (ISerializationDriver<T>?)TryFindDriver( typeof( T ) );
        }

        /// <inheritdoc />
        public IUntypedSerializationDriver? TryFindDriver( Type t )
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
        /// When new, the resolver is appended after the existing ones.
        /// </summary>
        /// <param name="resolver">The resolver that must be found or added.</param>
        public void Register( ISerializerResolver resolver )
        {
            Util.InterlockedAddUnique( ref _resolvers, resolver );
        }
    }
}
