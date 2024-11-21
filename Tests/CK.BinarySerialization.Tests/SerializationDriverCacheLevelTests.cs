using NUnit.Framework;
using System;
using System.Collections.Generic;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using FluentAssertions;
using System.Diagnostics;
using System.Linq;
using System.Collections;

namespace CK.BinarySerialization.Tests;

[TestFixture]
public class SerializationDriverCacheLevelTests
{
    class Thing
    {
        public string? Name { get; set; }

        public string? FromWriter { get; set; }
    }

    static string? ICanChange = null;
    static SerializationDriverCacheLevel WriterCacheLevel = SerializationDriverCacheLevel.Never;

    sealed class ThingSerializer : ReferenceTypeSerializer<Thing>
    {
        public ThingSerializer( SerializationDriverCacheLevel cacheLevel )
        {
            Captured = ICanChange;
            CacheLevel = cacheLevel;
        }

        public override string DriverName => "Thing!";

        public override int SerializationVersion => 0;

        public string? Captured { get; }

        public override SerializationDriverCacheLevel CacheLevel { get; }

        protected override void Write( IBinarySerializer s, in Thing o )
        {
            s.Writer.WriteNullableString( o.Name );
            s.Writer.Write( $"Written by '{Captured}'. {GetHashCode()}" );
        }
    }

    /// <summary>
    /// This resolver is contextless (except the static WriterCacheLevel dependency
    /// for tests).
    /// More complex like <see cref="StandardGenericSerializerResolver"/> can depend on
    /// the shared context in order to compose serializers.
    /// </summary>
    sealed class Resolver : ISerializerResolver
    {
        public ISerializationDriver? TryFindDriver( BinarySerializerContext context, Type t )
        {
            if( t == typeof( Thing ) )
            {
                return new ThingSerializer( WriterCacheLevel );
            }
            return null;
        }
    }

    sealed class ThingDeserializer : ReferenceTypeDeserializer<Thing>
    {
        protected override void ReadInstance( ref RefReader r )
        {
            var n = new Thing();
            var d = r.SetInstance( n );
            n.Name = r.Reader.ReadNullableString();
            n.FromWriter = r.Reader.ReadString();
        }
    }

    [Test]
    public void Never_level_always_provides_a_new_Serializer()
    {
        WriterCacheLevel = SerializationDriverCacheLevel.Never;
        var thing = new Thing { Name = "Don't care." };

        // Never mind the context: the serializer is recreated for each written instance.
        var sC = new BinarySerializerContext( new SharedBinarySerializerContext() );
        sC.Shared.AddResolver( new Resolver() );

        var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
        dC.Shared.AddDeserializerDriver( new ThingDeserializer() );

        ICanChange = "First";

        var back = TestHelper.SaveAndLoadObject( thing, sC, dC );
        back.FromWriter.Should().StartWith( "Written by 'First'." );

        ICanChange = "Second";
        var back2 = TestHelper.SaveAndLoadObject( thing, sC, dC );
        back2.FromWriter.Should().StartWith( "Written by 'Second'." );

        // Not that this test works because the StandardGenericSerializerResolver propagates the_Never
        // cache level to the containers it handles: here the ValuTuple`2 serialization driver is also
        // never cached.
        ICanChange = "Even in the same session!";
        var back3 = TestHelper.SaveAndLoadValue( (thing, new Thing()), sC, dC );
        back3.Item1.FromWriter.Should().StartWith( "Written by 'Even in the same session!'." );
        back3.Item2.FromWriter.Should().StartWith( "Written by 'Even in the same session!'." );
        back3.Item2.FromWriter.Should().NotBe( back3.Item1.FromWriter );
    }

