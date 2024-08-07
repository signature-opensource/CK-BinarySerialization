using CK.Core;
using CK.Setup;
using CK.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using static CK.Testing.MonitorTestHelper;

namespace CK.BinarySerialization.Poco.Tests
{
    [TestFixture]
    public class SimpleTests
    {
        public interface ISimple : IPoco
        {
            [DefaultValue( "Hello!" )]
            string Thing { get; set; }
        }

        public interface ISimpleMore : ISimple
        {
            [DefaultValue( "World!" )]
            string AnotherThing { get; set; }
        }

        [Test]
        public void serialization_and_deserialization()
        {
            var engineConfiguration = TestHelper.CreateDefaultEngineConfiguration();
            engineConfiguration.FirstBinPath.Types.Add( typeof( ISimple ),
                                                        typeof( CommonPocoJsonSupport ),
                                                        typeof( PocoDirectory ) );
            using var auto = engineConfiguration.RunSuccessfully().CreateAutomaticServices();

            var o1 = auto.Services.GetRequiredService<PocoDirectory>().Create<ISimple>();
            var o2 = auto.Services.GetRequiredService<PocoDirectory>().Create<ISimple>( o => o.Thing = "Goodbye!" );

            // The de/serializer contexts' services must contain the PocoDirectory.
            var dC = new BinaryDeserializerContext( BinaryDeserializer.DefaultSharedContext, auto.Services );
            var sC = new BinarySerializerContext( BinarySerializer.DefaultSharedContext, auto.Services );

            object? backO1 = TestHelper.SaveAndLoadAny( o1, sC, dC );
            backO1.Should().NotBeSameAs( o1 );
            backO1.Should().BeEquivalentTo( o1 );

            object? backO2 = TestHelper.SaveAndLoadAny( o2, sC, dC );
            backO2.Should().NotBeSameAs( o2 );
            backO2.Should().BeEquivalentTo( o2 );

            BinarySerializer.IdempotenceCheck( o1, sC, dC );
            BinarySerializer.IdempotenceCheck( o2, sC, dC );
        }

        [Test]
        public void serialization_and_deserialization_of_list()
        {
            var engineConfiguration = TestHelper.CreateDefaultEngineConfiguration();
            engineConfiguration.FirstBinPath.Types.Add( typeof( ISimple ),
                                                        typeof( CommonPocoJsonSupport ),
                                                        typeof( PocoDirectory ) );
            using var auto = engineConfiguration.RunSuccessfully().CreateAutomaticServices();

            var o1 = auto.Services.GetRequiredService<PocoDirectory>().Create<ISimple>();
            var o2 = auto.Services.GetRequiredService<PocoDirectory>().Create<ISimple>( o => o.Thing = "Goodbye!" );
            var list = new List<ISimple>() { o1, o2 };

            // The de/serializer contexts' services must contain the PocoDirectory.
            var dC = new BinaryDeserializerContext( BinaryDeserializer.DefaultSharedContext, auto.Services );
            var sC = new BinarySerializerContext( BinarySerializer.DefaultSharedContext, auto.Services );

            object? backList = TestHelper.SaveAndLoadAny( list, sC, dC );
            backList.Should().NotBeSameAs( list );

            BinarySerializer.IdempotenceCheck( list, sC, dC );
        }

        public interface IOtherSimple : IPoco
        {
            [DefaultValue( "Hello!" )]
            string Thing { get; set; }
        }

        [Test]
        public void interfaces_are_mapped_to_the_primary()
        {
            var engineConfiguration = TestHelper.CreateDefaultEngineConfiguration();
            engineConfiguration.FirstBinPath.Types.Add( typeof( IOtherSimple ),
                                                     typeof( ISimpleMore ),
                                                     typeof( CommonPocoJsonSupport ),
                                                     typeof( PocoDirectory ) );
            using var auto = engineConfiguration.RunSuccessfully().CreateAutomaticServices();

            var o1 = auto.Services.GetRequiredService<PocoDirectory>().Create<ISimpleMore>();
            var o2 = auto.Services.GetRequiredService<PocoDirectory>().Create<ISimpleMore>( o => { o.Thing = "Goodbye!"; o.AnotherThing = "Universe!"; } );

            // The de/serializer contexts' services must contain the PocoDirectory.
            var dC = new BinaryDeserializerContext( BinaryDeserializer.DefaultSharedContext, auto.Services );
            var sC = new BinarySerializerContext( BinarySerializer.DefaultSharedContext, auto.Services );

            object? backO1 = TestHelper.SaveAndLoadAny( o1, sC, dC );
            backO1.Should().NotBeSameAs( o1 );
            backO1.Should().BeEquivalentTo( o1 );

            object? backO2 = TestHelper.SaveAndLoadAny( o2, sC, dC );
            backO2.Should().NotBeSameAs( o2 );
            backO2.Should().BeEquivalentTo( o2 );

            ViaType<ISimple>( o1, dC, sC );
            ViaType<IPoco>( o1, dC, sC );

            BinarySerializer.IdempotenceCheck( o1, sC, dC );
            BinarySerializer.IdempotenceCheck( o2, sC, dC );

            static void ViaType<T>( T o1, BinaryDeserializerContext dC, BinarySerializerContext sC ) where T : class
            {
                var b = TestHelper.SaveAndLoadObject<T>( o1, sC, dC );
                b.Should().NotBeSameAs( o1 );
                ((ISimpleMore)b).Thing.Should().Be( "Hello!" );
                ((ISimpleMore)b).AnotherThing.Should().Be( "World!" );
                b.ToString().Should().Be( o1.ToString() );
            }
        }

        [Test]
        public void mutations_work_when_Json_can_be_read_back()
        {
            var engineConfiguration = TestHelper.CreateDefaultEngineConfiguration();
            engineConfiguration.FirstBinPath.Types.Add( typeof( IOtherSimple ),
                                                     typeof( ISimpleMore ),
                                                     typeof( CommonPocoJsonSupport ),
                                                     typeof( PocoDirectory ) );
            using var auto = engineConfiguration.RunSuccessfully().CreateAutomaticServices();

            var o1 = auto.Services.GetRequiredService<PocoDirectory>().Create<ISimpleMore>( o1 => o1.Thing = "Hop" );

            // The de/serializer contexts' services must contain the PocoDirectory.
            var dC = new BinaryDeserializerContext( BinaryDeserializer.DefaultSharedContext, auto.Services );
            var sC = new BinarySerializerContext( BinarySerializer.DefaultSharedContext, auto.Services );

            TestHelper.SaveAndLoad( s => s.WriteObject( o1 ), d => d.ReadObject<IOtherSimple>().Should().Match( x => ((IOtherSimple)x).Thing == "Hop" ), sC, dC );
        }

    }
}
