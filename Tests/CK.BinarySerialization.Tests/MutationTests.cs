using NUnit.Framework;
using System;
using System.Collections.Generic;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using FluentAssertions;

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
                    i.SetLocalType( typeof( NewGranteLevel ) );
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
                    i.SetLocalType( typeof( NewGranteLevelIsNowAnInt ) );
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
                    i.SetLocalType( typeof( NowItsAInt ) );
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

        [SerializationVersion( 0 )]
        struct ThingStruct : ICKVersionedBinarySerializable
        {
            public readonly string Name;

            public ThingStruct( string name )
            {
                Name = name;
            }

            public ThingStruct( ICKBinaryReader r, int version )
            {
                Name = r.ReadString();
            }

            public void Write( ICKBinaryWriter w )
            {
                w.Write( Name );
            }
        }

        [SerializationVersion( 0 )]
        sealed class ThingSealedClass : ICKVersionedBinarySerializable
        {
            public string Name { get; }

            public ThingSealedClass( string name )
            {
                Name = name;
            }

            public ThingSealedClass( ICKBinaryReader r, int version )
            {
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
                    i.SetLocalType( typeof( ThingSealedClass ) );
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
                    i.SetLocalType( typeof( ThingStruct ) );
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
    }
}