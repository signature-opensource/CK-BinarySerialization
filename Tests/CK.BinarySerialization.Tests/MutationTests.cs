using NUnit.Framework;
using System;
using System.Collections.Generic;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using FluentAssertions;
using System.Diagnostics;
using System.Linq;

namespace CK.BinarySerialization.Tests
{
    [TestFixture]
    public class MutationTests
    {
        enum NewGranteLevel : byte
        {
            NewNameOfAdmin = 127
        }

        [Test]
        public void enum_renamed_and_moved()
        {
            var o = GrantLevel.Administrator;

            static void SetNewLocalType( IMutableTypeReadInfo i )
            {
                if( i.ReadInfo.AssemblyName == "CK.Core" && i.ReadInfo.TypeName == "GrantLevel" )
                {
                    i.SetTargetType( typeof( NewGranteLevel ) );
                }
            }
            // Since we don't want to pollute the default shared cache, we use a brand new one.
            var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
            dC.Shared.AddDeserializationHook( SetNewLocalType );

            object back = TestHelper.SaveAndLoadAny( o, deserializerContext: dC );
            back.Should().BeOfType<NewGranteLevel>();
            back.Should().Be( NewGranteLevel.NewNameOfAdmin );
        }

        enum NewGranteLevelIsNowAnInt : int
        {
            NewNameOfAdmin = 127
        }

        [Test]
        public void enum_changed_its_underlying_type_for_a_wider_type_is_always_fine()
        {
            var o = GrantLevel.Administrator;

            static void SetNewLocalType( IMutableTypeReadInfo i )
            {
                if( i.ReadInfo.AssemblyName == "CK.Core" && i.ReadInfo.TypeName == "GrantLevel" )
                {
                    i.SetTargetType( typeof( NewGranteLevelIsNowAnInt ) );
                }
            }
            var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
            dC.Shared.AddDeserializationHook( SetNewLocalType );

            object back = TestHelper.SaveAndLoadAny( o, deserializerContext: dC );
            back.Should().BeOfType<NewGranteLevelIsNowAnInt>();
            back.Should().Be( NewGranteLevelIsNowAnInt.NewNameOfAdmin );
        }

        enum BeforeItWasALong : long
        {
            FitInByte = byte.MaxValue,
            FitInInt = int.MaxValue,
            FitInLongOnly = long.MaxValue
        }

        enum NowItsAInt : int
        {
            FitInByte = byte.MaxValue,
            FitInInt = int.MaxValue
        }

        [Test]
        public void enum_changed_its_underlying_type_to_a_narrower_type_must_not_overflow()
        {
            static void SetNewLocalType( IMutableTypeReadInfo i )
            {
                if( i.ReadInfo.TypeName == "MutationTests+BeforeItWasALong" )
                {
                    i.SetTargetType( typeof( NowItsAInt ) );
                }
            }
            var dC = new BinaryDeserializerContext();
            dC.Shared.AddDeserializationHook( SetNewLocalType );

            var noWay = BeforeItWasALong.FitInLongOnly;

            FluentActions.Invoking( () => TestHelper.SaveAndLoadAny( noWay, deserializerContext: dC ) )
                .Should().Throw<OverflowException>();

            var canDoIt = BeforeItWasALong.FitInInt;
            var canAlsoDoIt = BeforeItWasALong.FitInByte;

            object back = TestHelper.SaveAndLoadAny( canDoIt, deserializerContext: dC );
            back.Should().BeOfType<NowItsAInt>();
            back.Should().Be( NowItsAInt.FitInInt );

            back = TestHelper.SaveAndLoadAny( canAlsoDoIt, deserializerContext: dC );
            back.Should().BeOfType<NowItsAInt>();
            back.Should().Be( NowItsAInt.FitInByte );
        }

        [SerializationVersion( 42 )]
        struct ThingStruct : ICKVersionedBinarySerializable
        {
            public readonly string Name;

            public ThingStruct( string name )
            {
                Name = name;
            }

            public ThingStruct( ICKBinaryReader r, int version )
            {
                version.Should().Be( 42 );
                Name = r.ReadString();
            }

            public void Write( ICKBinaryWriter w )
            {
                w.Write( Name );
            }
        }

        [SerializationVersion( 42 )]
        sealed class ThingSealedClass : ICKVersionedBinarySerializable
        {
            public string Name { get; }

            public ThingSealedClass( string name )
            {
                Name = name;
            }

            public ThingSealedClass( ICKBinaryReader r, int version )
            {
                version.Should().Be( 42 );  
                Name = r.ReadString();
            }

            public void Write( ICKBinaryWriter w )
            {
                w.Write( Name );
            }
        }


