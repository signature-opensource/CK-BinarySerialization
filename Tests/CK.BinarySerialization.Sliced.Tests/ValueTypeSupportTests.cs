using NUnit.Framework;
using CK.Core;
using FluentAssertions;
using static CK.Testing.MonitorTestHelper;

namespace CK.BinarySerialization.Sliced.Tests
{
    [TestFixture]
    public class ValueTypeSupportTests
    {
        [SerializationVersion(0)]
        struct Simple : ICKSlicedSerializable
        {
            public int One;
            public int Two;

            public Simple( IBinaryDeserializer d, TypeReadInfo info )
            {
                One = d.Reader.ReadInt32();
                Two = d.Reader.ReadInt32();
            }

            public static void Write( Simple o, IBinarySerializer s )
            {
                s.Writer.Write( o.One );
                s.Writer.Write( o.Two );
            }
        }

        [Test]
        public void value_types_are_supported()
        {
            var o = new Simple() { One = 3712, Two = 42 };
            object? backO = TestHelper.SaveAndLoadAny( o );
            backO.Should().Be( o );
        }
    }
}
