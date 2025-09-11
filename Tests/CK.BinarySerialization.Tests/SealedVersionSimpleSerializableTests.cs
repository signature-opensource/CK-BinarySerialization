using NUnit.Framework;
using System;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using Shouldly;

namespace CK.BinarySerialization.Tests;

[TestFixture]
public class SealedVersionSimpleSerializableTests
{
    [SerializationVersion( 1 )]
    readonly struct ValueType : ICKVersionedBinarySerializable
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

        public ValueType( ICKBinaryReader r, int version )
        {
            Power = r.ReadInt32();
            Name = r.ReadString();
            if( version >= 1 )
            {
                Age = r.ReadNullableInt16();
            }
            else
            {
                Age = null;
            }
        }

        public void WriteData( ICKBinaryWriter w )
        {
            w.Write( Power );
            w.Write( Name ?? "" );
            w.WriteNullableInt16( Age );
        }
    }

    [Test]
    public void value_type_simple_serializable_with_version()
    {
        ValueType v = new ValueType( 31, "Albert", 12 );
        object? backO = TestHelper.SaveAndLoadAny( v );
        backO.ShouldBe( v );
    }

    [SerializationVersion( 0 )]
    sealed class SimpleSealed : ICKVersionedBinarySerializable
    {
        public int Power { get; set; }

        public SimpleSealed()
        {
        }

        public SimpleSealed( ICKBinaryReader r, int version )
        {
            Power = r.ReadInt32();
        }

        public void WriteData( ICKBinaryWriter w )
        {
            w.Write( Power );
        }
    }

    [Test]
    public void reference_type_sealed_serializable()
    {
        var b = new SimpleSealed() { Power = 3712 };
        object? backB = TestHelper.SaveAndLoadObject( b );
        backB.ShouldBeEquivalentTo( b );
    }

    struct MissingVersionValueType : ICKVersionedBinarySerializable
    {
        public void WriteData( ICKBinaryWriter w )
        {
        }
    }

    sealed class MissingVersionReferenceType : ICKVersionedBinarySerializable
    {
        public void WriteData( ICKBinaryWriter w )
        {
        }
    }

    [Test]
    public void version_attribute_is_required()
    {
        var v = new MissingVersionValueType();
        Util.Invokable( () => TestHelper.SaveAndLoadAny( v ) )
            .ShouldThrow<InvalidOperationException>()
            .Message.ShouldMatch( @".*must be decorated with a \[SerializationVersion().*" );

        var o = new MissingVersionReferenceType();
        Util.Invokable( () => TestHelper.SaveAndLoadObject( o ) )
            .ShouldThrow<InvalidOperationException>()
            .Message.ShouldMatch( @".*must be decorated with a \[SerializationVersion().*" );
    }

    class MissingSealedReferenceType : ICKVersionedBinarySerializable
    {
        public void WriteData( ICKBinaryWriter w )
        {
        }
    }

    [Test]
    public void reference_type_MUST_be_sealed()
    {
        var o = new MissingSealedReferenceType();
        Util.Invokable( () => TestHelper.SaveAndLoadObject( o ) )
            .ShouldThrow<InvalidOperationException>()
            .Message.ShouldMatch( ".*It must be a sealed class or a value type.*" );
    }


    [SerializationVersion( 0 )]
    struct MissingCtorValueType : ICKVersionedBinarySerializable
    {
        public void WriteData( ICKBinaryWriter w )
        {
        }
    }

    [SerializationVersion( 0 )]
    sealed class MissingCtorReferenceType : ICKVersionedBinarySerializable
    {
        public void WriteData( ICKBinaryWriter w )
        {
        }
    }

    [Test]
    public void constructor_with_IBinaryReader_and_int_version_is_required()
    {
        var v = new MissingCtorValueType();
        Util.Invokable( () => TestHelper.SaveAndLoadAny( v ) )
            .ShouldThrow<InvalidOperationException>();

        var o = new MissingCtorReferenceType();
        Util.Invokable( () => TestHelper.SaveAndLoadObject( o ) )
            .ShouldThrow<InvalidOperationException>();
    }


}
