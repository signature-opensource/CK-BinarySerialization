using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    class DArray<T> : ReferenceTypeSerializer<T[]>
    {
        readonly TypedWriter<T> _item;

        public DArray( TypedWriter<T> item ) => _item = item;

        public override string DriverName => "Array";

        public override int SerializationVersion => -1;

        internal protected override void Write( IBinarySerializer w, in T[] o )
        {
            w.Writer.WriteNonNegativeSmallInt32( o.Length );
            for( int i = 0; i < o.Length; ++i ) _item( w, in o[i] );
        }
    }
}
