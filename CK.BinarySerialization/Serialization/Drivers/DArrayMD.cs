using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DArrayMD<T,TItem> : ReferenceTypeSerializer<T> where T : class
    {
        readonly TypedWriter<TItem> _item;

        public DArrayMD( Delegate item ) => _item = Unsafe.As<TypedWriter<TItem>>( item );

        public override string DriverName => "Array";

        public override int SerializationVersion => -1;

        internal protected override void Write( IBinarySerializer s, in T o )
        {
            Array a = Unsafe.As<Array>(o);
            // Rank is in the TypeReadInfo. No need to write it here.
            bool isEmpty = false;
            var lengths = new int[a.Rank];
            for( int i = 0; i < lengths.Length; ++i )
            {
                var l = a.GetLength( i );
                isEmpty |= l == 0;
                lengths[i] = l;
                s.Writer.WriteNonNegativeSmallInt32( l );
            }
            if( !isEmpty )
            {
                var coords = new int[a.Rank];
                coords[coords.Length - 1] = -1;
                while( BinarySerializerImpl.NextInArray( coords, lengths ) )
                {
                    var i = (TItem)a.GetValue( coords )!;
                    _item( s, in i );
                }
            }
        }
        
    }
}