    [Test]
    public void Context_level_provides_a_new_Serializer_only_for_different_BinarySerializerContext()
    {
        WriterCacheLevel = SerializationDriverCacheLevel.Context;
        var thing = new Thing { Name = "Don't care." };

        var sharedContext = new SharedBinarySerializerContext();
        sharedContext.AddResolver( new Resolver() );

        var sC1 = new BinarySerializerContext( sharedContext );
        var sC2 = new BinarySerializerContext( sharedContext );

        var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
        dC.Shared.AddDeserializerDriver( new ThingDeserializer() );

        // sC1 will keep the "First".
        ICanChange = "First";
        var back1 = TestHelper.SaveAndLoadObject( thing, sC1, dC );
        back1.FromWriter.Should().StartWith( "Written by 'First'." );

        // sC2 will keep the "Second".
        ICanChange = "Second";
        var back2 = TestHelper.SaveAndLoadObject( thing, sC2, dC );
        back2.FromWriter.Should().StartWith( "Written by 'Second'." );

        ICanChange = "Will be First or Second... not me :(";
        var back1Bis = TestHelper.SaveAndLoadObject( thing, sC1, dC );
        back1Bis.FromWriter.Should().StartWith( "Written by 'First'." );

        var back2Bis = TestHelper.SaveAndLoadObject( thing, sC2, dC );
        back2Bis.FromWriter.Should().StartWith( "Written by 'Second'." );

        ICanChange = "Of course, on a new Context, I'll be here!";
        var sC3 = new BinarySerializerContext( sharedContext );
        var back3 = TestHelper.SaveAndLoadObject( thing, sC3, dC );
        back3.FromWriter.Should().StartWith( "Written by 'Of course, on a new Context, I'll be here!'." );
    }

