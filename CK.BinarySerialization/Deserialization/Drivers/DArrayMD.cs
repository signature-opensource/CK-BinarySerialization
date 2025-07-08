using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization.Deserialization;

sealed class DArrayMD<T, TItem> : ReferenceTypeDeserializer<T> where T : class
{
    readonly TypedReader<TItem> _item;

    public DArrayMD( IDeserializationDriver item )
        : base( item.IsCached )
    {
        _item = Unsafe.As<TypedReader<TItem>>( item.TypedReader );
    }

    protected override void ReadInstance( ref RefReader r )
    {
        Debug.Assert( r.ReadInfo.SubTypes.Count == 1 );

        bool isEmpty = false;
        var lengths = new int[r.ReadInfo.ArrayRank];
        for( int i = 0; i < lengths.Length; ++i )
        {
            isEmpty |= (lengths[i] = r.Reader.ReadNonNegativeSmallInt32()) == 0;
        }
        var a = Unsafe.As<T>( Array.CreateInstance( typeof( TItem ), lengths ) );
        var d = r.SetInstance( a );
        if( !isEmpty )
        {
            var array = Unsafe.As<Array>( a );
            var coords = new int[lengths.Length];
            coords[coords.Length - 1] = -1;
            while( BinarySerializerImpl.NextInArray( coords, lengths ) )
            {
                array.SetValue( _item( d, r.ReadInfo.SubTypes[0] ), coords );
            }
        }
    }

}
