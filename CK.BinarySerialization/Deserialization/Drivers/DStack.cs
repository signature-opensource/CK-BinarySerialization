using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    sealed class DStack<T> : ReferenceTypeDeserializer<Stack<T>>
    {
        readonly TypedReader<T> _item;

        public DStack( TypedReader<T> item )
        {
            _item = item;
        }

        protected override void ReadInstance( ref RefReader r )
        {
            Debug.Assert( r.ReadInfo.GenericParameters.Count == 1 );
            int len = r.Reader.ReadNonNegativeSmallInt32();
            var s = new Stack<T>( len );
            var d = r.SetInstance( s );
            var a = ArrayPool<T>.Shared.Rent( len );
            try
            {
                for( int i = 0; i < len; i++ )
                {
                    a[i] = _item( d, r.ReadInfo.GenericParameters[0] );
                }
                for( int i = len-1; i >= 0; i-- )
                {
                    s.Push( a[i] );
                }
            }
            finally
            {
                ArrayPool<T>.Shared.Return( a );
            }
        }
    }
}
