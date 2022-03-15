using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Handles deserialization of object in two phases: an unitialized instance of the 
    /// type is created calling <see cref="System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(Type)"/>), 
    /// then, in a second step, the unitialized instance can be deserialized.
    /// <para>
    /// This should be implemented for reference types that can support this kind of in-place initialization
    /// since this is used to break too deep recursion when a long linked list of objects is serialized.
    /// </para>
    /// <para>
    /// The <see cref="ISerializationDriverAllowDeferredRead"/> marker interface should be used
    /// on the serialization driver to allow this behavior.
    /// </para>
    /// </summary>
    public interface IDeserializationDeferredDriver
    {
        /// <summary>
        /// Deserializes an already allocated object.
        /// </summary>
        /// <param name="d">The deserializer.</param>
        /// <param name="readInfo">
        /// The read information of the type as it has been written.
        /// If type based serialization has been used (with versions and ancestors). 
        /// </param>
        /// <param name="o">The object to fill.</param>
        void ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo, object o );

    }
}
