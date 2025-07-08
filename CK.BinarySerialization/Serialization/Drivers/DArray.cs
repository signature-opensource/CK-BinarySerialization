using System.Runtime.CompilerServices;

namespace CK.BinarySerialization.Serialization;

sealed class DArray<T> : ReferenceTypeSerializer<T[]>
{
    readonly ISerializationDriver _item;

    public DArray( ISerializationDriver item )
    {
        _item = item;
    }

    public override string DriverName => "Array";

    public override int SerializationVersion => -1;

    public override SerializationDriverCacheLevel CacheLevel => _item.CacheLevel;

    internal protected override void Write( IBinarySerializer s, in T[] o )
    {
        var item = Unsafe.As<TypedWriter<T>>( _item.TypedWriter );
        s.Writer.WriteNonNegativeSmallInt32( o.Length );
        for( int i = 0; i < o.Length; ++i ) item( s, in o[i] );
    }
}
