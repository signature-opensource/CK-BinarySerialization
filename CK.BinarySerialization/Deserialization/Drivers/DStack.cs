using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    class DStack<T> : ReferenceTypeDeserializer<Stack<T>>
    {
        readonly TypedReader<T> _item;

        public DStack( TypedReader<T> item )
        {
            _item = item;
        }

        protected override Stack<T> ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo )
        {
            Debug.Assert( readInfo.GenericParameters.Count == 1 );
            int len = r.Reader.ReadNonNegativeSmallInt32();
            var a = ArrayPool<T>.Shared.Rent( len );
            try
            {
                for( int i = 0; i < len; i++ )
                {
                    a[i] = _item( r, readInfo.GenericParameters[0] );
                }
                Array.Reverse( a, 0, len );
                return new Stack<T>( a.Take( len ) );
            }
            finally
            {
                ArrayPool<T>.Shared.Return( a );
            }
        }
    }
}
