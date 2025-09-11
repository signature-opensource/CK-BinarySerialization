using System;
using System.Diagnostics;

namespace CK.BinarySerialization;

/// <summary>
/// Deserializer base for value type <typeparamref name="T"/>.
/// <para>
/// This deserializer cannot read a previously written reference type: when a type has changed from class
/// to struct, the <see cref="ValueTypeDeserializerWithRef{T}"/> must be used (as long as previously written 
/// reference types must still be read).
/// </para>
/// <para>
/// The default constructor sets <see cref="IsCached"/> to true. This is fine for basic drivers but as soon as
/// the driver depends on others (like generics drivers), the non default constructor should be used. 
/// </para>
/// </summary>
/// <typeparam name="T">The type to deserialize.</typeparam>
public abstract class ValueTypeDeserializer<T> : IDeserializationDriverInternal, IValueTypeNonNullableDeserializationDriver<T> where T : struct
{
    sealed class NullableAdapter : IValueTypeNullableDeserializationDriver<T>
    {
        readonly ValueTypeDeserializer<T> _deserializer;
        readonly TypedReader<T?> _reader;

        public NullableAdapter( ValueTypeDeserializer<T> deserializer )
        {
            _deserializer = deserializer;
            ResolvedType = typeof( Nullable<> ).MakeGenericType( typeof( T ) );
            _reader = ReadInstance;
        }

        public IValueTypeNullableDeserializationDriver<T> ToNullable => this;

        public IValueTypeNonNullableDeserializationDriver<T> ToNonNullable => _deserializer;

        public Type ResolvedType { get; }

        public Delegate TypedReader => _reader;

        public bool IsCached => _deserializer.IsCached;

        IDeserializationDriver IDeserializationDriver.Nullable => this;

        IDeserializationDriver IDeserializationDriver.NonNullable => _deserializer;

        public T? ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
        {
            Debug.Assert( readInfo.IsNullable );
            if( d.Reader.ReadBoolean() )
            {
                return _deserializer.ReadInstance( d, readInfo.NonNullable );
            }
            return default;
        }

    }

    readonly NullableAdapter _null;
    readonly TypedReader<T> _reader;

    /// <summary>
    /// Initializes a new <see cref="ValueTypeDeserializer{T}"/> where <see cref="IsCached"/> is true.
    /// <para>
    /// Caution: this cached default is easier for basic types but not for composite drivers that relies on other ones (like generic ones).
    /// </para>
    /// </summary>
    protected ValueTypeDeserializer()
    {
        _null = new NullableAdapter( this );
        _reader = ReadInstance;
        IsCached = true;
    }

    /// <summary>
    /// Initializes a new <see cref="ValueTypeDeserializer{T}"/> that states whether it is cached or not.
    /// </summary>
    /// <param name="isCached">Whether this deserializer is cached.</param>
    protected ValueTypeDeserializer( bool isCached )
    {
        _null = new NullableAdapter( this );
        _reader = ReadInstance;
        IsCached = isCached;
    }

    /// <summary>
    /// Must read a non null instance from the deserializer.
    /// </summary>
    /// <param name="d">The deserializer.</param>
    /// <param name="readInfo">The read type info.</param>
    /// <returns>The new instance.</returns>
    protected abstract T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo );

    /// <inheritdoc />
    public Type ResolvedType => typeof( T );

    /// <inheritdoc />
    public Delegate TypedReader => _reader;

    /// <inheritdoc />
    public bool IsCached { get; }

    /// <inheritdoc />
    public IValueTypeNullableDeserializationDriver<T> ToNullable => _null;

    /// <inheritdoc />
    public IValueTypeNonNullableDeserializationDriver<T> ToNonNullable => this;

    IDeserializationDriver IDeserializationDriver.Nullable => _null;

    IDeserializationDriver IDeserializationDriver.NonNullable => this;

    T IValueTypeNonNullableDeserializationDriver<T>.ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => ReadInstance( d, readInfo );

    object IDeserializationDriverInternal.ReadObjectData( IBinaryDeserializer d, ITypeReadInfo readInfo ) => ReadInstance( d, readInfo );

}
