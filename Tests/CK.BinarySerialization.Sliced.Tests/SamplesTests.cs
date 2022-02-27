using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using static CK.Testing.MonitorTestHelper;
using CK.Core;
using System.Linq;

namespace CK.BinarySerialization.Tests
{
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
            object? backG = TestHelper.SaveAndLoadAny( town );
            backG.Should().BeEquivalentTo( town, o => o.IgnoringCyclicReferences() );

            BinarySerializer.IdempotenceCheck( town );
        }

        [Test]
        public void Destroyable_simple_test()
        {
            var t = new Samples.Town( "Paris" );
            var g = new Samples.Garage( t );
            var e = new Samples.Employee( g ) { Name = "Albert" };
            var m = new Samples.Manager( g ) { Name = "Alice", Rank = 42 };
            e.BestFriend = m;

            var backT = TestHelper.SaveAndLoadObject( t );
            backT.Should().BeEquivalentTo( t, o => o.IgnoringCyclicReferences() );
            var eBack = backT.Persons.OfType<Samples.Employee>().Single( p => p.Name == "Albert" );
            ((Samples.Manager)eBack.BestFriend!).Rank.Should().Be( 42 );

            BinarySerializer.IdempotenceCheck( t );

            m.Destroy();
            backT = TestHelper.SaveAndLoadObject( t );
            backT.Should().BeEquivalentTo( t, o => o.IgnoringCyclicReferences() );

            eBack = backT.Persons.OfType<Samples.Employee>().Single( p => p.Name == "Albert" );
            eBack.BestFriend!.IsDestroyed.Should().BeTrue();
            ((Samples.Manager)eBack.BestFriend!).Rank.Should().Be( 0, "Rank has NOT been read back." );

            BinarySerializer.IdempotenceCheck( t );
        }

    }
}
