using System;

namespace CK.BinarySerialization
{
    sealed class SlicedReferenceDeserializer<T> : ReferenceTypeDeserializer<T> where T : class
    {
        protected override void ReadInstance( ref RefReader r )
        {
            throw new NotImplementedException();
        }
    }
}
