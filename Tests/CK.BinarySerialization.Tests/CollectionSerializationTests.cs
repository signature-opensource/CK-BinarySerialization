using NUnit.Framework;
using System;
using System.Collections.Generic;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using FluentAssertions;

namespace CK.BinarySerialization.Tests
{
    [TestFixture]
    public class CollectionSerializationTests
    {

        [Test]
        public void value_type_array_serialization()
        {
            int[] a = new int[] { 3712, 42 };
            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );
        }

        [Test]
        public void nullable_value_type_array_serialization()
        {
            int?[] a = new int?[] { 3712, null, 42 };
            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );
        }

        [Test]
        public void value_type_List_serialization()
        {
            var a = new List<uint>{ 3712, 42 };
            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );

            a.Clear();
            ((List<uint>)TestHelper.SaveAndLoadObject( a )).Should().BeEmpty();
        }

        [Test]
        public void nullable_value_type_List_serialization()
        {
            var a = new List<uint?> { 3712, null, 42 };
            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );

            a.Clear();
            ((List<uint?>)TestHelper.SaveAndLoadObject( a )).Should().BeEmpty();
        }

    }
}
