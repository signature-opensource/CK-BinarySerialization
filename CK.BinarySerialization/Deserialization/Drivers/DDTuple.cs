

using CK.BinarySerialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization.Deserialization
{
    sealed class DTuple<T1> : ReferenceTypeDeserializer<Tuple<T1>>
    {
        readonly TypedReader<T1> _item1;

        public DTuple( Delegate[] d, bool isCached )
            : base( isCached )
        {
            _item1 = Unsafe.As<TypedReader<T1>>( d[0] );
        }

        protected override void ReadInstance( ref RefReader r )
        {
            Debug.Assert( r.ReadInfo.SubTypes.Count == 1 );
            var a = (Tuple<T1>)RuntimeHelpers.GetUninitializedObject( typeof( Tuple<T1> ) );
            var d = r.SetInstance( a );
            var parameters = new object?[]
            {
               _item1( d, r.ReadInfo.SubTypes[0] )
            };
            typeof( Tuple<T1> ).GetConstructors()[0].Invoke( a, parameters );
        }
    }

    sealed class DValueTuple<T1> : ValueTypeDeserializer<ValueTuple<T1>>
    {
        readonly TypedReader<T1> _item1;

        public DValueTuple( Delegate[] d, bool isCached )
            : base( isCached )
        {
            _item1 = Unsafe.As<TypedReader<T1>>( d[0] );
        }

        protected override ValueTuple<T1> ReadInstance( IBinaryDeserializer d, ITypeReadInfo info )
        {
            return new ValueTuple<T1>( _item1( d, info.SubTypes[0] ) );
        }
    }
    sealed class DTuple<T1, T2> : ReferenceTypeDeserializer<Tuple<T1, T2>>
    {
        readonly TypedReader<T1> _item1;
readonly TypedReader<T2> _item2;

        public DTuple( Delegate[] d, bool isCached )
            : base( isCached )
        {
            _item1 = Unsafe.As<TypedReader<T1>>( d[0] );
_item2 = Unsafe.As<TypedReader<T2>>( d[1] );
        }

        protected override void ReadInstance( ref RefReader r )
        {
            Debug.Assert( r.ReadInfo.SubTypes.Count == 2 );
            var a = (Tuple<T1, T2>)RuntimeHelpers.GetUninitializedObject( typeof( Tuple<T1, T2> ) );
            var d = r.SetInstance( a );
            var parameters = new object?[]
            {
               _item1( d, r.ReadInfo.SubTypes[0] ),
_item2( d, r.ReadInfo.SubTypes[1] )
            };
            typeof( Tuple<T1, T2> ).GetConstructors()[0].Invoke( a, parameters );
        }
    }

    sealed class DValueTuple<T1, T2> : ValueTypeDeserializer<ValueTuple<T1, T2>>
    {
        readonly TypedReader<T1> _item1;
readonly TypedReader<T2> _item2;

        public DValueTuple( Delegate[] d, bool isCached )
            : base( isCached )
        {
            _item1 = Unsafe.As<TypedReader<T1>>( d[0] );
_item2 = Unsafe.As<TypedReader<T2>>( d[1] );
        }

        protected override ValueTuple<T1, T2> ReadInstance( IBinaryDeserializer d, ITypeReadInfo info )
        {
            return new ValueTuple<T1, T2>( _item1( d, info.SubTypes[0] ),
_item2( d, info.SubTypes[1] ) );
        }
    }
    sealed class DTuple<T1, T2, T3> : ReferenceTypeDeserializer<Tuple<T1, T2, T3>>
    {
        readonly TypedReader<T1> _item1;
readonly TypedReader<T2> _item2;
readonly TypedReader<T3> _item3;

        public DTuple( Delegate[] d, bool isCached )
            : base( isCached )
        {
            _item1 = Unsafe.As<TypedReader<T1>>( d[0] );
_item2 = Unsafe.As<TypedReader<T2>>( d[1] );
_item3 = Unsafe.As<TypedReader<T3>>( d[2] );
        }

        protected override void ReadInstance( ref RefReader r )
        {
            Debug.Assert( r.ReadInfo.SubTypes.Count == 3 );
            var a = (Tuple<T1, T2, T3>)RuntimeHelpers.GetUninitializedObject( typeof( Tuple<T1, T2, T3> ) );
            var d = r.SetInstance( a );
            var parameters = new object?[]
            {
               _item1( d, r.ReadInfo.SubTypes[0] ),
_item2( d, r.ReadInfo.SubTypes[1] ),
_item3( d, r.ReadInfo.SubTypes[2] )
            };
            typeof( Tuple<T1, T2, T3> ).GetConstructors()[0].Invoke( a, parameters );
        }
    }

    sealed class DValueTuple<T1, T2, T3> : ValueTypeDeserializer<ValueTuple<T1, T2, T3>>
    {
        readonly TypedReader<T1> _item1;
readonly TypedReader<T2> _item2;
readonly TypedReader<T3> _item3;

        public DValueTuple( Delegate[] d, bool isCached )
            : base( isCached )
        {
            _item1 = Unsafe.As<TypedReader<T1>>( d[0] );
_item2 = Unsafe.As<TypedReader<T2>>( d[1] );
_item3 = Unsafe.As<TypedReader<T3>>( d[2] );
        }

        protected override ValueTuple<T1, T2, T3> ReadInstance( IBinaryDeserializer d, ITypeReadInfo info )
        {
            return new ValueTuple<T1, T2, T3>( _item1( d, info.SubTypes[0] ),
_item2( d, info.SubTypes[1] ),
_item3( d, info.SubTypes[2] ) );
        }
    }
    sealed class DTuple<T1, T2, T3, T4> : ReferenceTypeDeserializer<Tuple<T1, T2, T3, T4>>
    {
        readonly TypedReader<T1> _item1;
readonly TypedReader<T2> _item2;
readonly TypedReader<T3> _item3;
readonly TypedReader<T4> _item4;

        public DTuple( Delegate[] d, bool isCached )
            : base( isCached )
        {
            _item1 = Unsafe.As<TypedReader<T1>>( d[0] );
_item2 = Unsafe.As<TypedReader<T2>>( d[1] );
_item3 = Unsafe.As<TypedReader<T3>>( d[2] );
_item4 = Unsafe.As<TypedReader<T4>>( d[3] );
        }

        protected override void ReadInstance( ref RefReader r )
        {
            Debug.Assert( r.ReadInfo.SubTypes.Count == 4 );
            var a = (Tuple<T1, T2, T3, T4>)RuntimeHelpers.GetUninitializedObject( typeof( Tuple<T1, T2, T3, T4> ) );
            var d = r.SetInstance( a );
            var parameters = new object?[]
            {
               _item1( d, r.ReadInfo.SubTypes[0] ),
_item2( d, r.ReadInfo.SubTypes[1] ),
_item3( d, r.ReadInfo.SubTypes[2] ),
_item4( d, r.ReadInfo.SubTypes[3] )
            };
            typeof( Tuple<T1, T2, T3, T4> ).GetConstructors()[0].Invoke( a, parameters );
        }
    }

    sealed class DValueTuple<T1, T2, T3, T4> : ValueTypeDeserializer<ValueTuple<T1, T2, T3, T4>>
    {
        readonly TypedReader<T1> _item1;
readonly TypedReader<T2> _item2;
readonly TypedReader<T3> _item3;
readonly TypedReader<T4> _item4;

        public DValueTuple( Delegate[] d, bool isCached )
            : base( isCached )
        {
            _item1 = Unsafe.As<TypedReader<T1>>( d[0] );
_item2 = Unsafe.As<TypedReader<T2>>( d[1] );
_item3 = Unsafe.As<TypedReader<T3>>( d[2] );
_item4 = Unsafe.As<TypedReader<T4>>( d[3] );
        }

        protected override ValueTuple<T1, T2, T3, T4> ReadInstance( IBinaryDeserializer d, ITypeReadInfo info )
        {
            return new ValueTuple<T1, T2, T3, T4>( _item1( d, info.SubTypes[0] ),
_item2( d, info.SubTypes[1] ),
_item3( d, info.SubTypes[2] ),
_item4( d, info.SubTypes[3] ) );
        }
    }
    sealed class DTuple<T1, T2, T3, T4, T5> : ReferenceTypeDeserializer<Tuple<T1, T2, T3, T4, T5>>
    {
        readonly TypedReader<T1> _item1;
readonly TypedReader<T2> _item2;
readonly TypedReader<T3> _item3;
readonly TypedReader<T4> _item4;
readonly TypedReader<T5> _item5;

        public DTuple( Delegate[] d, bool isCached )
            : base( isCached )
        {
            _item1 = Unsafe.As<TypedReader<T1>>( d[0] );
_item2 = Unsafe.As<TypedReader<T2>>( d[1] );
_item3 = Unsafe.As<TypedReader<T3>>( d[2] );
_item4 = Unsafe.As<TypedReader<T4>>( d[3] );
_item5 = Unsafe.As<TypedReader<T5>>( d[4] );
        }

        protected override void ReadInstance( ref RefReader r )
        {
            Debug.Assert( r.ReadInfo.SubTypes.Count == 5 );
            var a = (Tuple<T1, T2, T3, T4, T5>)RuntimeHelpers.GetUninitializedObject( typeof( Tuple<T1, T2, T3, T4, T5> ) );
            var d = r.SetInstance( a );
            var parameters = new object?[]
            {
               _item1( d, r.ReadInfo.SubTypes[0] ),
_item2( d, r.ReadInfo.SubTypes[1] ),
_item3( d, r.ReadInfo.SubTypes[2] ),
_item4( d, r.ReadInfo.SubTypes[3] ),
_item5( d, r.ReadInfo.SubTypes[4] )
            };
            typeof( Tuple<T1, T2, T3, T4, T5> ).GetConstructors()[0].Invoke( a, parameters );
        }
    }

    sealed class DValueTuple<T1, T2, T3, T4, T5> : ValueTypeDeserializer<ValueTuple<T1, T2, T3, T4, T5>>
    {
        readonly TypedReader<T1> _item1;
readonly TypedReader<T2> _item2;
readonly TypedReader<T3> _item3;
readonly TypedReader<T4> _item4;
readonly TypedReader<T5> _item5;

        public DValueTuple( Delegate[] d, bool isCached )
            : base( isCached )
        {
            _item1 = Unsafe.As<TypedReader<T1>>( d[0] );
_item2 = Unsafe.As<TypedReader<T2>>( d[1] );
_item3 = Unsafe.As<TypedReader<T3>>( d[2] );
_item4 = Unsafe.As<TypedReader<T4>>( d[3] );
_item5 = Unsafe.As<TypedReader<T5>>( d[4] );
        }

        protected override ValueTuple<T1, T2, T3, T4, T5> ReadInstance( IBinaryDeserializer d, ITypeReadInfo info )
        {
            return new ValueTuple<T1, T2, T3, T4, T5>( _item1( d, info.SubTypes[0] ),
_item2( d, info.SubTypes[1] ),
_item3( d, info.SubTypes[2] ),
_item4( d, info.SubTypes[3] ),
_item5( d, info.SubTypes[4] ) );
        }
    }
    sealed class DTuple<T1, T2, T3, T4, T5, T6> : ReferenceTypeDeserializer<Tuple<T1, T2, T3, T4, T5, T6>>
    {
        readonly TypedReader<T1> _item1;
readonly TypedReader<T2> _item2;
readonly TypedReader<T3> _item3;
readonly TypedReader<T4> _item4;
readonly TypedReader<T5> _item5;
readonly TypedReader<T6> _item6;

        public DTuple( Delegate[] d, bool isCached )
            : base( isCached )
        {
            _item1 = Unsafe.As<TypedReader<T1>>( d[0] );
_item2 = Unsafe.As<TypedReader<T2>>( d[1] );
_item3 = Unsafe.As<TypedReader<T3>>( d[2] );
_item4 = Unsafe.As<TypedReader<T4>>( d[3] );
_item5 = Unsafe.As<TypedReader<T5>>( d[4] );
_item6 = Unsafe.As<TypedReader<T6>>( d[5] );
        }

        protected override void ReadInstance( ref RefReader r )
        {
            Debug.Assert( r.ReadInfo.SubTypes.Count == 6 );
            var a = (Tuple<T1, T2, T3, T4, T5, T6>)RuntimeHelpers.GetUninitializedObject( typeof( Tuple<T1, T2, T3, T4, T5, T6> ) );
            var d = r.SetInstance( a );
            var parameters = new object?[]
            {
               _item1( d, r.ReadInfo.SubTypes[0] ),
_item2( d, r.ReadInfo.SubTypes[1] ),
_item3( d, r.ReadInfo.SubTypes[2] ),
_item4( d, r.ReadInfo.SubTypes[3] ),
_item5( d, r.ReadInfo.SubTypes[4] ),
_item6( d, r.ReadInfo.SubTypes[5] )
            };
            typeof( Tuple<T1, T2, T3, T4, T5, T6> ).GetConstructors()[0].Invoke( a, parameters );
        }
    }

    sealed class DValueTuple<T1, T2, T3, T4, T5, T6> : ValueTypeDeserializer<ValueTuple<T1, T2, T3, T4, T5, T6>>
    {
        readonly TypedReader<T1> _item1;
readonly TypedReader<T2> _item2;
readonly TypedReader<T3> _item3;
readonly TypedReader<T4> _item4;
readonly TypedReader<T5> _item5;
readonly TypedReader<T6> _item6;

        public DValueTuple( Delegate[] d, bool isCached )
            : base( isCached )
        {
            _item1 = Unsafe.As<TypedReader<T1>>( d[0] );
_item2 = Unsafe.As<TypedReader<T2>>( d[1] );
_item3 = Unsafe.As<TypedReader<T3>>( d[2] );
_item4 = Unsafe.As<TypedReader<T4>>( d[3] );
_item5 = Unsafe.As<TypedReader<T5>>( d[4] );
_item6 = Unsafe.As<TypedReader<T6>>( d[5] );
        }

        protected override ValueTuple<T1, T2, T3, T4, T5, T6> ReadInstance( IBinaryDeserializer d, ITypeReadInfo info )
        {
            return new ValueTuple<T1, T2, T3, T4, T5, T6>( _item1( d, info.SubTypes[0] ),
_item2( d, info.SubTypes[1] ),
_item3( d, info.SubTypes[2] ),
_item4( d, info.SubTypes[3] ),
_item5( d, info.SubTypes[4] ),
_item6( d, info.SubTypes[5] ) );
        }
    }
    sealed class DTuple<T1, T2, T3, T4, T5, T6, T7> : ReferenceTypeDeserializer<Tuple<T1, T2, T3, T4, T5, T6, T7>>
    {
        readonly TypedReader<T1> _item1;
readonly TypedReader<T2> _item2;
readonly TypedReader<T3> _item3;
readonly TypedReader<T4> _item4;
readonly TypedReader<T5> _item5;
readonly TypedReader<T6> _item6;
readonly TypedReader<T7> _item7;

        public DTuple( Delegate[] d, bool isCached )
            : base( isCached )
        {
            _item1 = Unsafe.As<TypedReader<T1>>( d[0] );
_item2 = Unsafe.As<TypedReader<T2>>( d[1] );
_item3 = Unsafe.As<TypedReader<T3>>( d[2] );
_item4 = Unsafe.As<TypedReader<T4>>( d[3] );
_item5 = Unsafe.As<TypedReader<T5>>( d[4] );
_item6 = Unsafe.As<TypedReader<T6>>( d[5] );
_item7 = Unsafe.As<TypedReader<T7>>( d[6] );
        }

        protected override void ReadInstance( ref RefReader r )
        {
            Debug.Assert( r.ReadInfo.SubTypes.Count == 7 );
            var a = (Tuple<T1, T2, T3, T4, T5, T6, T7>)RuntimeHelpers.GetUninitializedObject( typeof( Tuple<T1, T2, T3, T4, T5, T6, T7> ) );
            var d = r.SetInstance( a );
            var parameters = new object?[]
            {
               _item1( d, r.ReadInfo.SubTypes[0] ),
_item2( d, r.ReadInfo.SubTypes[1] ),
_item3( d, r.ReadInfo.SubTypes[2] ),
_item4( d, r.ReadInfo.SubTypes[3] ),
_item5( d, r.ReadInfo.SubTypes[4] ),
_item6( d, r.ReadInfo.SubTypes[5] ),
_item7( d, r.ReadInfo.SubTypes[6] )
            };
            typeof( Tuple<T1, T2, T3, T4, T5, T6, T7> ).GetConstructors()[0].Invoke( a, parameters );
        }
    }

    sealed class DValueTuple<T1, T2, T3, T4, T5, T6, T7> : ValueTypeDeserializer<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
    {
        readonly TypedReader<T1> _item1;
readonly TypedReader<T2> _item2;
readonly TypedReader<T3> _item3;
readonly TypedReader<T4> _item4;
readonly TypedReader<T5> _item5;
readonly TypedReader<T6> _item6;
readonly TypedReader<T7> _item7;

        public DValueTuple( Delegate[] d, bool isCached )
            : base( isCached )
        {
            _item1 = Unsafe.As<TypedReader<T1>>( d[0] );
_item2 = Unsafe.As<TypedReader<T2>>( d[1] );
_item3 = Unsafe.As<TypedReader<T3>>( d[2] );
_item4 = Unsafe.As<TypedReader<T4>>( d[3] );
_item5 = Unsafe.As<TypedReader<T5>>( d[4] );
_item6 = Unsafe.As<TypedReader<T6>>( d[5] );
_item7 = Unsafe.As<TypedReader<T7>>( d[6] );
        }

        protected override ValueTuple<T1, T2, T3, T4, T5, T6, T7> ReadInstance( IBinaryDeserializer d, ITypeReadInfo info )
        {
            return new ValueTuple<T1, T2, T3, T4, T5, T6, T7>( _item1( d, info.SubTypes[0] ),
_item2( d, info.SubTypes[1] ),
_item3( d, info.SubTypes[2] ),
_item4( d, info.SubTypes[3] ),
_item5( d, info.SubTypes[4] ),
_item6( d, info.SubTypes[5] ),
_item7( d, info.SubTypes[6] ) );
        }
    }
}
