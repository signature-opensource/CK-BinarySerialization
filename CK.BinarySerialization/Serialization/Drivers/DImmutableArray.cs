using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CK.BinarySerialization.Serialization;

sealed class DImmutableArray<T> : ValueTypeSerializer<ImmutableArray<T>>
{
    readonly ISerializationDriver _item;

    public DImmutableArray( ISerializationDriver item )
    {
        _item = item;
    }

    public override string DriverName => "ImmutableArray";

    public override int SerializationVersion => -1;

    public override SerializationDriverCacheLevel CacheLevel => base.CacheLevel;

    protected internal override void Write( IBinarySerializer s, in ImmutableArray<T> o )
    {
        var a = ImmutableCollectionsMarshal.AsArray( o );
        if( a == null )
        {
            s.Writer.Write( (byte)SerializationMarker.Null );
            return;
        }
        var sA = (BinarySerializerImpl)s;
        if( sA.TrackObject( a ) )
        {
            var item = Unsafe.As<TypedWriter<T>>( _item.TypedWriter );
            s.Writer.Write( (byte)SerializationMarker.ObjectData );
            s.Writer.WriteNonNegativeSmallInt32( a.Length );
            for( int i = 0; i < a.Length; ++i ) item( s, in a[i] );
        }
    }
}
