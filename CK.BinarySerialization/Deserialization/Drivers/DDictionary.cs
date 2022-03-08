using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    sealed class DDictionary<TKey,TValue> : ReferenceTypeDeserializer<Dictionary<TKey,TValue>> where TKey : notnull
    {
        readonly TypedReader<TKey> _key;
        readonly TypedReader<TValue> _value;

        static DDictionary()
        {
#if NETCOREAPP3_1
            var k = typeof( EqualityComparer<TKey> ).AssemblyQualifiedName!;
            var c = EqualityComparer<TKey>.Default;
            SharedDeserializerKnownObject.Default.RegisterKnownKey( k, c );
#else
            ... ? To be checked in Net6!
            if( typeof( TKey ) != typeof( string ) )
            {
                var k = typeof( EqualityComparer<TKey> ).AssemblyQualifiedName!;
                var c = EqualityComparer<TKey>.Default;
                SharedDeserializerKnownObject.Default.RegisterKnownKey( k, c );
            }
#endif
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
            var (d,dict) = r.SetInstance( d =>
            {
                d.DebugCheckSentinel();
                var comparer = d.ReadObject<IEqualityComparer<TKey>>();
                d.DebugCheckSentinel();
                return new Dictionary<TKey, TValue>( len, comparer );
            } );
            var kInfo = r.ReadInfo.SubTypes[0];
            var vInfo = r.ReadInfo.SubTypes[1];
            while( --len >= 0 )
            {
                d.DebugCheckSentinel();
                dict.Add( _key( d, kInfo ), _value( d, vInfo ) );
                d.DebugCheckSentinel();
            }
            d.DebugCheckSentinel();
        }
    }
}
