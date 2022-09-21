using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.BinarySerialization.Tests
{
    [TestFixture]
    public partial class MutationTests
    {
        [Test]
        public void reading_a_nullable_value_from_a_non_null_write_is_by_design_even_for_value_type()
        {
            TestHelper.SaveAndLoad(
                s =>
                {
                    s.WriteValue( (byte)5 );
                    s.WriteNullableValue( (byte?)5 );
                    s.WriteNullableValue( (byte?)null );
                },
                d =>
                {
                    byte? v = d.ReadNullableValue<byte>();
                    v.Should().BeOfType( typeof( byte ) ).And.Be( 5 );
                    v = d.ReadNullableValue<byte>();
                    v.Should().BeOfType( typeof( byte ) ).And.Be( 5 );
                    v = d.ReadNullableValue<byte>();
                    FluentActions.Invoking( () => v!.GetType() ).Should().Throw<NullReferenceException>();
                } );
        }

        //[Test]
        //public void Array_of_int_to_array_of_nullable_int()
        //{
        //    var a = new int[] { 1, 2, 3 };
        //    TestHelper.SaveAndLoad(
        //        s => s.WriteObject( a ),
        //        d => d.ReadObject<int?[]>().Should().BeOfType<int?[]>().And.BeEquivalentTo( a ) );
        //}


    }
}
