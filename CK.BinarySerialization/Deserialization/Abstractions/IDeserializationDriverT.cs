using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Deserialization driver that knows how to instantiate an instance of a <see cref="ResolvedType"/> 
    /// and initializes it from a <see cref="IBinaryDeserializer"/> or handles null.
    /// </summary>
    public interface IDeserializationDriver< out T> : IDeserializationDriver where T : notnull
    {
        /// <summary>
        /// Gets the driver that reads a nullable <typeparamref name="T"/>.
        /// </summary>
        new INullableDeserializationDriver<T> ToNullable { get; }

        /// <summary>
        /// Gets the driver that reads a non nullable <typeparamref name="T"/>.
        /// </summary>
        new INonNullableDeserializationDriver<T> ToNonNullable { get; }
    }
}
