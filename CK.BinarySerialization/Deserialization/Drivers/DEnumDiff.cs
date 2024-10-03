using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Deserialization;

class DEnumDiff<T, TLU, TU> : ValueTypeDeserializer<T>
    where T : struct
    where TLU : struct
    where TU : struct
{
    readonly TypedReader<TU> _underlying;

    public DEnumDiff( TypedReader<TU> underlying )
        : base( false )
    {
        _underlying = underlying;
    }

    protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
    {
        TU u = _underlying( d, readInfo.SubTypes[0] );
        return (T)Convert.ChangeType( u, typeof( TLU ) );
    }
}
