using System;
using System.Globalization;

namespace CK.BinarySerialization.Deserialization
{
    sealed class DChangeBasicType<T,TRead> : ValueTypeDeserializer<T> 
        where T : struct
        where TRead : struct
    {
        readonly TypedReader<TRead> _r;
        readonly TypeCode _target;

        public DChangeBasicType( TypedReader<TRead> r, TypeCode target )
            : base( false )
        {
            _r = r;
            _target = target;
        }

        protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
        {
            TRead v = _r( d, readInfo );
            return (T)Convert.ChangeType( v, _target, CultureInfo.InvariantCulture );
        }
    }
}
