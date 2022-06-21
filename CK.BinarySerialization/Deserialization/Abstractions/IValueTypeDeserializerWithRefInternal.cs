using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// This internal marker interface is supported by the generic <see cref="ValueTypeDeserializerWithRef{T}"/>
    /// just to be able test its type without generic analysis.
    /// </summary>
    interface IValueTypeDeserializerWithRefInternal : IDeserializationDriverInternal
    {
        /// <summary>
        /// Special "class to struct mutation" reader used to handle deferred deserialization of value type that 
        /// was a reference type and has been chosen as a deferred one.
        /// <para>
        /// We cannot use the regular <see cref="IDeserializationDriverInternal.ReadObjectData(IBinaryDeserializer, ITypeReadInfo)"/> 
        /// since it calls <see cref="ValueTypeDeserializerWithRef{T}.ReadInstanceAndTrack(IBinaryDeserializer, ITypeReadInfo)"/>
        /// and in this case, the value is either in the reader's deferredValueQueue (second pass) and this is only used to skip the binary layout
        /// or a fake unitialized instance is already tracked (first pass and the read value will be enqueued in the deferredValueQueue).
        /// </para>
        /// </summary>
        /// <param name="d">The deserializer.</param>
        /// <param name="readInfo">The read info.</param>
        /// <returns>The read value.</returns>
        object ReadRawObjectData( IBinaryDeserializer d, ITypeReadInfo readInfo );
    }
}
