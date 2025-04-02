using CK.Core;
using NUnit.Framework;
using Shouldly;
using System;
using static CK.Testing.MonitorTestHelper;

namespace CK.BinarySerialization.Tests;

[TestFixture]
public partial class SwitchingTypesTests
{
    // Common target: a basic versioned binary serializable object.
    [SerializationVersion( 42 )]
    public sealed class SimplifiedDeserializedObject : ICKVersionedBinarySerializable
    {
        public int Length { get; set; }

        public SimplifiedDeserializedObject( ICKBinaryReader r, int version )
        {
            Throw.CheckArgument( "We don't support anymore lower versions.", version == 42 );
            Length = r.ReadInt32();
        }

        public void WriteData( ICKBinaryWriter w )
        {
            w.Write( Length );
        }
    }

    //
    // Switching type while deserializing is easy: AddDeserializationHook can
    // reroute the local type to be SimplifiedDeserializedObject.
    // But this requires the deserialization layer to know the original type AND
    // to know that it must be switched.
    //
    [SerializationVersion( 42 )]
    public sealed class SomeComplexAliveObject : ICKVersionedBinarySerializable
    {
        public string? Data { get; set; }

        public void WriteData( ICKBinaryWriter w )
        {
            w.Write( Data?.Length ?? -1 );
        }
    }

    [Test]
    public void Saving_a_complex_object_and_deserializing_a_simple_one()
    {
        var origin = new SomeComplexAliveObject { Data = "Hello!" };

        static void SwitchType( IMutableTypeReadInfo i )
        {
            if( i.WrittenInfo.TypeName == "SwitchingTypesTests+SomeComplexAliveObject" )
            {
                i.SetTargetType( typeof( SimplifiedDeserializedObject ) );
            }
        }

        var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
        dC.Shared.AddDeserializationHook( SwitchType );

        var changed = TestHelper.SaveAndLoadAny( origin, deserializerContext: dC );
        changed.ShouldBeOfType<SimplifiedDeserializedObject>().Length.ShouldBe( 6 );
    }


    //
    // Switching type when writing is more powerful.
    // The following code demonstrates this approach.
    //
    public sealed class SomeComplexAliveObjectRerouted
    {
        public string? Data { get; set; }

        public SomeComplexAliveObjectRerouted? Next { get; set; }

        // Systematic global registration of the AsVersioned driver.
        static SomeComplexAliveObjectRerouted()
        {
            // Here we don't want to lock the BinarySerializer.DefaultSharedContext of the tests
            // (AddSerializationDriver for a type can be done only once).
            // But in real life, this must be done.

            // BinarySerializer.DefaultSharedContext.AddSerializationDriver( typeof( SomeComplexAliveObjectRerouted ), new AsVersioned() );
        }

        // This should be private
        public
        // It is public only for the test.
        sealed class AsVersioned : ReferenceTypeSerializer<SomeComplexAliveObjectRerouted>, ISerializationDriverTypeRewriter
        {
            public override string DriverName => "VersionedBinarySerializable";

            // This is the version target: this enable versioning to be controlled and maintain.
            public override int SerializationVersion => 42;

            public Type GetTypeToWrite( Type type )
            {
                Throw.DebugAssert( "Here it is simple. But this can be more complex.",
                                   type == typeof( SomeComplexAliveObjectRerouted ) );
                return typeof( SimplifiedDeserializedObject );
            }

            // The binary layout must obviously be the same as the target.
            protected override void Write( IBinarySerializer s, in SomeComplexAliveObjectRerouted o )
            {
                int length = o.Data?.Length ?? 0;
                var next = o.Next;
                while( next != null )
                {
                    length += next.Data?.Length ?? 0;
                    next = next.Next;
                }
                s.Writer.Write( length );
            }
        }
    }

    [Test]
    public void Saving_a_complex_object_as_a_simple_one()
    {
        var o1 = new SomeComplexAliveObjectRerouted { Data = "Hello" };
        var o2 = new SomeComplexAliveObjectRerouted { Data = "World!", Next = o1 };
        object[] both = [o1, o2];
        object[] graph = [o1, o2, both];

        // Using an independent shared context.
        var sharedContext = new SharedBinarySerializerContext( knownObjects: SharedSerializerKnownObject.Default );
        sharedContext.AddSerializationDriver( typeof( SomeComplexAliveObjectRerouted ), new SomeComplexAliveObjectRerouted.AsVersioned() );
        var sC = new BinarySerializerContext( sharedContext );

        var changed = TestHelper.SaveAndLoadObject( graph, serializerContext: sC );
        changed[0].ShouldBeOfType<SimplifiedDeserializedObject>().Length.ShouldBe( 5 );
        changed[1].ShouldBeOfType<SimplifiedDeserializedObject>().Length.ShouldBe( 11 );
        changed[2].ShouldBeOfType<object[]>()[0].ShouldBeSameAs( changed[0] );
        changed[2].ShouldBeOfType<object[]>()[1].ShouldBeSameAs( changed[1] );
    }
}
