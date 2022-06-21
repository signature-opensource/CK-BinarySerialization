using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CK.BinarySerialization.Tests
{
    [TestFixture]
    public class NullabilityInfoTests
    {
        public int? NullableValueType { get; set; }

        [Test]
        public void nullability_on_value_type()
        {
            var c = new NullabilityInfoContext();
            var n = c.Create( typeof( NullabilityInfoTests ).GetProperty( nameof( NullableValueType ) )! );
            n.ReadState.Should().Be( NullabilityState.Nullable );
            n.WriteState.Should().Be( NullabilityState.Nullable );
            n.Type.Should().Be( typeof( Nullable<int> ) );
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
                n.ReadState.Should().Be( NullabilityState.Nullable );
                n.WriteState.Should().Be( NullabilityState.NotNull );
                n.Type.Should().Be( typeof( string ) );
            }
            {
                var n = c.Create( typeof( NullabilityInfoTests ).GetProperty( nameof( NullableInRefType ) )! );
                n.ReadState.Should().Be( NullabilityState.NotNull );
                n.WriteState.Should().Be( NullabilityState.Nullable );
                n.Type.Should().Be( typeof( string ) );
            }
        }

        public Dictionary<string,int?>? Dictionary { get; set; }

        [Test]
        public void nullability_dictionary_key_is_notnull()
        {
            var c = new NullabilityInfoContext();
            var n = c.Create( typeof( NullabilityInfoTests ).GetProperty( nameof( Dictionary ) )! );

            n.GenericTypeArguments[0].ReadState.Should().Be( NullabilityState.NotNull );
            n.GenericTypeArguments[0].WriteState.Should().Be( NullabilityState.NotNull );

            n.GenericTypeArguments[1].ReadState.Should().Be( NullabilityState.Nullable );
            n.GenericTypeArguments[1].WriteState.Should().Be( NullabilityState.Nullable );
        }



    }
}
