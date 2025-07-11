using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CK.BinarySerialization;

/// <summary>
/// Immutable singleton that contains default deserializers for well-known types.
/// <para>
/// A simple dictionary is enough since it is only read.
/// </para>
/// </summary>
public sealed class BasicTypesDeserializerResolver : IDeserializerResolver
{
    static readonly Dictionary<string, IDeserializationDriver> _byName;

    /// <summary>
    /// Gets the default registry.
    /// </summary>
    public static readonly IDeserializerResolver Instance = new BasicTypesDeserializerResolver();

    BasicTypesDeserializerResolver() { }

    static BasicTypesDeserializerResolver()
    {
        _byName = new Dictionary<string, IDeserializationDriver>()
        {
            { "string", new Deserialization.DString() },
            { "byte[]", new Deserialization.DByteArray() },

            { "bool", new Deserialization.DBool() },
            { "int", new Deserialization.DInt32() },
            { "uint", new Deserialization.DUInt32() },
            { "sbyte", new Deserialization.DInt8() },
            { "byte", new Deserialization.DUInt8() },
            { "short", new Deserialization.DInt16() },
            { "ushort", new Deserialization.DUInt16() },
            { "long", new Deserialization.DInt64() },
            { "ulong", new Deserialization.DUInt64() },
            { "float", new Deserialization.DSingle() },
            { "double", new Deserialization.DDouble() },
            { "char", new Deserialization.DChar() },
            { "DateTime", new Deserialization.DDateTime() },
            { "DateTimeOffset", new Deserialization.DDateTimeOffset() },
            { "TimeSpan", new Deserialization.DTimeSpan() },
            { "Guid", new Deserialization.DGuid() },
            { "decimal", new Deserialization.DDecimal() },

            { "SVersion", new Deserialization.DSVersion() },
            { "CSVersion", new Deserialization.DCSVersion() },
            { "SVersionBound", new Deserialization.DSVersionBound() },
            { "PackageQualityVector", new Deserialization.DPackageQualityVector() },
            { "PackageQualityFilter", new Deserialization.DPackageQualityFilter() },
            { "Version", new Deserialization.DVersion() },
        };
    }

    /// <summary>
    /// Simple lookup in type to deserialization drivers lookup.
    /// </summary>
    /// <param name="info">The info to resolve.</param>
    /// <returns>A deserialization driver or null.</returns>
    public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
    {
        var d = _byName.GetValueOrDefault( info.DriverName );
        if( d == null ) return null;

        // The TargetType for basic types is necessarily locally resolved AND this should NOT
        // be changed!
        Throw.CheckState( "Come on! Altering the mapping of basic types makes no sense!", info.ReadInfo.TargetType == d.ResolvedType );

        // If the expected type is the resolved one, we're done.
        if( d.ResolvedType == info.ExpectedType )
        {
            return d;
        }

        // Because no change of the mapping has been done, then expected type is then necessarily NOT a
        // type that can be assigned from the resolved type: no need to check for this.
        Debug.Assert( !info.ExpectedType.IsAssignableFrom( d.ResolvedType ) );

        // Allow Convert.ChangeType to occur but not to/from string or any object. Note that DateTime is not convertible to other type than string.
        //
        // Note that If the ExpectedType is an Enum, this just work because Convert.ChangeType handles it.
        //
        Debug.Assert( IsBasicallyConvertible( d.ResolvedType ) != TypeCode.Empty );

        var target = IsBasicallyConvertible( info.ExpectedType );
        if( target == TypeCode.Empty )
        {
            return null;
        }
        var tV = typeof( Deserialization.DChangeBasicType<,> ).MakeGenericType( info.ExpectedType, d.ResolvedType );
        return (IDeserializationDriver)Activator.CreateInstance( tV, d.TypedReader, target )!;
    }

    /// <summary>
    /// Returns <see cref="TypeCode.Empty"/> when not convertible.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns>The type code or <see cref="TypeCode.Empty"/> if the type is not basically convertible.</returns>
    public static TypeCode IsBasicallyConvertible( Type type )
    {
        var c = Type.GetTypeCode( type );
        return c == TypeCode.String || c == TypeCode.Object || c == TypeCode.DateTime || c == TypeCode.DBNull
                ? TypeCode.Empty
                : c;
    }
}
