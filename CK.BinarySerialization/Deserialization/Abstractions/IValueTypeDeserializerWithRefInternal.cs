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
    }
}
