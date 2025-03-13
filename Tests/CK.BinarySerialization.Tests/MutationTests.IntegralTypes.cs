using CK.Core;
using Shouldly;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.BinarySerialization.Tests;

[TestFixture]
public partial class MutationTests
{
    [Test]
    public void basic_numeric_types_can_change_thanks_to_Convert_ChangeType_and_must_not_overflow()
    {
        TestHelper.SaveAndLoad( s => s.WriteValue( (byte)5 ),
                                d => d.ReadValue<ushort>().ShouldBe( 5 ) );
        TestHelper.SaveAndLoad( s => s.WriteValue( (sbyte)-128 ),
                                d => d.ReadValue<short>().ShouldBe( -128 ) );
        TestHelper.SaveAndLoad( s => s.WriteValue( (sbyte)-128 ),
                                d => d.ReadValue<int>().ShouldBe( -128 ) );
        TestHelper.SaveAndLoad( s => s.WriteValue( (byte)5 ),
                                d => d.ReadValue<long>().ShouldBe( 5 ) );
        TestHelper.SaveAndLoad( s => s.WriteValue( (int)646464664 ),
                                d => d.ReadValue<uint>().ShouldBe( 646464664 ) );
        TestHelper.SaveAndLoad( s => s.WriteValue( (uint)1646464664 ),
                                d => d.ReadValue<long>().ShouldBe( 1646464664 ) );
        TestHelper.SaveAndLoad( s => s.WriteValue( (uint)1646464664 ),
                                d => d.ReadValue<ulong>().ShouldBe( (uint)1646464664 ) );

        Util.Invokable( () =>
            TestHelper.SaveAndLoad( s => s.WriteValue( (sbyte)-5 ), d => d.ReadValue<byte>() ) ).ShouldThrow<OverflowException>();

        Util.Invokable( () =>
            TestHelper.SaveAndLoad( s => s.WriteValue( 256 ), d => d.ReadValue<byte>() ) ).ShouldThrow<OverflowException>();
    }

}
