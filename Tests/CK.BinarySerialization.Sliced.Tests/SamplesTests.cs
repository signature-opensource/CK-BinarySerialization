using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.BinarySerialization.Tests;

[TestFixture]
public class SamplesTests
{

    [Test]
    public void hierarchy_root_only_serialization()
    {
        var o = new Samples.Town( "Paris" );
        object? backO = TestHelper.SaveAndLoadAny( o );
        backO.Should().BeEquivalentTo( o );

        BinarySerializer.IdempotenceCheck( o ); 
    }

    [Test]
    public void small_graph_serialization()
    {
        var t = new Samples.Town( "Paris" );
        var p = new Samples.Person( t ) { Name = "Albert" };

        object? backP = TestHelper.SaveAndLoadAny( p );
        backP.Should().BeEquivalentTo( p, o => o.IgnoringCyclicReferences() );

        BinarySerializer.IdempotenceCheck( p );

        object? backT = TestHelper.SaveAndLoadAny( t );
        backT.Should().BeEquivalentTo( t, o => o.IgnoringCyclicReferences() );

        BinarySerializer.IdempotenceCheck( t ); 
    }

    [TestCase(30,2000)]
    public void big_town_with_a_lot_of_garages_with_employees_that_have_BestFriends( int nbGarage, int nbEmployees )
    {
        var town = new Samples.Town( "BigOne" );
        for( int i = 0; i < nbGarage; i++ )
        {
            var garage = new Samples.Garage( town );
            int realize = Enumerable.Range( 0, nbEmployees ).Select( i => new Samples.Employee( garage ) { Name = $"n°{i}", EmployeeNumber = i } ).Count();
            // This creates a linked list of employees that will
            // be serialized by the employee n°0: without the IDeserializationDeferredDriver this
            // would explode the stack.
            for( int j = 1; j < garage.Employees.Count; j++ )
            {
                garage.Employees[j - 1].BestFriend = garage.Employees[j];
            }
        }
        var backG = TestHelper.SaveAndLoadObject( town );
        // This enters an infinite loop: backG.Should().BeEquivalentTo( town, o => o.IgnoringCyclicReferences() );
        // Using the Statistics.
        backG.Stats.Should().Be( town.Stats );
        BinarySerializer.IdempotenceCheck( town );
    }

    [Test]
    public void Destroyable_simple_test()
    {
        var t = new Samples.Town( "Paris" );
        var g = new Samples.Garage( t );
        var albert = new Samples.Employee( g ) { Name = "Albert" };
        var alice = new Samples.Manager( g ) { Name = "Alice", Rank = 42 };
        albert.BestFriend = alice;

        {
            var backT = TestHelper.SaveAndLoadObject( t );
            backT.Should().BeEquivalentTo( t, o => o.IgnoringCyclicReferences() );
            var eBack = backT.Persons.OfType<Samples.Employee>().Single( p => p.Name == "Albert" );
            ((Samples.Manager)eBack.BestFriend!).Rank.Should().Be( 42 );
        }

        BinarySerializer.IdempotenceCheck( t );

        // Alice is removed from t.Persons list.
        // But Albert still has a reference to its BestFriend Alice.
        // Alice will be serialized and deserialized as a Destroyed object
        // that the deserialized Albert will still reference her.
        alice.Destroy();
        
        {
            var backT = TestHelper.SaveAndLoadObject( t );

            // Albert is alone in the list.
            var eBack = backT.Persons.Cast<Samples.Employee>().Single( p => p.Name == "Albert" );
            // And its BestFriend is destroyed.
            eBack.BestFriend!.IsDestroyed.Should().BeTrue();
            // Specialized data of destroyed object is not written (and not read back).
            // We, here, serialize the Person's Name (Person is the root of the hierarchy) to be able to identify them.
            eBack.BestFriend!.Name.Should().Be( "Alice" );
            ( (Samples.Manager)eBack.BestFriend!).Rank.Should().Be( 0, "Rank has NOT been read back." );
        }

        BinarySerializer.IdempotenceCheck( t );
    }

    [TestCase( null )]
    [TestCase( 1 )]
    [TestCase( 2 )]
    public void serializing_towns( int? seed )
    {
        var town = Samples.Town.CreateTown( seed );
        BinarySerializer.IdempotenceCheck( town );

        var duplicate = BinarySerializer.DeepClone( town );
        duplicate.Stats.Should().Be( town.Stats );

        var town2 = SamplesV2.Town.CreateTown( seed );
        if( seed != null ) town2.Stats.Should().Be( town.Stats );

        BinarySerializer.IdempotenceCheck( town2 );

        var duplicate2 = BinarySerializer.DeepClone( town2 );
        duplicate2.Stats.Should().Be( town2.Stats );
    }

}
