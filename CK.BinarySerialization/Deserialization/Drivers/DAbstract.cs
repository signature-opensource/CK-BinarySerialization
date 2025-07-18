using System;

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

        public IDeserializationDriver Nullable => this;

        public IDeserializationDriver NonNullable => _d;

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

    public IDeserializationDriver Nullable => _nullable;

    public IDeserializationDriver NonNullable => this;

    static T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.ReadObject<T>();

}
