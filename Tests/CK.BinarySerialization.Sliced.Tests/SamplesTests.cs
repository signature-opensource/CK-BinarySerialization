using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using static CK.Testing.MonitorTestHelper;
using CK.Core;

namespace CK.BinarySerialization.Tests
{
    [TestFixture]
    public class SamplesTests
    {

        [Test]
        public void hierarchy_root_only_serialization()
        {
            var o = new Samples.Person() { Name = "Albert" };
            object? backO = TestHelper.SaveAndLoadAny( o );
            backO.Should().Be( o );
        }

    }
}