        [Test]
        public void from_struct_to_sealed_class_using_ICKVersionedBinarySerializable()
        {
            static void SetNewLocalType( IMutableTypeReadInfo i )
            {
                if( i.ReadInfo.TypeName == "MutationTests+ThingStruct" )
                {
                    i.SetTargetType( typeof( ThingSealedClass ) );
                }
            }
            var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
            dC.Shared.AddDeserializationHook( SetNewLocalType );

            var t = new ThingStruct( "Spi" );
            object backT = TestHelper.SaveAndLoadAny( t, deserializerContext: dC );
            var tC = (ThingSealedClass)backT;
            tC.Name.Should().Be( t.Name );

            var tA = new ThingStruct[] { new ThingStruct( "n°1" ), new ThingStruct( "n°2" ) };
            object backA = TestHelper.SaveAndLoadAny( tA, deserializerContext: dC );
            var tAC = (ThingSealedClass[])backA;
            tAC.Length.Should().Be( 2 );
            tAC[0].Name.Should().Be( tA[0].Name );
            tAC[1].Name.Should().Be( tA[1].Name );
        }

        [Test]
        public void from_sealed_class_to_nullable_struct_using_ICKVersionedBinarySerializable()
        {
            static void SetNewLocalType( IMutableTypeReadInfo i )
            {
                if( i.ReadInfo.TypeName == "MutationTests+ThingSealedClass" )
                {
                    i.SetTargetType( typeof( ThingStruct ) );
                }
            }
            var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
            dC.Shared.AddDeserializationHook( SetNewLocalType );

            var t = new ThingSealedClass( "Spi" );
            object backT = TestHelper.SaveAndLoadAny( t, deserializerContext: dC );
            var tC = (ThingStruct)backT;
            tC.Name.Should().Be( t.Name );

            var tA = new ThingSealedClass[] { new ThingSealedClass( "n°1" ), new ThingSealedClass( "n°2" ) };
            object backA = TestHelper.SaveAndLoadAny( tA, deserializerContext: dC );
            var tAC = (ThingStruct?[])backA;
            tAC.Length.Should().Be( 2 );
            tAC[0]!.Value.Name.Should().Be( tA[0].Name );
            tAC[1]!.Value.Name.Should().Be( tA[1].Name );
        }


        // A ICKSimpleBinarySerializable handles its version alone.
        // Let's say its currently 4.
        struct AnotherThingStructSimple : ICKSimpleBinarySerializable
        {
            public readonly string Name;

            public AnotherThingStructSimple( string name )
            {
                Name = name;
            }

            public AnotherThingStructSimple( ICKBinaryReader r )
            {
                int v = r.ReadByte();
                // Handles previous version as needed here.
                Name = r.ReadString();
            }

            public void Write( ICKBinaryWriter w )
            {
                w.Write( (byte)4 ); // Current version.
                w.Write( Name );
            }
        }

        // The ICKVersionedBinarySerializable can start at 0 (as usual).
        // But it is simpler and more readable to set it to the next version.
        [SerializationVersion(5)]
        sealed class AnotherThingButClassAndVersioned : ICKVersionedBinarySerializable
        {
            public readonly string Name;
            public string V5HasANewProp { get; set; }

            public AnotherThingButClassAndVersioned( string name, string newProp )
            {
                if( newProp == null ) throw new ArgumentNullException( nameof( newProp ) );
                Name = name;
                V5HasANewProp = newProp;
            }

            public AnotherThingButClassAndVersioned( ICKBinaryReader r, int version )
            {
                int actualVersion;
                if( version == -1 )
                {
                    // We are coming from the Simple: reads the version byte.
                    actualVersion = r.ReadByte();
                }
                else
                {
                    // Coming from the Versioned.
                    actualVersion = version;
                }
                // (Handle the different versions as needed.)
                actualVersion.Should().BeInRange( 0, 5 );
                if( version >= 5 )
                {
                    V5HasANewProp = r.ReadString();
                }
                else
                {
                    V5HasANewProp = "";
                }
                Name = r.ReadString();
            }

            public void Write( ICKBinaryWriter w )
            {
                // No more version to write.
                w.Write( V5HasANewProp );
                w.Write( Name );
            }
        }

        [Test]
        public void from_Simple_to_Versioned_BinarySerializable()
        {
            static void SetNewLocalType( IMutableTypeReadInfo i )
            {
                if( i.ReadInfo.TypeName == "MutationTests+AnotherThingStructSimple" )
                {
                    i.SetTargetType( typeof( AnotherThingButClassAndVersioned ) );
                }
            }
            var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
            dC.Shared.AddDeserializationHook( SetNewLocalType );

            var t = new AnotherThingStructSimple( "Spi" );
            BinarySerializer.IdempotenceCheck( t );
            
            object backT = TestHelper.SaveAndLoadAny( t, deserializerContext: dC );
            var tC = (AnotherThingButClassAndVersioned)backT;
            tC.Name.Should().Be( t.Name );
            BinarySerializer.IdempotenceCheck( tC );

            var tA = new AnotherThingStructSimple[] { new AnotherThingStructSimple( "n°1" ), new AnotherThingStructSimple( "n°2" ) };
            BinarySerializer.IdempotenceCheck( tA );
            
            object backA = TestHelper.SaveAndLoadAny( tA, deserializerContext: dC );
            var tAC = (AnotherThingButClassAndVersioned?[])backA;
            tAC.Length.Should().Be( 2 );
            tAC[0]!.Name.Should().Be( tA[0].Name );
            tAC[1]!.Name.Should().Be( tA[1].Name );
            BinarySerializer.IdempotenceCheck( tAC );
        }

