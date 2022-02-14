using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DEnum<T, TU> : ValueTypeSerializer<T> where T : struct, Enum where TU : struct
    {
        readonly TypedWriter<TU> _underlying;

        public DEnum( TypedWriter<TU> u )
        {
            _underlying = u;
        }

        public override string DriverName => "Enum";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in T o )
        {
            var c = o;
            _underlying( w, Unsafe.As<T, TU>( ref c ) );
        }

    }
}
