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
        public void Array_of_int_to_Array_of_long_is_safe()
        {
            var list = new int[] { 1, 2, 3 };
            TestHelper.SaveAndLoad(
                s => s.WriteObject( list ),
                d => d.ReadObject<long[]>().Should().BeOfType<long[]>().And.BeEquivalentTo( list ) );
        }

        [Test]
        public void Array_of_int_to_Array_of_byte_can_overflow()
        {
            var list = new int[] { 1, 256, 3 };

            FluentActions.Invoking( () =>
                TestHelper.SaveAndLoad( s => s.WriteObject( list ), d => d.ReadObject<byte[]>() )
                ).Should().Throw<OverflowException>();

            list[1] = 255;
            TestHelper.SaveAndLoad(
                s => s.WriteObject( list ),
                d => d.ReadObject<byte[]>().Should().BeOfType<byte[]>().And.BeEquivalentTo( list ) );
        }

        [Test]
        public void List_of_int_to_Array_of_long_is_safe()
        {
            var list = new List<int> { 1, 2, 3 };
            TestHelper.SaveAndLoad(
                s => s.WriteObject( list ),
                d => d.ReadObject<long[]>().Should().BeOfType<long[]>().And.BeEquivalentTo( list ) );
        }

        [Test]
        public void List_of_int_to_Array_of_byte_can_overflow()
        {
            var list = new List<int> { 1, 256, 3 };

            FluentActions.Invoking( () =>
                TestHelper.SaveAndLoad( s => s.WriteObject( list ), d => d.ReadObject<byte[]>() )
                ).Should().Throw<OverflowException>();

            list[1] = 255;
            TestHelper.SaveAndLoad(
                s => s.WriteObject( list ),
                d => d.ReadObject<byte[]>().Should().BeOfType<byte[]>().And.BeEquivalentTo( list ) );
        }

        [Test]
        public void from_List_of_int_to_List_of_long_is_safe()
        {
            var list = new List<int> { 1, 2, 3 };
            TestHelper.SaveAndLoad(
                s => s.WriteObject( list ),
                d => d.ReadObject<List<long>>().Should().BeOfType<List<long>>().And.BeEquivalentTo( list ) );
        }
    }
}
