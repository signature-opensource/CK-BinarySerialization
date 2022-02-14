using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    sealed class DArray<T> : ReferenceTypeDeserializer<T[]>
    {
        readonly TypedReader<T> _item;

        public DArray( TypedReader<T> item )
        {
            _item = item;
        }

        protected override void ReadInstance( ref RefReader r )
        {
            Debug.Assert( r.ReadInfo.ElementTypeReadInfo != null );
            var a = new T[r.Reader.ReadNonNegativeSmallInt32()];
            var d = r.SetInstance( a );
            for( int i = 0; i < a.Length; i++ )
            {
                a[i] = _item( d, r.ReadInfo.ElementTypeReadInfo );
            }
        }
    }
}
