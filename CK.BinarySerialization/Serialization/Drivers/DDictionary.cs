using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DDictionary<TKey,TValue> : ReferenceTypeSerializer<Dictionary<TKey,TValue>> where TKey : notnull
    {
        readonly TypedWriter<TKey> _key;
        readonly TypedWriter<TValue> _value;

        static DDictionary()
        {
            var k = typeof( EqualityComparer<TKey> ).AssemblyQualifiedName!;
            var c = EqualityComparer<TKey>.Default;
            SharedSerializerKnownObject.Default.RegisterKnownObject( c, k );
        }

        public DDictionary( TypedWriter<TKey> k, TypedWriter<TValue> v ) => (_key, _value) = (k, v);

        public override string DriverName => "Dictionary";

        public override int SerializationVersion => -1;

        internal protected override void Write( IBinarySerializer w, in Dictionary<TKey, TValue> o )
        {
            w.Writer.WriteNonNegativeSmallInt32( o.Count );
            w.WriteObject( o.Comparer );
            foreach( var kv in o )
            {
                _key( w, kv.Key );
                _value( w, kv.Value );
            }
        }
    }
}