        // Required step: you need to be sure that only this one will exist in the wild
        // before switching back to Simple!
        // (We don't need the V5HasANewProp now.)
        [SerializationVersion( 6 )]
        sealed class AnotherThingVersionedThatWantsToGoBackToSimple : ICKVersionedBinarySerializable
        {
            public readonly string Name;

            public AnotherThingVersionedThatWantsToGoBackToSimple( string name )
            {
                Name = name;
            }

            public AnotherThingVersionedThatWantsToGoBackToSimple( ICKBinaryReader r, int version )
            {
                int actualVersion;
                if( version == -1 )
                {
                    // We are coming from the initial Simple: reads the version byte.
                    actualVersion = r.ReadByte();
                }
                else
                {
                    // Coming from the Versioned.
                    if( version == 5 )
                    {
                        // Coming from the AnotherThingButClassAndVersioned, the one before deciding that Simple will be better.
                        // No future byte version to read.
                        actualVersion= version;
                    }
                    else
                    {
                        // The current version, the one ready to be muted into Simple: it has its version byte.
                        version.Should().Be( 6 );
                        // We can read (or simply skip) the future simple version byte (that is 6).
                        actualVersion = r.ReadByte();
                    }
                }
                // (Handle the different versions as needed.)
                actualVersion.Should().BeInRange( 0, 6 );
                if( version == 5 )
                {
                    // Skip the property that has disappeared.
                    r.ReadString();
                }
                Name = r.ReadString();
            }

            public void Write( ICKBinaryWriter w )
            {
                // Prepare the future by writing the version byte.
                w.Write( (byte)6 );
                w.Write( Name );
            }
        }

        // This can be used ONLY once the version 6 is everywhere!
        // You should not reset the version to 0 for this one unless you remember that 
        // 6 was the "special back to Simple" version (and skip it one you reach 6 again).
        readonly struct AnotherThingBackToSimple : ICKSimpleBinarySerializable
        {
            public readonly string Name;

            public AnotherThingBackToSimple( string name )
            {
                Name = name;
            }

            public AnotherThingBackToSimple( ICKBinaryReader r )
            {
                int v = r.ReadByte();
                Debug.Assert( v >= 6 );
                Name = r.ReadString();
            }

            public void Write( ICKBinaryWriter w )
            {
                w.Write( (byte)7 ); // The new Simple version.
                w.Write( Name );
            }
        }


        [Test]
        public void from_Versioned_to_Simple_BinarySerializable_requires_a_resynchronization_point()
        {
            // This is not realistic but works: here the different versions of the "same actual type" coexist.
            // They all be reloaded as AnotherThingVersionedThatWantsToGoBackToSimple.
            var tA = new object[] { new AnotherThingStructSimple( "n°1" ),
                                    new AnotherThingButClassAndVersioned( "n°2", "new Prop" ),
                                    new AnotherThingVersionedThatWantsToGoBackToSimple( "n°3" )
                                  };
            // Without hooks, they remain what they are.
            BinarySerializer.IdempotenceCheck( tA );

            // The hook maps to the last version.
            static void SetNewLocalType( IMutableTypeReadInfo i )
            {
                if( i.ReadInfo.TypeName == "MutationTests+AnotherThingStructSimple"
                    || i.ReadInfo.TypeName == "MutationTests+AnotherThingButClassAndVersioned" )
                {
                    i.SetTargetType( typeof( AnotherThingVersionedThatWantsToGoBackToSimple ) );
                }
            }
            var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
            dC.Shared.AddDeserializationHook( SetNewLocalType );

            var backA = TestHelper.SaveAndLoadObject( tA, deserializerContext: dC );
            backA.Length.Should().Be( 3 );
            backA.Should().AllBeOfType<AnotherThingVersionedThatWantsToGoBackToSimple>();
            backA.Cast<AnotherThingVersionedThatWantsToGoBackToSimple>().Select( s => s.Name ).Should().BeEquivalentTo( new[] { "n°1", "n°2", "n°3" } );

            BinarySerializer.IdempotenceCheck( backA );

            // Once we have homogeneous version. We can add a hook to the final new Simple.
            // We can reuse the same context: the previous hook will do nothing. 
            static void SetLastSimple( IMutableTypeReadInfo i )
            {
                if( i.ReadInfo.TypeName == "MutationTests+AnotherThingVersionedThatWantsToGoBackToSimple" )
                {
                    i.SetTargetType( typeof( AnotherThingBackToSimple ) );
                }
            }
            dC.Shared.AddDeserializationHook( SetLastSimple );

            object[] backFinalObjects = TestHelper.SaveAndLoadObject( backA, deserializerContext: dC );
            backFinalObjects.Should().AllBeOfType<AnotherThingBackToSimple>();
            backFinalObjects.Cast<AnotherThingBackToSimple>().Select( s => s.Name ).Should().BeEquivalentTo( new[] { "n°1", "n°2", "n°3" } );
        }


    }
}