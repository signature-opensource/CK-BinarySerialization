using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CK.BinarySerialization.Deserialization;

sealed class DArray<T> : ReferenceTypeDeserializer<T[]>
{
    readonly IDeserializationDriver _item;

    public DArray( IDeserializationDriver item )
        : base( item.IsCached )
    {
        _item = item;
    }

    protected override void ReadInstance( ref RefReader r )
    {
        Throw.DebugAssert( r.ReadInfo.SubTypes.Count == 1 );
        if( r.ReadInfo.DriverName == "ImmutableArray" )
        {
            // Mutating from ImmutableArray.
            var inner = DImmutableArray<T>.ReadTrackedArray( r.DangerousDeserializer, r.ReadInfo, _item );
            // Since info.IsValueType is true, the object is not tracked again.
            // A default ImmutableArray has a null inner array.
            // The current contract is that ReadInstance must return a non null instance
            // (this may be changed in the future to accomodate such mutations).
            // However, here, as a ImmutableArray is a struct and its default is
            // barely used, it is a good idea to consider that this mutation produces
            // a non null but empty array.
            r.SetInstance( inner ?? Array.Empty<T>() );
            return;
        }
        var item = Unsafe.As<TypedReader<T>>( _item.TypedReader );
        var a = new T[r.Reader.ReadNonNegativeSmallInt32()];
        var d = r.SetInstance( a );
        var t = r.ReadInfo.SubTypes[0];
        for( int i = 0; i < a.Length; i++ )
        {
            a[i] = item( d, t );
        }
    }
}
