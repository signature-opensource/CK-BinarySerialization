using CK.Core;
using FluentAssertions;
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
        TestHelper.SaveAndLoad( s => s.WriteValue( (byte)5 ), d => d.ReadValue<ushort>().Should().BeOfType( typeof( ushort ) ).And.Be( 5 ) );
        TestHelper.SaveAndLoad( s => s.WriteValue( (sbyte)-128 ), d => d.ReadValue<short>().Should().BeOfType( typeof( short ) ).And.Be( -128 ) );
        TestHelper.SaveAndLoad( s => s.WriteValue( (sbyte)-128 ), d => d.ReadValue<int>().Should().BeOfType( typeof( int ) ).And.Be( -128 ) );
        TestHelper.SaveAndLoad( s => s.WriteValue( (byte)5 ), d => d.ReadValue<long>().Should().BeOfType( typeof( long ) ).And.Be( 5 ) );
        TestHelper.SaveAndLoad( s => s.WriteValue( (int)646464664 ), d => d.ReadValue<uint>().Should().BeOfType( typeof( uint ) ).And.Be( 646464664 ) );
        TestHelper.SaveAndLoad( s => s.WriteValue( (uint)1646464664 ), d => d.ReadValue<long>().Should().BeOfType( typeof( long ) ).And.Be( 1646464664 ) );
        TestHelper.SaveAndLoad( s => s.WriteValue( (uint)1646464664 ), d => d.ReadValue<ulong>().Should().BeOfType( typeof( ulong ) ).And.Be( 1646464664 ) );

        FluentActions.Invoking( () =>
            TestHelper.SaveAndLoad( s => s.WriteValue( (sbyte)-5 ), d => d.ReadValue<byte>() ) ).Should().Throw<OverflowException>();

        FluentActions.Invoking( () =>
            TestHelper.SaveAndLoad( s => s.WriteValue( 256 ), d => d.ReadValue<byte>() ) ).Should().Throw<OverflowException>();
    }

}
