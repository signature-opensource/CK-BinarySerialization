using System;
using System.Collections.Generic;

namespace CK.BinarySerialization;

/// <summary>
/// Immutable singleton that contains default serializers for well-known types.
/// <para>
/// A simple static dictionary is enough since it is only read.
/// </para>
/// </summary>
public sealed class BasicTypesSerializerResolver : ISerializerResolver
{
    static readonly Dictionary<Type, ISerializationDriver> _byType;

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static readonly BasicTypesSerializerResolver Instance = new BasicTypesSerializerResolver();

    BasicTypesSerializerResolver() { }

    static BasicTypesSerializerResolver()
    {
        _byType = new Dictionary<Type, ISerializationDriver>();
        Register( new Serialization.DString() );
        Register( new Serialization.DByteArray() );

        Register( new Serialization.DBool() );
        Register( new Serialization.DInt32() );
        Register( new Serialization.DUInt32() );
        Register( new Serialization.DInt8() );
        Register( new Serialization.DUInt8() );
        Register( new Serialization.DInt16() );
        Register( new Serialization.DUInt16() );
        Register( new Serialization.DInt64() );
        Register( new Serialization.DUInt64() );
        Register( new Serialization.DSingle() );
        Register( new Serialization.DDouble() );
        Register( new Serialization.DChar() );
        Register( new Serialization.DDateTime() );
        Register( new Serialization.DDateTimeOffset() );
        Register( new Serialization.DTimeSpan() );
        Register( new Serialization.DGuid() );
        Register( new Serialization.DDecimal() );

        Register( new Serialization.DSVersion() );
        Register( new Serialization.DCSVersion() );
        Register( new Serialization.DSVersionBound() );
        Register( new Serialization.DPackageQualityVector() );
        Register( new Serialization.DPackageQualityFilter() );
        Register( new Serialization.DVersion() );
    }

    static void Register<T>( StaticValueTypeSerializer<T> driver ) where T : struct
    {
        _byType.Add( typeof( T ), driver );
        _byType.Add( typeof( Nullable<> ).MakeGenericType( typeof( T ) ), driver.ToNullable );
    }

    static void Register<T>( ReferenceTypeSerializer<T> driver ) where T : class
    {
        _byType.Add( typeof( T ), driver.ToNullable );
    }

    /// <inheritdoc />
    /// <remarks>
    /// The <paramref name="context"/> is not used by this resolver.
    /// </remarks>
    public ISerializationDriver? TryFindDriver( BinarySerializerContext context, Type t ) => _byType.GetValueOrDefault( t );

}
