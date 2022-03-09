using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DTuple<T> : ReferenceTypeSerializer<T> where T : class, ITuple
    {
        readonly UntypedWriter[] _items;

        public DTuple( Delegate[] items ) => _items = Unsafe.As<UntypedWriter[]>( items );

        public override string DriverName => "Tuple";

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
