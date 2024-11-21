using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CK.BinarySerialization;


/// <summary>
/// Encapsulates a <see cref="ITypeReadInfo"/> that is not null, not a nullable type,
/// has a driver name and a valid resolved concrete local type and has not yet a 
/// resolved <see cref="ISerializationDriver"/>.
/// </summary>
public readonly ref struct DeserializerResolverArg
{
    /// <summary>
    /// Gets the non nullable <see cref="ITypeReadInfo"/>.
    /// </summary>
    public readonly ITypeReadInfo ReadInfo;

    /// <summary>
    /// Gets the local type.
    /// </summary>
    public readonly Type ExpectedType;

    /// <summary>
    /// Gets the driver name.
    /// </summary>
    public string DriverName => ReadInfo.DriverName!;

    /// <summary>
    /// Gets the deserialization context. 
    /// </summary>
    public readonly BinaryDeserializerContext Context;

    /// <summary>
    /// True if the <see cref="ExpectedType"/> is the same as the <see cref="ITypeReadInfo.TryResolveLocalType()"/>
    /// and <see cref="ITypeReadInfo.IsDirtyInfo"/> is false.
    /// <para>
    /// Whether the resolved driver is eventually cached (<see cref="IDeserializationDriver.IsCached"/>) is up to
    /// the resolvers.
    /// </para>
    /// </summary>
    public readonly bool IsPossibleNominalDeserialization;

    /// <summary>
    /// Initializes a new <see cref="DeserializerResolverArg"/>.
    /// </summary>
    /// <param name="info">The type info for which a deserialization driver must be resolved.</param>
    /// <param name="context">The shared context is used only to detect mismatch of resolution context.</param>
    /// <param name="expectedType">
    /// Type that must be deserialized.
    /// This should be changed to a NullableTypeTree once to be able to handle generic type mutations.
    /// </param>
    internal DeserializerResolverArg( ITypeReadInfo info, BinaryDeserializerContext context, Type expectedType )
    {
        Debug.Assert( info != null );
        Debug.Assert( !info.IsNullable, "Type must not be nullable." );
        Debug.Assert( info.DriverName != null, "Must have a driver name." );
        Debug.Assert( context != null );
        Debug.Assert( expectedType != null );
        Debug.Assert( expectedType != info.TargetType || !info.HasResolvedConcreteDriver, "Deserialization driver for TargetType must not be already resolved." );
        Debug.Assert( expectedType == info.TargetType || info.TargetType == null || !expectedType.IsAssignableFrom( info.TargetType ) );
        ReadInfo = info;
        ExpectedType = expectedType;
        IsPossibleNominalDeserialization = ExpectedType == info.TryResolveLocalType() && !info.IsDirtyInfo;
        Context = context;
    }

}
