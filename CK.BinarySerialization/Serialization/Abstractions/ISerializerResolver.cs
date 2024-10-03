using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization;

/// <summary>
/// Finds a serializer for a type.
/// <para>
/// They are typically exposed as singletons like <see cref="BasicTypesSerializerResolver.Instance"/>
/// since they are purely factories of drivers.
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
