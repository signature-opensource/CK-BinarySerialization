using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    class DList<T> : ReferenceTypeDeserializer<List<T>>
    {
        readonly TypedReader<T> _item;

        public DList( TypedReader<T> item )
        {
            _item = item;
        }

        protected override List<T> ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo )
        {
            Debug.Assert( readInfo.GenericParameters.Count == 1 );
            int len = r.Reader.ReadNonNegativeSmallInt32();
            var a = new List<T>( len );
            while( --len >= 0 )
            {
                a.Add( _item( r, readInfo.GenericParameters[0] ) );
            }
            return a;
        }
    }
}
