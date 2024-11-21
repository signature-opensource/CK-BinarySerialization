using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Deserialization;

sealed class DAbstract<T> : IDeserializationDriver where T : class
{
    sealed class DAbstractNullable : IDeserializationDriver
    {
        readonly DAbstract<T> _d;

        public DAbstractNullable( DAbstract<T> d )
        {
            _d = d;
            TypedReader = (TypedReader<T?>)ReadInstance;
        }

        public Type ResolvedType => _d.ResolvedType;

        public Delegate TypedReader { get; }

        public bool IsCached => true;

        public IDeserializationDriver ToNullable => this;

        public IDeserializationDriver ToNonNullable => _d;

        static T? ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.ReadNullableObject<T>();
    }

    readonly DAbstractNullable _nullable;

    public DAbstract()
    {
        ResolvedType = typeof( T );
        TypedReader = (TypedReader<T?>)ReadInstance;
        _nullable = new DAbstractNullable( this );
    }

    public Type ResolvedType { get; }

    public Delegate TypedReader { get; }

    public bool IsCached => true;

    public IDeserializationDriver ToNullable => _nullable;

    public IDeserializationDriver ToNonNullable => this;

    static T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.ReadObject<T>();

}
