using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Finds a serializer for a type.
    /// <para>
    /// Not all the resolvers are the same: <see cref="BasicTypesSerializerResolver.Instance"/> relies on 
    /// immutable mappings and is exposed as a singleton, <see cref="SimpleBinarySerializerResolver"/> is 
    /// a pure factory (it doesn't cache its result).
    /// </para>
    /// </summary>
    public interface ISerializerResolver
    {
        /// <summary>
        /// Finds a serialization driver for a type.
        /// The driver's nullability is driven by the type. 
        /// Reference type defaults to nullable (rule of the oblivious nullable context).
        /// </summary>
        /// <param name="context">The serializer context.</param>
        /// <param name="t">The type for which a driver must be found.</param>
        /// <returns>The driver or null.</returns>
        ISerializationDriver? TryFindDriver( BinarySerializerContext context, Type t );
    }
}
