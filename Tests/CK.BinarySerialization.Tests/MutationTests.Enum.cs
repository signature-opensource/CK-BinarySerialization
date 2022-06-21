using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.BinarySerialization.Tests
{
    [TestFixture]
    public partial class MutationTests
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

    }
}