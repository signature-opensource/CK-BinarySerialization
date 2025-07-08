using NUnit.Framework;
using System;
using CK.Core;
using static CK.Testing.MonitorTestHelper;

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
            b.ShouldBe( o );
        }
        {
            var o = (23, 56);
            var b = TestHelper.SaveAndLoadValue( o );
            b.ShouldBe( o );
        }
    }

    [Test]
    public void Tuple_serialization()
    {
        {
            var o = Tuple.Create( (ushort)23 );
            var b = TestHelper.SaveAndLoadObject( o );
            b.ShouldBe( o );
        }
        {
            var o = Tuple.Create( 23, 56 );
            var b = TestHelper.SaveAndLoadObject( o );
            b.ShouldBe( o );
        }
    }

}
