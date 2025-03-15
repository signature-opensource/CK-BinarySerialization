using CK.Core;
using Shouldly;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.BinarySerialization.Tests;

[TestFixture]
public partial class MutationTests
{
    [Test]
    public void HashSet_are_NOT_mutated_to_array_list_or_stack()
    {
        var set = new HashSet<int> { 1, 2, 3 };

        Util.Invokable( () =>
            TestHelper.SaveAndLoad( s => s.WriteObject( set ), d => d.ReadObject<List<int>>() )
        ).ShouldThrow<InvalidOperationException>();

        Util.Invokable( () =>
            TestHelper.SaveAndLoad( s => s.WriteObject( set ), d => d.ReadObject<int[]>() )
        ).ShouldThrow<InvalidOperationException>();

        Util.Invokable( () =>
            TestHelper.SaveAndLoad( s => s.WriteObject( set ), d => d.ReadObject<Stack<int>>() )
        ).ShouldThrow<InvalidOperationException>();
    }

    [Test]
    public void Queue_are_NOT_mutated_to_array_list_or_stack()
    {
        var q = new Queue<int>();
        q.Enqueue( 1 );
        q.Enqueue( 2 );
        q.Enqueue( 3 );

        Util.Invokable( () =>
            TestHelper.SaveAndLoad( s => s.WriteObject( q ), d => d.ReadObject<List<int>>() )
        ).ShouldThrow<InvalidOperationException>();

        Util.Invokable( () =>
            TestHelper.SaveAndLoad( s => s.WriteObject( q ), d => d.ReadObject<int[]>() )
        ).ShouldThrow<InvalidOperationException>();

        Util.Invokable( () =>
            TestHelper.SaveAndLoad( s => s.WriteObject( q ), d => d.ReadObject<Stack<int>>() )
        ).ShouldThrow<InvalidOperationException>();
    }

    [Test]
    public void HashSet_of_numeric_CANNOT_mutate_if_Comparer_is_not_the_default_one()
    {
        var set = new HashSet<int>( ByTenInt32Equality.Instance ) { 1, 2, 3 };

        var sC = new BinarySerializerContext( new SharedBinarySerializerContext( knownObjects: new SharedSerializerKnownObject() ) );
        var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext( knownObjects: new SharedDeserializerKnownObject() ) );
        sC.Shared.KnownObjects.RegisterKnownObject( ByTenInt32Equality.Instance, "Tests.ByTenEqualityComparer" );
        dC.Shared.KnownObjects.RegisterKnownKey( "Tests.ByTenEqualityComparer", ByTenInt32Equality.Instance );

