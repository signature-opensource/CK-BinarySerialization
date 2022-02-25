using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Deserialization driver that knows how to instantiate an instance of a <see cref="ResolvedType"/> 
    /// and initializes it from a <see cref="IBinaryDeserializer"/> or handles null thanks to its 2 drivers.
    /// </summary>
    public interface IDeserializationDriver
    {
        /// <summary>
        /// Gets the type that this drivers is able to resolve.
        /// </summary>
        Type ResolvedType { get; }

        /// <summary>
        /// Gets a <see cref="TypedReader{T}"/> for this type and nullability.
        /// </summary>
        Delegate TypedReader { get; }

        /// <summary>
        /// Gets the nullable driver.
        /// </summary>
        IDeserializationDriver ToNullable { get; }
        
        /// <summary>
        /// Gets the non nullable driver.
        /// </summary>
        IDeserializationDriver ToNonNullable { get; }
    }
}
