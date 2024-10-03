using NUnit.Framework;
using System;
using System.Collections.Generic;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using FluentAssertions;

namespace CK.BinarySerialization.Tests;



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
        var a = new List<uint> { 3712, 42 };
        object? backA = TestHelper.SaveAndLoadObject( a );
        backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );

        var backB = TestHelper.SaveAndLoadObject<IList<uint>>( a );
        backB.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );

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
    public void nullable_enum_type_List_serialization()
    {
        var a = new List<GrantLevel?> { GrantLevel.Viewer, null, GrantLevel.Editor };
        object? backA = TestHelper.SaveAndLoadObject( a );
        backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );
    }

    [Test]
    public void nullable_ValueTuple_type_List_serialization()
    {
        var a = new List<(int, string?)?> { (3, null), null, (4, "four") };
        object? backA = TestHelper.SaveAndLoadObject( a );
        backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );
    }

    [Test]
    public void nullable_Tuple_type_List_serialization()
    {
        var a = new List<Tuple<int, string?>?> { Tuple.Create(3, (string?)null), null, Tuple.Create( 4, (string?)"four") };
        object? backA = TestHelper.SaveAndLoadObject( a );
        backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );
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
    public void HashSet_serialization()
    {
        var a = new HashSet<uint>();
        a.Add( 3712 );
        a.Add( 42 );
        a.Add( 57161671 );
        a.Add( 5468 );
        object? backA = TestHelper.SaveAndLoadObject( a );
        backA.Should().BeEquivalentTo( a );
        a.Clear();
        ((HashSet<uint>)TestHelper.SaveAndLoadObject( a )).Should().BeEmpty();
    }


    [Test]
    public void HashSet_serialization_with_specific_comparer_that_must_be_serializable_or_KnownObject()
    {
        var a = new HashSet<int>( ByTenInt32Equality.Instance ) { -1, 0, 5, 9, 12, 17 };
        a.Should().HaveCount( 2 );

        // The comparer cannot be serialized.
        FluentActions.Invoking( () => TestHelper.SaveAndLoadObject( a ) )
                      .Should().Throw<InvalidOperationException>();

        // We create totally independent contexts here to avoid the Default contexts pollution.
        var sC = new BinarySerializerContext( new SharedBinarySerializerContext( knownObjects: new SharedSerializerKnownObject() ) );
        var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext( knownObjects: new SharedDeserializerKnownObject() ) );

        // Since the comparer is a singleton, we can use the KnownObject approach.
        // One may also develop and register "fake" serialization and a deserialization drivers, or
        // implement ICKSimpleSerializable (but it's more work).
        // Using KnownObject is easier for true singletons.
        sC.Shared.KnownObjects.RegisterKnownObject( ByTenInt32Equality.Instance, "Tests.ByTenEqualityComparer" );
        dC.Shared.KnownObjects.RegisterKnownKey( "Tests.ByTenEqualityComparer", ByTenInt32Equality.Instance );

        // Now we can serialize our HashSet with its comparer.
        var backA = TestHelper.SaveAndLoadObject( a, sC, dC );
        backA.Should().NotBeSameAs( a ).And.BeEquivalentTo( a );
        backA.Comparer.Should().BeSameAs( ByTenInt32Equality.Instance );

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

    [Test]
    public void multidimensional_arrays()
    {
        {
            var a = new int[2, 3] { { 0, 1, 2 }, { 3, 4, 5 } };
            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );
        }
        {
            var a = new int?[2, 3] { { 0, null, 2 }, { 3, 4, null } };
            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );
        }
        {
            // Empty array but still multidimensional.
            var a = new int[0, 3] { };
            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );
        }
        {
            var a = new int[2, 2, 3]
            {
                { { 1, 2, 3}, {4, 5, 6} },
                { { 7, 8, 9}, {10, 11, 12} }
            };
            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );
        }
        {
            int[,,,] a = new int[1, 2, 2, 2]
            {
                {
                    { {1, 2}, {3, 4} },
                    { {5, 6}, {7, 8} }
                }
            };

            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );
        }
    }

    [Test]
    public void simple_dictionary_support()
    {
        {
            var a = new Dictionary<string, int?>
            {
                {"1", 2},
                {"3", 4},
                {"5", null},
                {"7", 8}
            };
            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a );
        }
        {
            var a = new Dictionary<int, string?>
            {
                {1, "2"},
                {3, "4"},
                {5, null!},
                {7, "8"}
            };
            object? backA = TestHelper.SaveAndLoadObject( a );
            backA.Should().BeEquivalentTo( a );
        }
    }

    [Test]
    public void complex_collection_support()
    {
        var a = new Dictionary<(int, int), string[,]?>
        {
            { (1, 2), new string[,]{ {"a","b"}, {"b","c"} } },
            { (2, 4), new string[,]{ {"c","d"}, {"e","f"} } },
            { (3, 6), new string[,]{ {"g","h"}, {"i","j"} } },
            { (4, 8), new string[,]{ {"k","l"}, {"m","n"} } },
            { (5, 0), new string[,]{ {"o","p"}, {"q","r"} } },
            { (6, 0), null }
        };
        object? backA = TestHelper.SaveAndLoadObject( a );
        backA.Should().BeEquivalentTo( a );
    }

    [Test]
    public void complex_collection_support_2()
    {
        var a = new Dictionary<string, List<Dictionary<ushort, string[,]?>>>
        {
            {
                "A",
                new List<Dictionary<ushort, string[,]?>>
                {
                    new Dictionary<ushort, string[,]?>
                    {
                        { 45, null },
                        { 72, new string[,]{ { "a", "b" }, { "b", "c" } } },
                        { 68, new string[,]{ { "o", "p" }, { "q", "r" } } }
                    }
                }
            }
        };
        object? backA = TestHelper.SaveAndLoadObject( a );
        backA.Should().BeEquivalentTo( a );
    }
    

    [Test]
    public void dictionary_comparer_support_OrdinalIgnoreCase_and_InvariantCultureIgnoreCase()
    {
        var a = new Dictionary<string, int>( StringComparer.OrdinalIgnoreCase );
        a.Add( "A", 1 );
        FluentActions.Invoking( () => a.Add( "a", 1 ) ).Should().Throw<ArgumentException>();
        var backA = TestHelper.SaveAndLoadObject( a );
        backA.Should().BeEquivalentTo( a );
        FluentActions.Invoking( () => backA.Add( "a", 1 ) ).Should().Throw<ArgumentException>();

        var a2 = new Dictionary<string, string>( StringComparer.InvariantCultureIgnoreCase )
        {
            { "A", "plop" }
        };
        FluentActions.Invoking( () => a2.Add( "a", "no way" ) ).Should().Throw<ArgumentException>();
        var backA2 = TestHelper.SaveAndLoadObject( a2 );
        backA2.Should().BeEquivalentTo( a2 );
        FluentActions.Invoking( () => backA2.Add( "a", "no way again" ) ).Should().Throw<ArgumentException>();
    }

    [Test]
    public void dictionary_of_byte()
    {
        var a = new Dictionary<byte, int>() { { 1, 1000 }, { 2, 2000 } };
        var backA = TestHelper.SaveAndLoadObject( a );
        backA.Should().BeEquivalentTo( a );
    }


    [Test]
    public void list_of_sealed_classes()
    {
        var a = new List<SimpleSealedDerived>();
        a.Add( new SimpleSealedDerived() { Power = 42, Name = "Alice" } );
        a.Add( new SimpleSealedDerived() { Power = 3712, Name = "Albert" } );

        object? backA = TestHelper.SaveAndLoadObject( a );
        backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );
    }

    [Test]
    public void list_of_non_sealed_classes()
    {
        var a = new List<SimpleBase>();
        a.Add( new SimpleSealedDerived() { Power = 42, Name = "Alice" } );
        a.Add( new SimpleDerived() { Power = 3712, Name = "Albert" } );

        object? backA = TestHelper.SaveAndLoadObject( a );
        backA.Should().BeEquivalentTo( a, o => o.WithStrictOrdering() );
    }
}
