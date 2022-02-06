using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Strongly typed serialization driver.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    public interface ITypeSerializationDriver<T> : ITypeSerializationDriver where T : notnull
    {
        /// <summary>
        /// Writes the object's data.
        /// </summary>
        /// <param name="w">The serializer.</param>
        /// <param name="o">The object instance.</param>
        void WriteData( IBinarySerializer w, in T o );

        void ITypeSerializationDriver.WriteData( IBinarySerializer w, object o ) => WriteData( w, (T)o );
    }
}
