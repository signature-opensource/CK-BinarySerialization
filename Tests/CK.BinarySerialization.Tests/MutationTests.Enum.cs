using CK.Core;
using Shouldly;
using NUnit.Framework;
using System;
using static CK.Testing.MonitorTestHelper;

namespace CK.BinarySerialization.Tests;

[TestFixture]
public partial class MutationTests
{
    enum NewGranteLevel : byte
    {
        SuperEditor = 80,
        NewNameOfAdmin = 127
    }

    [Test]
    public void enum_renamed_and_moved()
    {
        var o = GrantLevel.Administrator;

        static void SetNewLocalType( IMutableTypeReadInfo i )
        {
            if( i.WrittenInfo.AssemblyName == "CK.Core" && i.WrittenInfo.TypeName == "GrantLevel" )
            {
                i.SetTargetType( typeof( NewGranteLevel ) );
            }
        }
        // Since we don't want to pollute the default shared cache, we use a brand new one.
        var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
        dC.Shared.AddDeserializationHook( SetNewLocalType );

        object back = TestHelper.SaveAndLoadAny( o, deserializerContext: dC );
        back.ShouldBeOfType<NewGranteLevel>();
        back.ShouldBe( NewGranteLevel.NewNameOfAdmin );
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
            if( i.WrittenInfo.AssemblyName == "CK.Core" && i.WrittenInfo.TypeName == "GrantLevel" )
            {
                i.SetTargetType( typeof( NewGranteLevelIsNowAnInt ) );
            }
        }
        var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
        dC.Shared.AddDeserializationHook( SetNewLocalType );

        object back = TestHelper.SaveAndLoadAny( o, deserializerContext: dC );
        back.ShouldBeOfType<NewGranteLevelIsNowAnInt>();
        back.ShouldBe( NewGranteLevelIsNowAnInt.NewNameOfAdmin );
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
            if( i.WrittenInfo.TypeName == "MutationTests+BeforeItWasALong" )
            {
                i.SetTargetType( typeof( NowItsAInt ) );
            }
        }
        var dC = new BinaryDeserializerContext();
        dC.Shared.AddDeserializationHook( SetNewLocalType );

        var noWay = BeforeItWasALong.FitInLongOnly;

        Util.Invokable( () => TestHelper.SaveAndLoadAny( noWay, deserializerContext: dC ) )
            .ShouldThrow<DeserializationException>()
                     .InnerException.ShouldBeOfType<OverflowException>();


        var canDoIt = BeforeItWasALong.FitInInt;
        var canAlsoDoIt = BeforeItWasALong.FitInByte;

        object back = TestHelper.SaveAndLoadAny( canDoIt, deserializerContext: dC );
        back.ShouldBeOfType<NowItsAInt>();
        back.ShouldBe( NowItsAInt.FitInInt );

        back = TestHelper.SaveAndLoadAny( canAlsoDoIt, deserializerContext: dC );
        back.ShouldBeOfType<NowItsAInt>();
        back.ShouldBe( NowItsAInt.FitInByte );
    }

    [Test]
    public void enum_type_mutation_when_reading_value_is_possible_even_if_underlying_type_differ()
    {
        TestHelper.SaveAndLoad( s => s.WriteValue( GrantLevel.SuperEditor ), d => d.ReadValue<NewGranteLevel>().ShouldBe( NewGranteLevel.SuperEditor ) );
        TestHelper.SaveAndLoad( s => s.WriteValue( BeforeItWasALong.FitInInt ), d => d.ReadValue<NowItsAInt>().ShouldBe( NowItsAInt.FitInInt ) );
    }

    [Test]
    public void enum_type_and_numeric_type_can_mutate_as_long_as_there_is_no_overflow()
    {
        TestHelper.SaveAndLoad( s => s.WriteValue<long>( 80 ), d => d.ReadValue<GrantLevel>().ShouldBe( GrantLevel.SuperEditor ) );
        TestHelper.SaveAndLoad( s => s.WriteValue( GrantLevel.SuperEditor ), d => d.ReadValue<byte>().ShouldBe( 80 ) );
    }

    //[Test]
    //public void from_value_tuple_to_tuple()
    //{
    //    TestHelper.SaveAndLoad( s => s.WriteValue( (3712, "Hop") ), d => d.ReadObject<Tuple<int,string>>().ShouldBe( Tuple.Create( 3712, "Hop" ) ) );
    //}

    //[Test]
    //public void from_tuple_to_value_tuple()
    //{
    //    TestHelper.SaveAndLoad( s => s.WriteAny( Tuple.Create( 3712, "Hop" ) ), d => d.ReadValue < (int,string)> ().ShouldBe( (3712,"Hop") ) );
    //}

}
