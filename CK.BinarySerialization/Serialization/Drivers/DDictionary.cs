﻿using System;
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

        internal protected override void Write( IBinarySerializer s, in Dictionary<TKey, TValue> o )
        {
            s.Writer.WriteNonNegativeSmallInt32( o.Count );
            s.DebugWriteSentinel();
            s.WriteObject( o.Comparer );
            s.DebugWriteSentinel();
            foreach( var kv in o )
            {
                s.DebugWriteSentinel();
                _key( s, kv.Key );
                _value( s, kv.Value );
                s.DebugWriteSentinel();
            }
            s.DebugWriteSentinel();
        }
    }
}
