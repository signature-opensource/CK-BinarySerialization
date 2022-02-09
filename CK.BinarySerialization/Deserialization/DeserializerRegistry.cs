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
        /// Initializes a new registry with the <see cref="BasicTypeDeserializerRegistry.Default"/>
        /// and <see cref="SimpleBinaryDeserializableRegistry.Default"/>.
        /// </summary>
        public DeserializerRegistry()
        {
            _resolvers = new IDeserializerResolver[] { BasicTypeDeserializerRegistry.Default, SimpleBinaryDeserializableRegistry.Default };
        }

        /// <inheritdoc />
        public object? TryFindDriver( TypeReadInfo info )
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
