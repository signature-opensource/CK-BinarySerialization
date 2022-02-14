using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DList<T> : ReferenceTypeSerializer<List<T>>
    {
        readonly TypedWriter<T> _item;

        public DList( TypedWriter<T> item ) => _item = item;

        public override string DriverName => "List";

        public override int SerializationVersion => -1;

        internal protected override void Write( IBinarySerializer w, in List<T> o )
        {
            w.Writer.WriteNonNegativeSmallInt32( o.Count );
            for( int i = 0; i < o.Count; ++i ) _item( w, o[i] );
        }
    }
}
