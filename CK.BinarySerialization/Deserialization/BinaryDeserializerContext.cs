using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    class BinaryDeserializerContext
    {
        readonly Dictionary<string,object> _knownObjects;
        readonly ISerializerResolver? _backSerializer;
        readonly IDeserializerKnownObject? _backKnownObject;
        bool _inUse;


        internal void Acquire()
        {
            if( _inUse )
            {
                throw new InvalidOperationException( "This BinaryDeserializerContext is already used by an existing BinaryDeserializer. The existing BinaryDeserializer must be disposed first." );
            }
            _inUse = true;
        }

        internal void Release()
        {
            _inUse = false;
        }

    }
}
