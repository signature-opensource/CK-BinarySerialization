using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DValueTuple<T> : ValueTypeSerializer<T> where T : struct, ITuple
    {
        readonly UntypedWriter[] _items;

        public DValueTuple( Delegate[] items ) => _items = Unsafe.As<UntypedWriter[]>( items );

        public override string DriverName => "ValueTuple";

        public override int SerializationVersion => -1;

        internal protected override void Write( IBinarySerializer s, in T o )
        {
            for( int i = 0; i < o.Length; ++i )
            {
                _items[i]( s, o[i]! );
            }
        }
    }
}
