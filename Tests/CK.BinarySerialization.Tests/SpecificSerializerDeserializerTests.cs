using NUnit.Framework;
using System;
using System.Collections.Generic;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using FluentAssertions;
using System.Diagnostics;

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


        // We don't specify IsCached = false here (via the constructor overload):
        // we accept the true default IsCached. By doing this, even if this
        // driver is resolved in priority (see SharedBinaryDeserializerContext.AddLocalTypeDeserializer)
        // we'll cache any composite drivers that relies on this one like the one for List<Node> for instance.
        // As of today, it would not make a lot of sense to set IsCached to false here since there is no way to remove 
        // or replace a registered AddLocalTypeDeserializer from a shared deserializer context.
        // It this happens to be useful (I doubt that), then we could implement a ReplaceLocalTypeDeserializer and/or
        // RemoveLocalTypeDeserializer that will accept only to update/remove non cached drivers: this will be perfectly
        // coherent (ignoring concurrent issues of updating the share cache when there are deserialization sessions running...)
        // since absolutely no driver that rely on it are cached. 
        class NodeDeserializer : ReferenceTypeDeserializer<Node>
        {
            protected override void ReadInstance( ref RefReader r )
            {
                r.ReadInfo.Version.Should().Be( 3712 );
                var n = new Node();
                var d = r.SetInstance( n );
                n.Name = r.Reader.ReadNullableString();
                n.Parent = d.ReadNullableObject<Node>();
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

            var sC = new BinarySerializerContext( new SharedBinarySerializerContext() );
            sC.Shared.AddSerializationDriver( typeof( Node ), new NodeSerializer() );

            var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
            dC.Shared.AddLocalTypeDeserializer( new NodeDeserializer() );

            Node back = TestHelper.SaveAndLoadObject( n1, sC, dC );
            back.Should().BeEquivalentTo( n1, options => options.IgnoringCyclicReferences() );
        }

        class NodeRoot
        {
            public Node? FirstRoot { get; set; }
            public Node? SecondRoot { get; set; }
            public List<Node>? Nodes { get; set; }
        }

        class NodeRootSerializer : ReferenceTypeSerializer<NodeRoot>
        {
            public override string DriverName => "NodeRoot";

            public override int SerializationVersion => 0;

            protected override void Write( IBinarySerializer w, in NodeRoot o )
            {
                w.WriteNullableObject( o.FirstRoot );
                w.WriteNullableObject( o.SecondRoot );
                w.WriteNullableObject( o.Nodes );
            }
        }

        class NodeRootDeserializer : ReferenceTypeDeserializer<NodeRoot>
        {
            protected override void ReadInstance( ref RefReader r )
            {
                r.ReadInfo.Version.Should().Be( 0 );
                var n = new NodeRoot();
                var d = r.SetInstance( n );
                n.FirstRoot = d.ReadNullableObject<Node>();
                n.SecondRoot = d.ReadNullableObject<Node>();
                n.Nodes = d.ReadNullableObject<List<Node>>();
            }
        }

        [Test]
        public void list_of_references()
        {
            var root = new NodeRoot();
            root.FirstRoot = new Node { Name = "n°1" };
            root.SecondRoot = new Node { Name = "n°2" };
            root.Nodes = new List<Node> { root.FirstRoot, root.SecondRoot };

            var sC = new BinarySerializerContext( new SharedBinarySerializerContext() );
            sC.Shared.AddSerializationDriver( typeof( Node ), new NodeSerializer() );
            sC.Shared.AddSerializationDriver( typeof( NodeRoot ), new NodeRootSerializer() );

            var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
            dC.Shared.AddLocalTypeDeserializer( new NodeDeserializer() );
            dC.Shared.AddLocalTypeDeserializer( new NodeRootDeserializer() );

            NodeRoot back = TestHelper.SaveAndLoadObject( root, sC, dC );
            back.Should().BeEquivalentTo( root );
            Debug.Assert( back.Nodes != null && back.Nodes.Count == 2 );
            back.Nodes[0].Should().BeSameAs( back.FirstRoot );
            back.Nodes[1].Should().BeSameAs( back.SecondRoot );
        }
    }
}
