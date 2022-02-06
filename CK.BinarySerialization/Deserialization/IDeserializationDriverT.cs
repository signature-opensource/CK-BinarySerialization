using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Handles deserialization from a type's assembly qualified name.
    /// </summary>
    public interface IDeserializationDriver<out T> where T : notnull
    {
        /// <summary>
        /// Reads the data and instantiates a new object.
        /// </summary>
        /// <param name="r">The deserializer.</param>
        /// <param name="readInfo">The type information read.</param>
        /// <returns>The new instance.</returns>
        T ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo );

    }
}
