using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CK.BinarySerialization.Tests;

[TestFixture]
public class NullabilityInfoTests
{
    public int? NullableValueType { get; set; }

    [Test]
    public void nullability_on_value_type()
    {
        var c = new NullabilityInfoContext();
        var n = c.Create( typeof( NullabilityInfoTests ).GetProperty( nameof( NullableValueType ) )! );
        n.ReadState.ShouldBe( NullabilityState.Nullable );
        n.WriteState.ShouldBe( NullabilityState.Nullable );
        n.Type.ShouldBe( typeof( Nullable<int> ) );
    }

    [DisallowNull]
    public string? NullableOutRefType { get; set; }

    [AllowNull]
    public string NullableInRefType { get; set; }

    [Test]
    public void nullability_on_ref_type_handles_nullable_Allow_and_DisallowNull()
    {
        var c = new NullabilityInfoContext();
        {
            var n = c.Create( typeof( NullabilityInfoTests ).GetProperty( nameof( NullableOutRefType ) )! );
            n.ReadState.ShouldBe( NullabilityState.Nullable );
            n.WriteState.ShouldBe( NullabilityState.NotNull );
            n.Type.ShouldBe( typeof( string ) );
        }
        {
            var n = c.Create( typeof( NullabilityInfoTests ).GetProperty( nameof( NullableInRefType ) )! );
            n.ReadState.ShouldBe( NullabilityState.NotNull );
            n.WriteState.ShouldBe( NullabilityState.Nullable );
            n.Type.ShouldBe( typeof( string ) );
        }
    }

    public Dictionary<string, int?>? Dictionary { get; set; }

    [Test]
    public void nullability_dictionary_key_is_notnull()
    {
        var c = new NullabilityInfoContext();
        var n = c.Create( typeof( NullabilityInfoTests ).GetProperty( nameof( Dictionary ) )! );

        n.GenericTypeArguments[0].ReadState.ShouldBe( NullabilityState.NotNull );
        n.GenericTypeArguments[0].WriteState.ShouldBe( NullabilityState.NotNull );

        n.GenericTypeArguments[1].ReadState.ShouldBe( NullabilityState.Nullable );
        n.GenericTypeArguments[1].WriteState.ShouldBe( NullabilityState.Nullable );
    }



}
