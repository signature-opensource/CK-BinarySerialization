using NUnit.Framework;
using System;
using System.Collections.Generic;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using FluentAssertions;

namespace CK.BinarySerialization.Tests
{
    [TestFixture]
    public class SimpleSerializableTests
    {
        readonly struct ValueType : ICKSimpleBinarySerializable
        {
            public readonly int Power;
            public readonly string Name;
            public readonly short? Age;

            public ValueType( int power, string name, short? age )
            {
                Power = power;
                Name = name;
                Age = age;
            }

            public ValueType( ICKBinaryReader r )
            {
                r.ReadByte(); // Version
                Power = r.ReadInt32();
                Name = r.ReadString();
                Age = r.ReadNullableInt16();
            }

            public void Write( ICKBinaryWriter w )
            {
                w.Write( (byte)0 ); // Version
                w.Write( Power );
                w.Write( Name ?? "" );
                w.WriteNullableInt16( Age );
            }
        }

        [Test]
        public void value_type_simple_serializable()
        {
            ValueType v = new ValueType( 31, "Albert", 12 );
            object? backO = TestHelper.SaveAndLoadObject( v );
            backO.Should().Be( v );
        }

        class SimpleBase : ICKSimpleBinarySerializable
        {
            public int Power { get; set; }

            public SimpleBase()
            {
            }

            public SimpleBase( ICKBinaryReader r )
            {
                r.ReadByte(); // Version
                Power = r.ReadInt32();
            }

            public virtual void Write( ICKBinaryWriter w )
            {
                w.Write( (byte)0 ); // Version
                w.Write( Power );
            }
        }

        class SimpleDerived : SimpleBase
        {
            public string? Name { get; set; }

            public SimpleDerived()
            {
            }

            public SimpleDerived( ICKBinaryReader r )
                : base( r )
            {
                r.ReadByte(); // Version
                Name = r.ReadNullableString();
            }

            public override void Write( ICKBinaryWriter w )
            {
                base.Write( w );
                w.Write( (byte)0 ); // Version
                w.WriteNullableString( Name );
            }
        }

        [Test]
        public void reference_type_simple_serializable()
        {
            var b = new SimpleBase() { Power = 3712 };
            object? backB = TestHelper.SaveAndLoadObject( b );
            backB.Should().BeEquivalentTo( b );
            
            var d = new SimpleDerived() { Power = 3712, Name = "Albert" };
            object? backD = TestHelper.SaveAndLoadObject( d );
            backD.Should().BeEquivalentTo( d );
        }

        struct MissingCtorValueType : ICKSimpleBinarySerializable
        {
            public void Write( ICKBinaryWriter w )
            {
            }
        }

        class MissingCtorReferenceType : ICKSimpleBinarySerializable
        {
            public void Write( ICKBinaryWriter w )
            {
            }
        }

        [Test]
        public void constructor_with_IBinaryReader_is_required()
        {
            var v = new MissingCtorValueType();
            FluentActions.Invoking( () => TestHelper.SaveAndLoadObject( v ) )
                .Should().Throw<InvalidOperationException>()
                .WithMessage( "*requires a constructor with ( ICKBinaryReader ) parameters*" );

            var o = new MissingCtorReferenceType();
            FluentActions.Invoking( () => TestHelper.SaveAndLoadObject( o ) )
                .Should().Throw<InvalidOperationException>()
                .WithMessage( "*requires a constructor with ( ICKBinaryReader ) parameters*" );
        }

        class XA<T1, T2> : ICKSimpleBinarySerializable
        {
            public string A { get; }

            public XA( string a )
            {
                A = a;
            }
            public XA( ICKBinaryReader r )
            {
                A = r.ReadString();
            }

            public virtual void Write( ICKBinaryWriter w )
            {
                w.Write( A );
            }
        }
        class XB<T> : XA<XB<int>, XC>
        {
            public string B { get; }

            public XB( string a, string b )
                : base( a )
            {
                B = b;
            }
            public XB( ICKBinaryReader r )
                : base( r )
            {
                B = r.ReadString();
            }
            public override void Write( ICKBinaryWriter w )
            {
                base.Write( w );
                w.Write( B );
            }

        }
        class XC : XB<int>
        {
            public string C { get; }

            public XC( string a, string b, string c )
                : base( a, b )
            {
                C = c;
            }
            public XC( ICKBinaryReader r )
                : base( r )
            {
                C = r.ReadString();
            }
            public override void Write( ICKBinaryWriter w )
            {
                base.Write( w );
                w.Write( C );
            }
        }

        [Test]
        public void some_complex_generic_types()
        {
            var c = new XC( "Albert", "Berth", "Clio" );
            object? backC = TestHelper.SaveAndLoadObject( c );
            backC.Should().BeEquivalentTo( c );
        }


    }
}
