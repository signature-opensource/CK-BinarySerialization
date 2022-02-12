using NUnit.Framework;
using System;
using System.Collections.Generic;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using FluentAssertions;

namespace CK.BinarySerialization.Tests
{
    [TestFixture]
    public class SpecificSerializerDeserializerTests
    {
        class Node
        {
            public string? Name { get; set; }

            public Node? Parent { get; set; }
        }

        class NodeSerializer : ReferenceTypeSerializer<Node>
        {
            public override string DriverName => "Node needs Node!";

            public override int SerializationVersion => 3712;

            protected override void Write( IBinarySerializer w, in Node o )
            {
                w.Writer.WriteNullableString( o.Name );
                w.WriteNullableObject( o.Parent );
            }
        }

        class NodeDeserializer : ReferenceTypeDeserializer<Node>
        {
            protected override Node ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo )
            {
                readInfo.SerializationVersion.Should().Be( 3712 );
                return new Node() {  Name = r.Reader.ReadNullableString(), Parent = r.ReadNullableObject<Node>() };
            }
        }

        [Test]
        public void recursive_serializer_and_deserializer()
        {
            var n1 = new Node() { Name = "Top" };
            var n2 = new Node() { Name = "Below", Parent = n1 };
            var n3 = new Node() { Name = "AlsoBelow", Parent = n1 };
            var n4 = new Node() { Name = "Cycle!", Parent = n3 };
            n1.Parent = n4;

            var sReg = new BinarySerializerContext();
            sReg.Add( typeof(Node), new NodeSerializer() );


            object back = TestHelper.SaveAndLoadObject( n1 );
            back.Should().BeEquivalentTo( n1 );
        }

    }
}
