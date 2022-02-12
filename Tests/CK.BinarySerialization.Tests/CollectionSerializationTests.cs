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

        [Test]
        public void value_type_Queue_serialization()
        {
            var a = new Queue<uint>();
            a.Enqueue( 3712 );
            a.Enqueue( 42 );
            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );

            a.Clear();
            ((Queue<uint>)TestHelper.SaveAndLoadObject( a )).Should().BeEmpty();
        }

        [Test]
        public void nullable_value_type_Queue_serialization()
        {
            var a = new Queue<uint?>();
            a.Enqueue( 3712 );
            a.Enqueue( null );
            a.Enqueue( 52 );
            a.Enqueue( null );
            a.Enqueue( 42 );
            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );

            a.Clear();
            ((Queue<uint?>)TestHelper.SaveAndLoadObject( a )).Should().BeEmpty();
        }

        [Test]
        public void value_type_Stack_serialization()
        {
            var a = new Stack<uint>();
            a.Push( 3712 );
            a.Push( 42 );
            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );

            a.Clear();
            ((Stack<uint>)TestHelper.SaveAndLoadObject( a )).Should().BeEmpty();
        }

        [Test]
        public void nullable_value_type_Stack_serialization()
        {
            var a = new Stack<uint?>();
            a.Push( 3712 );
            a.Push( null );
            a.Push( 52 );
            a.Push( null );
            a.Push( 42 );
            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );

            a.Clear();
            ((Stack<uint?>)TestHelper.SaveAndLoadObject( a )).Should().BeEmpty();
        }

    }
}
