using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    class DQueue<T> : ReferenceTypeDeserializer<Queue<T>>
    {
        readonly TypedReader<T> _item;

        public DQueue( TypedReader<T> item )
        {
            _item = item;
        }

        protected override Queue<T> ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo )
        {
            Debug.Assert( readInfo.GenericParameters.Count == 1 );
            int len = r.Reader.ReadNonNegativeSmallInt32();
            var a = new Queue<T>( len );
            while( --len >= 0 )
            {
                a.Enqueue( _item( r, readInfo.GenericParameters[0] ) );
            }
            return a;
        }
    }
}