    [Test]
    public void StandardGenericSerializerResolver_propagates_the_Never_cache_level_to_its_containers()
    {
        var thing = new Thing { Name = "Don't care." };

        var sC = new BinarySerializerContext( new SharedBinarySerializerContext() );
        sC.Shared.AddResolver( new Resolver() );

        var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
        dC.Shared.AddDeserializerDriver( new ThingDeserializer() );

        WriterCacheLevel = SerializationDriverCacheLevel.Never;

        // List<>
        {
            ICanChange = "First";
            var container = new List<Thing> { thing };
            var back = TestHelper.SaveAndLoadObject( container, sC, dC );
            back[0].FromWriter.Should().StartWith( "Written by 'First'." );

            ICanChange = "Second";
            var back2 = TestHelper.SaveAndLoadObject( container, sC, dC );
            back2[0].FromWriter.Should().StartWith( "Written by 'Second'." );
        }
        // Array
        {
            ICanChange = "First";
            var container = new Thing[] { thing };
            var back = TestHelper.SaveAndLoadObject( container, sC, dC );
            back[0].FromWriter.Should().StartWith( "Written by 'First'." );

            ICanChange = "Second";
            var back2 = TestHelper.SaveAndLoadObject( container, sC, dC );
            back2[0].FromWriter.Should().StartWith( "Written by 'Second'." );
        }
        // Dictionary
        {
            ICanChange = "First";
            var container = new Dictionary<string, Thing> { { "a", thing } };
            var back = TestHelper.SaveAndLoadObject( container, sC, dC );
            back["a"].FromWriter.Should().StartWith( "Written by 'First'." );

            ICanChange = "Second";
            var back2 = TestHelper.SaveAndLoadObject( container, sC, dC );
            back2["a"].FromWriter.Should().StartWith( "Written by 'Second'." );
        }
        // KeyValuePair<shared,never>
        {
            ICanChange = "First";
            var container = new KeyValuePair<string, Thing>( "a", thing );
            var back = TestHelper.SaveAndLoadValue( container, sC, dC );
            back.Value.FromWriter.Should().StartWith( "Written by 'First'." );

            ICanChange = "Second";
            var back2 = TestHelper.SaveAndLoadValue( container, sC, dC );
            back2.Value.FromWriter.Should().StartWith( "Written by 'Second'." );
        }
        // KeyValuePair<never,share>
        {
            ICanChange = "First";
            var container = new KeyValuePair<Thing, string>( thing, "a" );
            var back = TestHelper.SaveAndLoadValue( container, sC, dC );
            back.Key.FromWriter.Should().StartWith( "Written by 'First'." );

            ICanChange = "Second";
            var back2 = TestHelper.SaveAndLoadValue( container, sC, dC );
            back2.Key.FromWriter.Should().StartWith( "Written by 'Second'." );
        }
        // Queue<>
        {
            ICanChange = "First";
            var container = new Queue<Thing>();
            container.Enqueue( thing );
            var back = TestHelper.SaveAndLoadObject( container, sC, dC );
            back.Single().FromWriter.Should().StartWith( "Written by 'First'." );

            ICanChange = "Second";
            var back2 = TestHelper.SaveAndLoadObject( container, sC, dC );
            back2.Single().FromWriter.Should().StartWith( "Written by 'Second'." );
        }
        // Stack<>
        {
            ICanChange = "First";
            var container = new Stack<Thing>();
            container.Push( thing );
            var back = TestHelper.SaveAndLoadObject( container, sC, dC );
            back.Single().FromWriter.Should().StartWith( "Written by 'First'." );

            ICanChange = "Second";
            var back2 = TestHelper.SaveAndLoadObject( container, sC, dC );
            back2.Single().FromWriter.Should().StartWith( "Written by 'Second'." );
        }
        // Tuple<never>
        {
            ICanChange = "First";
            var container = new Tuple<Thing>( thing );
            var back = TestHelper.SaveAndLoadObject( container, sC, dC );
            back.Item1.FromWriter.Should().StartWith( "Written by 'First'." );

            ICanChange = "Second";
            var back2 = TestHelper.SaveAndLoadObject( container, sC, dC );
            back2.Item1.FromWriter.Should().StartWith( "Written by 'Second'." );
        }
        // Tuple<shared,never>
        {
            ICanChange = "First";
            var container = new Tuple<string, Thing>( "x", thing );
            var back = TestHelper.SaveAndLoadObject( container, sC, dC );
            back.Item2.FromWriter.Should().StartWith( "Written by 'First'." );

            ICanChange = "Second";
            var back2 = TestHelper.SaveAndLoadObject( container, sC, dC );
            back2.Item2.FromWriter.Should().StartWith( "Written by 'Second'." );
        }
        // Tuple<shared,never,shared,shared>
        {
            ICanChange = "First";
            var container = new Tuple<string, Thing, int, string>( "x", thing, 3, "Hop" );
            var back = TestHelper.SaveAndLoadObject( container, sC, dC );
            back.Item2.FromWriter.Should().StartWith( "Written by 'First'." );

            ICanChange = "Second";
            var back2 = TestHelper.SaveAndLoadObject( container, sC, dC );
            back2.Item2.FromWriter.Should().StartWith( "Written by 'Second'." );
        }
        // ValueTuple<never>
        {
            ICanChange = "First";
            var container = new ValueTuple<Thing>( thing );
            var back = TestHelper.SaveAndLoadValue( container, sC, dC );
            back.Item1.FromWriter.Should().StartWith( "Written by 'First'." );

            ICanChange = "Second";
            var back2 = TestHelper.SaveAndLoadValue( container, sC, dC );
            back2.Item1.FromWriter.Should().StartWith( "Written by 'Second'." );
        }
        // ValueTuple<shared,never>
        {
            ICanChange = "First";
            var container = new ValueTuple<string, Thing>( "x", thing );
            var back = TestHelper.SaveAndLoadValue( container, sC, dC );
            back.Item2.FromWriter.Should().StartWith( "Written by 'First'." );

            ICanChange = "Second";
            var back2 = TestHelper.SaveAndLoadValue( container, sC, dC );
            back2.Item2.FromWriter.Should().StartWith( "Written by 'Second'." );
        }
        // ValueTuple<shared,never,shared,shared>
        {
            ICanChange = "First";
            var container = new ValueTuple<string, Thing, int, string>( "x", thing, 3, "Hop" );
            var back = TestHelper.SaveAndLoadValue( container, sC, dC );
            back.Item2.FromWriter.Should().StartWith( "Written by 'First'." );

            ICanChange = "Second";
            var back2 = TestHelper.SaveAndLoadValue( container, sC, dC );
            back2.Item2.FromWriter.Should().StartWith( "Written by 'Second'." );
        }
    }

}
