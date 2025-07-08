using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization.Serialization;

sealed class DKeyValuePair<TKey, TValue> : ValueTypeSerializer<KeyValuePair<TKey, TValue>>
{
    readonly TypedWriter<TKey> _key;
    readonly TypedWriter<TValue> _value;

    public DKeyValuePair( Delegate k, Delegate v, SerializationDriverCacheLevel cache )
    {
        _key = Unsafe.As<TypedWriter<TKey>>( k );
        _value = Unsafe.As<TypedWriter<TValue>>( v );
        CacheLevel = cache;
    }

    public override string DriverName => "KeyValuePair";

    public override int SerializationVersion => -1;

    public override SerializationDriverCacheLevel CacheLevel { get; }

    internal protected override void Write( IBinarySerializer s, in KeyValuePair<TKey, TValue> o )
    {
        _key( s, o.Key );
        _value( s, o.Value );
    }
}
