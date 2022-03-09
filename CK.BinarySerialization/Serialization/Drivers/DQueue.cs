using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DQueue<T> : ReferenceTypeSerializer<Queue<T>>
    {
        readonly TypedWriter<T> _item;

        public DQueue( Delegate item ) => _item = Unsafe.As<TypedWriter<T>>( item );

        public override string DriverName => "Queue";

        public override int SerializationVersion => -1;

        internal protected override void Write( IBinarySerializer s, in Queue<T> o )
        {
            s.Writer.WriteNonNegativeSmallInt32( o.Count );
            foreach( var i in o ) _item( s, i );
        }
    }
}
