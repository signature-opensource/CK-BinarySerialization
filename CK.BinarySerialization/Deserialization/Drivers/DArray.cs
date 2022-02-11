using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    class DArray<T> : ReferenceTypeDeserializer<T[]>
    {
        readonly TypedReader<T> _item;

        public DArray( TypedReader<T> item )
        {
            _item = item;
        }

        protected override T[] ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo )
        {
            Debug.Assert( readInfo.ElementTypeReadInfo != null );
            var a = new T[r.Reader.ReadNonNegativeSmallInt32()];
            for( int i = 0; i < a.Length; i++ )
            {
                a[i] = _item( r, readInfo.ElementTypeReadInfo );
            }
            return a;
        }
    }
}
