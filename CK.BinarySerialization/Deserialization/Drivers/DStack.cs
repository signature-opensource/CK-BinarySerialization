using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Deserialization;

sealed class DStack<T> : ReferenceTypeDeserializer<Stack<T>>
{
    readonly TypedReader<T> _item;

    public DStack( IDeserializationDriver item )
        : base( item.IsCached )
    {
        _item = Unsafe.As<TypedReader<T>>( item.TypedReader );
    }

    protected override void ReadInstance( ref RefReader r )
    {
        Debug.Assert( r.ReadInfo.SubTypes.Count == 1 );
        int len = r.Reader.ReadNonNegativeSmallInt32();
        var s = new Stack<T>( len );
        var d = r.SetInstance( s );
        var a = ArrayPool<T>.Shared.Rent( len );
        try
        {
            for( int i = 0; i < len; i++ )
            {
                a[i] = _item( d, r.ReadInfo.SubTypes[0] );
            }
            for( int i = len - 1; i >= 0; i-- )
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
