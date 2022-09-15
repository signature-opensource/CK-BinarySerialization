using CK.Core;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.ComponentModel;
using static CK.Testing.StObjEngineTestHelper;

namespace CK.BinarySerialization.Poco.Tests
{
    public class SimpleTests
    {
        public interface ISimple : IPoco
        {
            [DefaultValue( "Hello!" )]
            string Thing { get; set; }
        }


        [Test]
        public void serialization_and_deserialization()
        {
            var c = TestHelper.CreateStObjCollector( typeof( ISimple ),
                                                     typeof( PocoJsonSerializer ),
                                                     typeof( PocoDirectory ) );
            using var s = TestHelper.CreateAutomaticServices( c ).Services;

            var o1 = s.GetRequiredService<PocoDirectory>().Create<ISimple>();
            var o2 = s.GetRequiredService<PocoDirectory>().Create<ISimple>( o => o.Thing = "Goodbye!" );

            // The de/serializer contexts' services must contain the PocoDirectory.
            var dC = new BinaryDeserializerContext( BinaryDeserializer.DefaultSharedContext, s );
            var sC = new BinarySerializerContext( BinarySerializer.DefaultSharedContext, s );

            object? backO1 = TestHelper.SaveAndLoadAny( o1, sC, dC );
            backO1.Should().NotBeSameAs( o1 );
            backO1.Should().BeEquivalentTo( o1 );

            object? backO2 = TestHelper.SaveAndLoadAny( o2, sC, dC );
            backO2.Should().NotBeSameAs( o2 );
            backO2.Should().BeEquivalentTo( o2 );

            BinarySerializer.IdempotenceCheck( o1, sC, dC );
            BinarySerializer.IdempotenceCheck( o2, sC, dC );
        }

    }
}
