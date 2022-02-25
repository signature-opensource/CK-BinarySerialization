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

        [Test]
        public void huge_linked_list_relies_on_IDeserializationDeferredDriver_to_avoid_StackOverflow()
        {
            var garage = new Samples.Garage( new Samples.Town( "BigOne" ) );
            int realize = Enumerable.Range( 0, 2/*100000*/ ).Select( i => new Samples.Employee( garage ) { Name = $"n°{i}", EmployeeNumber = i } ).Count();

            // This creates a linked list of 99999 employees that will
            // be serialized by the employee n°0: without the IDeserializationDeferredDriver this
            // would explode the stack.
            for( int i = 1; i < garage.Employees.Count; i++ )
            {
                garage.Employees[i-1].BestFriend = garage.Employees[i];
            }
            
            object? backG = TestHelper.SaveAndLoadAny( garage );
            backG.Should().BeEquivalentTo( garage, o => o.IgnoringCyclicReferences() );

            BinarySerializer.IdempotenceCheck( garage );
        }

    }
}
