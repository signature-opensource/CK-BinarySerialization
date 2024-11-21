using CK.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Serialization;

sealed class DDictionary<TKey, TValue> : ReferenceTypeSerializer<Dictionary<TKey, TValue>> where TKey : notnull
{
    readonly TypedWriter<TKey> _key;
    readonly TypedWriter<TValue> _value;

    public DDictionary( Delegate k, Delegate v, SerializationDriverCacheLevel cache )
    {
        _key = Unsafe.As<TypedWriter<TKey>>( k );
        _value = Unsafe.As<TypedWriter<TValue>>( v );
        CacheLevel = cache;
    }

    public override string DriverName => "Dictionary";

    public override int SerializationVersion => -1;

    public override SerializationDriverCacheLevel CacheLevel { get; }

    internal protected override void Write( IBinarySerializer s, in Dictionary<TKey, TValue> o )
    {
        s.Writer.WriteNonNegativeSmallInt32( o.Count );
        var cmp = o.Comparer;
        s.WriteNullableObject( cmp == EqualityComparer<TKey>.Default ? null : cmp );
        foreach( var kv in o )
        {
            _key( s, kv.Key );
            _value( s, kv.Value );
        }
    }
}