        Util.Invokable( () =>
            TestHelper.SaveAndLoad( s => s.WriteObject( set ), d => d.ReadObject<HashSet<long>>(), sC, dC )
        ).ShouldThrow<DeserializationException>()
         .InnerException.ShouldBeOfType<InvalidCastException>();
    }

    [Test]
    public void HashSet_of_numeric_with_default_comparer_can_mutate()
    {
        var set = new HashSet<int>() { 1, 2, 3 };
        TestHelper.SaveAndLoad(
            s => s.WriteObject( set ),
            d => d.ReadObject<HashSet<long>>().ShouldBeOfType<HashSet<long>>().ShouldBe( set.Select( i => (long)i ), ignoreOrder: true ) );
    }

    [Test]
    public void Array_of_int_to_Array_of_long_is_safe()
    {
        var list = new int[] { 1, 2, 3 };
        TestHelper.SaveAndLoad(
            s => s.WriteObject( list ),
            d => d.ReadObject<long[]>().ShouldBeOfType<long[]>().ShouldBe( list.Select( i => (long)i ) ) );
    }

    [Test]
    public void Array_of_int_to_Array_of_byte_can_overflow()
    {
        var list = new int[] { 1, 256, 3 };

        Util.Invokable( () =>
            TestHelper.SaveAndLoad( s => s.WriteObject( list ), d => d.ReadObject<byte[]>() )
            ).ShouldThrow<DeserializationException>()
             .InnerException.ShouldBeOfType<OverflowException>();

        list[1] = 255;
        TestHelper.SaveAndLoad(
            s => s.WriteObject( list ),
            d => d.ReadObject<byte[]>().ShouldBeOfType<byte[]>().ShouldBe( list.Select( i => (byte)i ) ) );
    }

    [Test]
    public void List_of_int_to_List_of_long_is_safe()
    {
        var list = new List<int> { 1, 2, 3 };
        TestHelper.SaveAndLoad(
            s => s.WriteObject( list ),
            d => d.ReadObject<List<long>>().ShouldBeOfType<List<long>>().ShouldBe(list.Select(i => (long)i)) );
    }

    [Test]
    public void List_of_int_to_Array_of_long_is_safe()
    {
        var list = new List<int> { 1, 2, 3 };
        TestHelper.SaveAndLoad(
            s => s.WriteObject( list ),
            d => d.ReadObject<long[]>().ShouldBeOfType<long[]>().ShouldBe(list.Select(i => (long)i)) );
    }

    [Test]
    public void Stack_of_int_to_Array_of_long_is_safe()
    {
        var set = new Stack<int>();
        set.Push( 1 );
        set.Push( 2 );
        set.Push( 3 );
        TestHelper.SaveAndLoad(
            s => s.WriteObject( set ),
            d => d.ReadObject<long[]>().ShouldBeOfType<long[]>().ShouldBe(set.Select(i => (long)i)) );
    }

    [Test]
    public void List_of_int_to_Array_of_byte_can_overflow()
    {
        var list = new List<int> { 1, 256, 3 };

        Util.Invokable( () =>
            TestHelper.SaveAndLoad( s => s.WriteObject( list ), d => d.ReadObject<byte[]>() )
            ).ShouldThrow<DeserializationException>()
             .InnerException.ShouldBeOfType<OverflowException>();

        list[1] = 255;
        TestHelper.SaveAndLoad(
            s => s.WriteObject( list ),
            d => d.ReadObject<byte[]>().ShouldBeOfType<byte[]>().ShouldBe(list.Select(i => (byte)i)) );
    }

    [Test]
    public void Dictionary_with_numeric_key_with_default_comparer_can_mutate()
    {
        var dic = new Dictionary<sbyte, short> { { 1, 2 }, { 127, -3 }, { -53, 500 } };

        Util.Invokable( () =>
            TestHelper.SaveAndLoad( s => s.WriteObject( dic ), d => d.ReadObject<Dictionary<int, sbyte>>() )
            ).ShouldThrow<DeserializationException>()
             .InnerException.ShouldBeOfType<OverflowException>();

        TestHelper.SaveAndLoad(
            s => s.WriteObject( dic ),
            d => d.ReadObject<Dictionary<int, double>>()
                    .ShouldBeOfType<Dictionary<int, double>>()
                             .ShouldMatch( x => x[1] == 2.0 && x[127] == -3.0 && x[-53] == 500.0 ) );
    }

    [Test]
    public void Dictionary_with_numeric_key_CANNOT_mutate_if_Comparer_is_not_the_default_one()
    {
        var dic = new Dictionary<int, string>( ByTenInt32Equality.Instance ) { { 1, "One" }, { 10, "Two" } };

        var sC = new BinarySerializerContext( new SharedBinarySerializerContext( knownObjects: new SharedSerializerKnownObject() ) );
        var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext( knownObjects: new SharedDeserializerKnownObject() ) );
        sC.Shared.KnownObjects.RegisterKnownObject( ByTenInt32Equality.Instance, "Tests.ByTenEqualityComparer" );
        dC.Shared.KnownObjects.RegisterKnownKey( "Tests.ByTenEqualityComparer", ByTenInt32Equality.Instance );

        Util.Invokable( () =>
            TestHelper.SaveAndLoad( s => s.WriteObject( dic ), d => d.ReadObject<Dictionary<long, string>>(), sC, dC )
        ).ShouldThrow<DeserializationException>()
         .InnerException.ShouldBeOfType<InvalidCastException>();
    }

    [Test]
    public void Array_can_mutate_to_List()
    {
        var a = new int[] { 1, 2, 3 };
        TestHelper.SaveAndLoad(
            s => s.WriteObject( a ),
            d => d.ReadObject<List<int>>().ShouldBeOfType<List<int>>().ShouldBe( a ) );
    }

    [Test]
    public void ImmutableArray_can_mutate_to_Array()
    {
        // The opposite is currently not true...
        ImmutableArray<int> a = [1, 2, 3];
        TestHelper.SaveAndLoad(
            s => s.WriteValue( a ),
            d => d.ReadObject<int[]>().ShouldBeOfType<int[]>().ShouldBe( a ) );
    }

    [Test]
    public void ImmutableArray_int_can_mutate_to_ImmutableArray_long()
    {
        ImmutableArray<int> a = [1, 2, 3];
        TestHelper.SaveAndLoad(
            s => s.WriteValue( a ),
            d => d.ReadValue<ImmutableArray<long>>().ShouldBeOfType<ImmutableArray<long>>().ShouldBe( a.Select( i => (long)i ) ) );
    }

    [Test]
    public void Array_can_mutate_to_Stack()
    {
        var a = new int[] { 1, 2, 3 };
        TestHelper.SaveAndLoad(
            s => s.WriteObject( a ),
            d => d.ReadObject<Stack<int>>().ShouldBeOfType<Stack<int>>().ShouldBe( a ) );
    }


}
