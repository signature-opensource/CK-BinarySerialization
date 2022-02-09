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
        /// Initializes a new registry with the <see cref="BasicTypeSerializerRegistry.Default"/> and
        /// <see cref="SimpleBinarySerializableRegistry.Default"/>.
        /// </summary>
        public SerializerRegistry()
        {
            _resolvers = new ISerializerResolver[] { BasicTypeSerializerRegistry.Default, SimpleBinarySerializableRegistry.Default };
        }

        /// <inheritdoc />
        public ITypeSerializationDriver<T>? TryFindDriver<T>() where T : notnull
        {
            return (ITypeSerializationDriver<T>?)TryFindDriver( typeof( T ) );
        }

        /// <inheritdoc />
        public ITypeSerializationDriver? TryFindDriver( Type t )
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
