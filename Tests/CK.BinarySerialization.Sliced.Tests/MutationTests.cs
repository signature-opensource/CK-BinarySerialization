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
    public class MutationTests
    {

        [Test]
        public void from_Sliced_to_Versioned()
        {
            var car = new Samples.Car( "Model", DateTime.UtcNow );

            // The Car that was a Sliced sealed class is now a Versioned readonly struct.
            static void SetNewLocalType( IMutableTypeReadInfo i )
            {
                if( i.WrittenInfo.TypeName== "Car" )
                {
                    i.SetTargetType( typeof( SamplesV2.Car ) );
                }
            }
            var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
            dC.Shared.AddDeserializationHook( SetNewLocalType );

            var v2Car = (SamplesV2.Car)TestHelper.SaveAndLoadAny( car, deserializerContext: dC );
            v2Car.Model.Should().Be( "Model" );

            var listOfCars = Enumerable.Range( 0, 20 ).Select( i => new Samples.Car( $"Model n°{i}", DateTime.UtcNow ) ).ToList();

            var v2CarList = (List<SamplesV2.Car?>)TestHelper.SaveAndLoadAny( listOfCars, deserializerContext: dC );
            v2CarList[0]!.Value.Model.Should().Be( "Model n°0" );

        }

        [Test]
        public void from_Sliced_to_Versioned_in_simple_graph()
        {
            var t = new Samples.Town( "Test" );
            var g = new Samples.Garage( t );
            var e = new Samples.Employee( g );
            var c = new Samples.Customer( t );
            var car = t.AddCar( "Model", DateTime.UtcNow );
            c.Car = car;

            var t2 = RunV1ToV2( t, 0 );
            t2.Persons.OfType<SamplesV2.Customer>().Single().Car.Should().BeEquivalentTo( t2.Cars[0] );
        }

        [TestCase( 0 )]
        [TestCase( 1 )]
        [TestCase( 2 )]
        public void from_Sliced_to_Versioned_in_graph( int seed )
        {
            var t = Samples.Town.CreateTown( seed );
            // Always defer reference types that can be deferred.
            RunV1ToV2( t, 0 );
        }

        static SamplesV2.Town RunV1ToV2( Samples.Town t, int maxRecursionDepth )
        {
            // The Car that was a Sliced sealed class is now a Versioned readonly struct.
            static void SwitchToV2( IMutableTypeReadInfo i )
            {
                if( i.WrittenInfo.TypeNamespace == "CK.BinarySerialization.Tests.Samples" )
                {
                    i.SetLocalTypeNamespace( "CK.BinarySerialization.Tests.SamplesV2" );
                }
            }
            var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
            dC.Shared.AddDeserializationHook( SwitchToV2 );

            var v2Town = (SamplesV2.Town)TestHelper.SaveAndLoadAny( t, new BinarySerializerContext() { MaxRecursionDepth = maxRecursionDepth }, deserializerContext: dC );
            v2Town.Cars.Should().AllBeOfType<SamplesV2.Car>();
            v2Town.Stats.Should().Be( t.Stats );
            return v2Town;
        }
    }
}
