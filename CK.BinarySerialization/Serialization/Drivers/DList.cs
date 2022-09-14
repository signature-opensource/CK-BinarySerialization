using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DList<T> : ReferenceTypeSerializer<List<T>>
    {
        readonly TypedWriter<T> _item;

        public DList( Delegate item, SerializationDriverCacheLevel cache )
        {
            _item = Unsafe.As<TypedWriter<T>>( item );
            CacheLevel = cache;
        }

        public override string DriverName => "List";

        public override int SerializationVersion => -1;

        public override SerializationDriverCacheLevel CacheLevel { get; }

        internal protected override void Write( IBinarySerializer s, in List<T> o )
        {
            s.Writer.WriteNonNegativeSmallInt32( o.Count );
            for( int i = 0; i < o.Count; ++i ) _item( s, o[i] );
        }
    }
}
