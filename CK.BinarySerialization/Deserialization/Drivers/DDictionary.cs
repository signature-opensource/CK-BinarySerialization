using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    sealed class DDictionary<TKey,TValue> : ReferenceTypeDeserializer<Dictionary<TKey,TValue>> where TKey : notnull
    {
        readonly TypedReader<TKey> _key;
        readonly TypedReader<TValue> _value;

        public DDictionary( TypedReader<TKey> k, TypedReader<TValue> v )
        {
            _key = k;
            _value = v;
        }

        protected override void ReadInstance( ref RefReader r )
        {
            Debug.Assert( r.ReadInfo.GenericParameters.Count == 2 );
            int len = r.Reader.ReadNonNegativeSmallInt32();
            var a = new Dictionary<TKey,TValue>( len );
            var d = r.SetInstance( a );
            while( --len >= 0 )
            {
                a.Add( _key( d, r.ReadInfo.GenericParameters[0] ), _value( d, r.ReadInfo.GenericParameters[1] ) );
            }
        }
    }
}
