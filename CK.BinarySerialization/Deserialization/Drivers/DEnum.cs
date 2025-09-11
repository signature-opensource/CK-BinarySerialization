using System;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization.Deserialization;

class DEnum<T, TU> : ValueTypeDeserializer<T>
    where T : struct, Enum
    where TU : struct
{
    readonly TypedReader<TU> _underlying;

    public DEnum( TypedReader<TU> underlying )
    {
        _underlying = underlying;
    }

    protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
    {
        var u = _underlying( d, readInfo.SubTypes[0] );
        return Unsafe.As<TU, T>( ref u );
    }
}
