using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static CK.Testing.MonitorTestHelper;


namespace CK.BinarySerialization.Tests;

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


    public sealed class DeviceEvent : ICKSimpleBinarySerializable
    {
        public DeviceEvent()
        {
        }

        public DeviceEvent( ICKBinaryReader r )
        {
            r.ReadByte();
        }

        public void Write( ICKBinaryWriter w )
        {
            w.Write( (byte)0 );
        }
    }

    public readonly struct SimplifiedEvent : ICKSimpleBinarySerializable
    {
        public SimplifiedEvent( ICKBinaryReader r )
        {
            r.ReadByte();
        }

        public void Write( ICKBinaryWriter w )
        {
            w.Write( (byte)0 );
        }
    }

    [SerializationVersion( 0 )]
    public class Channel<T> : ICKSlicedSerializable
    {
        public T? Last;

        public void Add( T t ) => Last = t;

        public Channel()
        {
        }

        public Channel( IBinaryDeserializer d, ITypeReadInfo info )
        {
            // Instead of using object ReadAnyNullable() (and casting), we use the
            // typed version: the target type is known and it is up to the deserialzation driver
            // to handle the binary data.
            // Here it works because the binary layout is the same for the DeviceEvent and the SimplifiedEvent
            //(there is only the version byte).
            Last = d.ReadAnyNullable<T>();
        }

        public static void Write( IBinarySerializer s, in Channel<T> o )
        {
            s.WriteAnyNullable( o.Last );
        }
    }

    [SerializationVersion( 0 )]
    public sealed class Holder : ICKSlicedSerializable
    {
        public readonly Channel<DeviceEvent> Events;

        public Holder()
        {
            Events = new Channel<DeviceEvent>();
        }

        public Holder( IBinaryDeserializer d, ITypeReadInfo info )
        {
            Events = d.ReadObject<Channel<DeviceEvent>>();
        }

        public static void Write( IBinarySerializer s, in Holder o )
        {
            s.WriteObject( o.Events );
        }
    }

    [SerializationVersion( 0 )]
    public sealed class HolderV2 : ICKSlicedSerializable
    {
        public readonly Channel<SimplifiedEvent> Events;

        public HolderV2()
        {
            Events = new Channel<SimplifiedEvent>();
        }

        public HolderV2( IBinaryDeserializer d, ITypeReadInfo info )
        {
            Events = d.ReadObject<Channel<SimplifiedEvent>>();
        }

        public static void Write( IBinarySerializer s, in HolderV2 o )
        {
            s.WriteObject( o.Events );
        }
    }

    [Test]
    public void changing_type_with_same_binary_layout()
    {
        var t = new Holder();
        t.Events.Add( new DeviceEvent() );
        static void SwitchToHolderV2( IMutableTypeReadInfo i )
        {
            if( i.WrittenInfo.TypeName == "MutationTests+Holder" )
            {
                i.SetLocalTypeName( "MutationTests+HolderV2" );
            }
        }
        var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
        dC.Shared.AddDeserializationHook( SwitchToHolderV2 );
        var t2 = TestHelper.SaveAnyAndLoad<HolderV2>( t, new BinarySerializerContext(), deserializerContext: dC );
        t.Events.Last.Should().NotBeNull();
    }

}
