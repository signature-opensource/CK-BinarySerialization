using System;

namespace CK.BinarySerialization;

/// <summary>
/// Optional interface that a <see cref="ISerializationDriver"/> can support
/// to change the written type info.
/// </summary>
public interface ISerializationDriverTypeRewriter
{
    /// <summary>
    /// Returns the type that must be written.
    /// </summary>
    /// <param name="type">The actual type.</param>
    /// <returns>The type to write.</returns>
    Type GetTypeToWrite( Type type );
}
