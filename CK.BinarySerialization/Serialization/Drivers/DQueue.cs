using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DQueue<T> : ReferenceTypeSerializer<Queue<T>>
    {
        readonly TypedWriter<T> _item;

        public DQueue( TypedWriter<T> item ) => _item = item;

        public override string DriverName => "Queue";

        public override int SerializationVersion => -1;

        internal protected override void Write( IBinarySerializer w, in Queue<T> o )
        {
            w.Writer.WriteNonNegativeSmallInt32( o.Count );
            foreach( var i in o ) _item( w, i );
        }
    }
}
