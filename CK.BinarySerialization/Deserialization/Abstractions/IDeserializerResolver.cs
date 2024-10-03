using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization;

/// <summary>
/// Bag for deserializers that tries to resolve a <see cref="IDeserializationDriver"/> from 
/// a non nullable <see cref="ITypeReadInfo"/> that must have a <see cref="ITypeReadInfo.ResolveLocalType()"/>,
/// a driver name and that has not yet a resolved driver.
/// </summary>
public interface IDeserializerResolver
{
    /// <summary>
    /// Tries to find a deserialization driver for a type information.
    /// </summary>
    /// <param name="info">The safe type information to resolve.</param>
    /// <returns>The deserialization driver or null.</returns>
    IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info );
}
