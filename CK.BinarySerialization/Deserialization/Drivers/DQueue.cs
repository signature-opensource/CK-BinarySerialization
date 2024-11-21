using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Deserialization;

sealed class DQueue<T> : ReferenceTypeDeserializer<Queue<T>>
{
    readonly TypedReader<T> _item;

    public DQueue( IDeserializationDriver item )
        : base( item.IsCached )
    {
        _item = Unsafe.As<TypedReader<T>>( item.TypedReader );
    }

    protected override void ReadInstance( ref RefReader r )
    {
        Debug.Assert( r.ReadInfo.SubTypes.Count == 1 );
        int len = r.Reader.ReadNonNegativeSmallInt32();
        var a = new Queue<T>( len );
        var d = r.SetInstance( a );
        while( --len >= 0 )
        {
            a.Enqueue( _item( d, r.ReadInfo.SubTypes[0] ) );
        }
    }
}
