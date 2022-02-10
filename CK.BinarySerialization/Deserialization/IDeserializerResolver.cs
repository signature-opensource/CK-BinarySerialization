using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Bag for deserializers that tries to resolve a <see cref="IDeserializationDriver"/> from 
    /// a <see cref="TypeReadInfo"/>.
    /// </summary>
    public interface IDeserializerResolver
    {
        /// <summary>
        /// Tries to find a deserialization driver for a type information.
        /// </summary>
        /// <param name="info">The type information to resolve.</param>
        /// <returns>The deserialization driver or null.</returns>
        IDeserializationDriver? TryFindDriver( TypeReadInfo info );
    }
}
