using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    sealed class DArrayMD<T,TItem> : ReferenceTypeDeserializer<T> where T : class
    {
        readonly TypedReader<TItem> _item;

        public DArrayMD( TypedReader<TItem> item )
        {
            _item = item;
        }

        protected override void ReadInstance( ref RefReader r )
        {
            Debug.Assert( r.ReadInfo.ElementTypeReadInfo != null );
            
            bool isEmpty = false;
            var lengths = new int[r.ReadInfo.ArrayRank];
            for( int i = 0; i < lengths.Length; ++i )
            {
                isEmpty |= (lengths[i] = r.Reader.ReadNonNegativeSmallInt32()) == 0;
            }
            var a = Unsafe.As<T>( Array.CreateInstance( typeof(TItem), lengths ) );
            var d = r.SetInstance( a );
            if( !isEmpty )
            {
                var array = Unsafe.As<Array>( a );
                var coords = new int[lengths.Length];
                coords[coords.Length - 1] = -1;
                while( InternalShared.NextInArray( coords, lengths ) )
                {
                    array.SetValue( _item( d, r.ReadInfo.ElementTypeReadInfo ), coords );
                }
            }
        }
    }
}
