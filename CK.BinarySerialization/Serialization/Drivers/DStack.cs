using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DStack<T> : ReferenceTypeSerializer<Stack<T>>
    {
        readonly TypedWriter<T> _item;

        public DStack( Delegate item ) => _item = Unsafe.As<TypedWriter<T>>( item );

        public override string DriverName => "Stack";

        public override int SerializationVersion => -1;

        internal protected override void Write( IBinarySerializer w, in Stack<T> o )
        {
            w.Writer.WriteNonNegativeSmallInt32( o.Count );
            foreach( var i in o ) _item( w, i );
        }
    }
}
