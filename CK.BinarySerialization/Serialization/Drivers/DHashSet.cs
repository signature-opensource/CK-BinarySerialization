using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DHashSet<T> : ReferenceTypeSerializer<HashSet<T>>
    {
        readonly TypedWriter<T> _item;

        public DHashSet( Delegate item ) => _item = Unsafe.As<TypedWriter<T>>( item );

        public override string DriverName => "Set";

        public override int SerializationVersion => -1;

        internal protected override void Write( IBinarySerializer s, in HashSet<T> o )
        {
            s.Writer.WriteNonNegativeSmallInt32( o.Count );
            foreach( var i in o ) _item( s, i );
        }
    }
}
