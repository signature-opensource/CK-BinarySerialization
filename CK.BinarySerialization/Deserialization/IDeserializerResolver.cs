using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Finds deserializer for the name of a type.
    /// </summary>
    public interface IDeserializerResolver
    {
        /// <summary>
        /// Tries to find a deserialization driver for a type information.
        /// </summary>
        /// <param name="info">The type to resolve.</param>
        /// <returns>The deserialization driver or null.</returns>
        object? TryFindDriver( TypeReadInfo info );
    }
}
