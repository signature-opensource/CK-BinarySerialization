using FluentAssertions;
using NUnit.Framework;
using System;

namespace CK.BinarySerialization.Tests
{
    [TestFixture]
    public class BasicTypeTests
    {
        [Test]
        public void TypeReadInfo_with_generics()
        {
            var c = typeof( Samples.ClosedSpecializedList );
            c.GetGenericArguments().Should().BeEmpty();
            c.BaseType!.GetGenericArguments().Should().BeEquivalentTo( new[] { typeof( int ) } );

            var o = typeof( Samples.OpenedSpecializedList<int> );
            o.GetGenericArguments().Should().BeEquivalentTo( new[] { typeof( int ) } );
            o.BaseType!.GetGenericArguments().Should().BeEquivalentTo( new[] { typeof( int ) } );
        }

        public void basic_types_testand_DebugMode()
        {

        }
    }
}

