using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CK.BinarySerialization.Deserialization
{
    sealed class DKeyValuePair<TKey, TValue> : ValueTypeDeserializer<KeyValuePair<TKey, TValue>>
    {
        readonly TypedReader<TKey> _key;
        readonly TypedReader<TValue> _value;

        public DKeyValuePair( IDeserializationDriver k, IDeserializationDriver v )
            : base( k.IsCached && v.IsCached )
        {
            _key = Unsafe.As<TypedReader<TKey>>( k.TypedReader );
            _value = Unsafe.As<TypedReader<TValue>>( v.TypedReader );
        }

        protected override KeyValuePair<TKey, TValue> ReadInstance( IBinaryDeserializer d, ITypeReadInfo info )
        {
            return new KeyValuePair<TKey, TValue>( _key( d, info.SubTypes[0] ), _value( d, info.SubTypes[1] ) );
        }
    }

}
