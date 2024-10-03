using NUnit.Framework;
using System;
using System.Collections.Generic;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using FluentAssertions;

namespace CK.BinarySerialization.Tests;

[TestFixture]
public class TupleSerializationTests
{

    [Test]
    public void ValueTuple_serialization()
    {
        {
            var o = new ValueTuple<short>( 23 );
            var b = TestHelper.SaveAndLoadValue( o );
            b.Should().Be( o );
        }
        {
            var o = (23, 56);
            var b = TestHelper.SaveAndLoadValue( o );
            b.Should().Be( o );
        }
    }

    [Test]
    public void Tuple_serialization()
    {
        {
            var o = Tuple.Create( (ushort)23 );
            var b = TestHelper.SaveAndLoadObject( o );
            b.Should().Be( o );
        }
        {
            var o = Tuple.Create( 23, 56 );
            var b = TestHelper.SaveAndLoadObject( o );
            b.Should().Be( o );
        }
    }

}
