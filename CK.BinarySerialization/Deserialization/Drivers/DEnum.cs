using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    class DEnum<T,TU> : ValueTypeDeserializer<T> 
        where T : struct, Enum 
        where TU : struct
    {
        readonly TypedReader<TU> _underlying;

        public DEnum( TypedReader<TU> underlying )
        {
            _underlying = underlying;
        }

        protected override T ReadInstance( IBinaryDeserializer d, TypeReadInfo readInfo )
        {
            var u = _underlying( d, readInfo.ElementTypeReadInfo! );
            return Unsafe.As<TU, T>( ref u );
        }
    }
}
