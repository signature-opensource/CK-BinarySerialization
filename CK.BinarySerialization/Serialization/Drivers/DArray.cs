using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DArray<T> : ReferenceTypeSerializer<T[]>
    {
        readonly TypedWriter<T> _item;

        public DArray( Delegate item ) => _item = Unsafe.As<TypedWriter<T>>( item );

        public override string DriverName => "Array";

        public override int SerializationVersion => -1;

        internal protected override void Write( IBinarySerializer s, in T[] o )
        {
            s.Writer.WriteNonNegativeSmallInt32( o.Length );
            for( int i = 0; i < o.Length; ++i ) _item( s, in o[i] );
        }
    }
}
