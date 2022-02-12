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
            static void SetNewLocalType( IBinaryDeserializer r, IMutableTypeReadInfo i )
            {
                if( i.ReadInfo.AssemblyName == "CK.Core" && i.ReadInfo.TypeName == "GrantLevel" )
                {
                    i.SetLocalType( typeof( NewGranteLevel ) );
                }
            }

            object back = TestHelper.SaveAndLoadObject( o, onReadType: SetNewLocalType );
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
            static void SetNewLocalType( IBinaryDeserializer r, IMutableTypeReadInfo i )
            {
                if( i.ReadInfo.AssemblyName == "CK.Core" && i.ReadInfo.TypeName == "GrantLevel" )
                {
                    i.SetLocalType( typeof( NewGranteLevelIsNowAnInt ) );
                }
            }

            object back = TestHelper.SaveAndLoadObject( o, onReadType: SetNewLocalType );
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
            static void SetNewLocalType( IBinaryDeserializer r, IMutableTypeReadInfo i )
            {
                if( i.ReadInfo.TypeName == "MutationTests+BeforeItWasALong" )
                {
                    i.SetLocalType( typeof( NowItsAInt ) );
                }
            }

            var noWay = BeforeItWasALong.FitInLongOnly;

            FluentActions.Invoking( () => TestHelper.SaveAndLoadObject( noWay, onReadType: SetNewLocalType ) )
                .Should().Throw<OverflowException>();

            var canDoIt = BeforeItWasALong.FitInInt;
            var canAlsoDoIt = BeforeItWasALong.FitInByte;

            object back = TestHelper.SaveAndLoadObject( canDoIt, onReadType: SetNewLocalType );
            back.Should().BeOfType<NowItsAInt>();
            back.Should().Be( NowItsAInt.FitInInt );
            
            back = TestHelper.SaveAndLoadObject( canAlsoDoIt, onReadType: SetNewLocalType );
            back.Should().BeOfType<NowItsAInt>();
            back.Should().Be( NowItsAInt.FitInByte );
        }

    }
}
