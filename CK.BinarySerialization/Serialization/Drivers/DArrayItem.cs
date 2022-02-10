using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    class DArrayItem<T> : INonNullableSerializationDriver<T[]> where T : notnull
    {
        readonly ISerializationDriver<T> _item;

        public DArrayItem( ISerializationDriver<T> item )
        {
            _item = item;
        }

        public string DriverName => "ArrayItem";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in T[] o )
        {
            w.Writer.WriteNonNegativeSmallInt32( o.Length );
            foreach( var i in o ) _item.WriteData( w, i );
        }
    }
}
