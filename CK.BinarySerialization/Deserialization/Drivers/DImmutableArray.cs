using CK.Core;
using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CK.BinarySerialization.Deserialization;

sealed class DImmutableArray<T> : ValueTypeDeserializer<ImmutableArray<T>>
{
    readonly IDeserializationDriver _item;

    public DImmutableArray( IDeserializationDriver item )
        : base( item.IsCached )
    {
        _item = item;
    }

    protected override ImmutableArray<T> ReadInstance( IBinaryDeserializer d, ITypeReadInfo info )
    {
        return ImmutableCollectionsMarshal.AsImmutableArray( ReadTrackedArray( d, info, _item ) );
    }

    internal static T[]? ReadTrackedArray( IBinaryDeserializer d, ITypeReadInfo info, IDeserializationDriver item )
    {
        T[]? a;
        var b = d.Reader.ReadByte();
        if( b == (byte)SerializationMarker.Null )
        {
            a = null;
        }
        else if( b == (byte)SerializationMarker.ObjectRef )
        {
            a = (T[])((BinaryDeserializerImpl)d).ReadObjectRef();
        }
        else
        {
            a = new T[d.Reader.ReadNonNegativeSmallInt32()];
            ((BinaryDeserializerImpl)d).Track( a );

            var rItem = Unsafe.As<TypedReader<T>>( item.TypedReader );
            var t = info.SubTypes[0];
            for( int i = 0; i < a.Length; i++ )
            {
                a[i] = rItem( d, t );
            }
        }

        return a;
    }
}
