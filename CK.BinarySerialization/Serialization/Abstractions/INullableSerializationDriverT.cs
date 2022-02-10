using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Typed deserialization driver that knows how to instantiate an instance of a <typeparamref name="T"/>
    /// that can be null and initializes it from a <see cref="IBinaryDeserializer"/>.
    /// </summary>
    public interface INullableSerializationDriver<T> : ISerializationDriver<T>, INullableSerializationDriver where T : notnull
    {
        /// <summary>
        /// Writes a potentially null instance.
        /// </summary>
        /// <param name="w">The serializer.</param>
        /// <param name="o">The nullable instance.</param>
        void WriteNullable( IBinarySerializer w, in T? o );
    }
}
