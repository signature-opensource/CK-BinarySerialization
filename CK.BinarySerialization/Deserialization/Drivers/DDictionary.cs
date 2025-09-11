using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization.Deserialization;

sealed class DDictionary<TKey, TValue> : ReferenceTypeDeserializer<Dictionary<TKey, TValue>> where TKey : notnull
{
    readonly TypedReader<TKey> _key;
    readonly TypedReader<TValue> _value;

    static DDictionary()
    {
        // Before, the AssemblyQualifiedName was used.
        // This should be removed once.
        var kOld = typeof( EqualityComparer<TKey> ).AssemblyQualifiedName!;
        SharedDeserializerKnownObject.Default.RegisterKnownKey( kOld, EqualityComparer<TKey>.Default );
    }

    public DDictionary( IDeserializationDriver k, IDeserializationDriver v )
        : base( k.IsCached && v.IsCached )
    {
        _key = Unsafe.As<TypedReader<TKey>>( k.TypedReader );
        _value = Unsafe.As<TypedReader<TValue>>( v.TypedReader );
    }

    protected override void ReadInstance( ref RefReader r )
    {
        Debug.Assert( r.ReadInfo.SubTypes.Count == 2 );
        int len = r.Reader.ReadNonNegativeSmallInt32();
        var (d, dict) = r.SetInstance( d =>
        {
            var comparer = d.ReadNullableObject<IEqualityComparer<TKey>>();
            return new Dictionary<TKey, TValue>( len, comparer );
        } );
        var kInfo = r.ReadInfo.SubTypes[0];
        var vInfo = r.ReadInfo.SubTypes[1];
        while( --len >= 0 )
        {
            dict.Add( _key( d, kInfo ), _value( d, vInfo ) );
        }
    }
}
