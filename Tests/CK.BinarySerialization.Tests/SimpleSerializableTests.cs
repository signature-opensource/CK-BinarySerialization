using NUnit.Framework;
using System;
using System.Collections.Generic;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using Shouldly;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace CK.BinarySerialization.Tests;

[TestFixture]
public partial class SimpleSerializableTests
{
    interface ILikeSerializer
    {
        int Count { get; set; }
    }

    class LikeSerializerImpl : ILikeSerializer
    {
        public int Count { get; set; }
    }

    delegate void UWriter( ILikeSerializer s, in object o );
    delegate void TWriter<T>( ILikeSerializer s, in T o );

    [Test]
    public void using_Unsafe_as_to_work_around_variance_for_reference_types()
    {
        var oB = new SimpleBase();
        var oD = new SimpleDerived();
        var s = new LikeSerializerImpl();
        TWriter<SimpleBase> bOnB = WriteBase;

        bOnB( s, oB );
        bOnB( s, oD );
        s.Count.ShouldBe( 2 );

        // - No contravariance.
        // TWriter<SimpleDerived> dOnB = WriteBase;
        // TWriter<SimpleDerived> dOnBC = (TWriter<SimpleDerived>)( bOnB );
        // - No covariance.
        // TWriter<object> oOnS = WriteBase;
        // UWriter uOnB = WriteBase;

        TWriter<SimpleDerived> dOnB = Unsafe.As<TWriter<SimpleDerived>>( bOnB );
        dOnB( s, oD );
        s.Count.ShouldBe( 3 );

        TWriter<object> oOnBase = Unsafe.As<TWriter<object>>( bOnB );
        oOnBase( s, oD );
        s.Count.ShouldBe( 4 );

        UWriter uOnBase = Unsafe.As<UWriter>( bOnB );
        uOnBase( s, oD );
        s.Count.ShouldBe( 5 );

        var bug = new object();
        // Safe.
        // bOnB( s, bug );

        // Unsafe means something: this should not work.
        oOnBase( s, bug );
        uOnBase( s, bug );
        s.Count.ShouldBe( 7 );

        static void WriteBase( ILikeSerializer s, in SimpleBase o )
        {
            ++s.Count;
        }

    }




    readonly struct Sample : ICKSimpleBinarySerializable
    {
        public readonly int Power;
        public readonly string Name;
        public readonly short? Age;

        public Sample( int power, string name, short? age )
        {
            Throw.CheckNotNullOrEmptyArgument( name );
            Power = power;
            Name = name;
            Age = age;
        }

        public Sample( ICKBinaryReader r )
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
            w.Write( Name );
            w.WriteNullableInt16( Age );
        }
    }

    [Test]
    public void value_type_simple_serializable()
    {
        Sample v = new Sample( 31, "Albert", 12 );
        object? backO = TestHelper.SaveAndLoadAny( v );
        backO.ShouldBe( v );
    }

    [Test]
    public void nullable_value_type_as_a_subtype()
    {
        var l = new List<Sample?>() { null, new Sample( 31, "Albert", 12 ) };
        object? backL = TestHelper.SaveAndLoadAny( l );
        backL.ShouldBe( l );
    }

    [Test]
    public void reference_type_simple_serializable()
    {
        var b = new SimpleBase() { Power = 3712 };
        object? backB = TestHelper.SaveAndLoadObject( b );
        backB.ShouldBeEquivalentTo( b );

        var d = new SimpleDerived() { Power = 3712, Name = "Albert" };
        object? backD = TestHelper.SaveAndLoadObject( d );
        backD.ShouldBeEquivalentTo( d );
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
        Util.Invokable( () => TestHelper.SaveAndLoadAny( v ) )
            .ShouldThrow<InvalidOperationException>();

        var o = new MissingCtorReferenceType();
        Util.Invokable( () => TestHelper.SaveAndLoadObject( o ) )
            .ShouldThrow<InvalidOperationException>();
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
    class XB<T> : XA<XB<T>, XC>
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
        backC.ShouldBeEquivalentTo( c );
    }

    /// <summary>
    /// Supporting both interfaces enables simple scenario to use the embedded version
    /// (to be used when not too many instances must be serialized) or use the shared version
    /// (when many instances must be serialized).
    /// </summary>
    [SerializationVersion( 3712 )]
    sealed class CanSupportBothSimpleSerialization : ICKSimpleBinarySerializable, ICKVersionedBinarySerializable
    {
        public bool SimpleWriteCalled;
        public bool SimpleDeserializationConstructorCalled;
        public bool VersionedWriteCalled;
        public bool VersionedDeserializationConstructorCalled;

        public string? Data { get; set; }

        public CanSupportBothSimpleSerialization( string? data )
        {
            Data = data;
        }

        /// <summary>
        /// Simple deserialization constructor.
        /// </summary>
        /// <param name="r">The reader.</param>
        public CanSupportBothSimpleSerialization( ICKBinaryReader r )
            : this( r, r.ReadSmallInt32() )
        {
            SimpleDeserializationConstructorCalled = true;
        }

        /// <summary>
        /// Versioned deserialization constructor.
        /// </summary>
        /// <param name="r">The reader.</param>
        /// <param name="version">The saved version number.</param>
        public CanSupportBothSimpleSerialization( ICKBinaryReader r, int version )
        {
            version.ShouldBe( 3712 );
            // Use the version as usual.
            Data = r.ReadNullableString();
            VersionedDeserializationConstructorCalled = true;
        }

        public void Write( ICKBinaryWriter w )
        {
            // Using a Debug.Assert here avoids the cost of the reflexion.
            Debug.Assert( SerializationVersionAttribute.GetRequiredVersion( GetType() ) == 3712 );
            w.WriteSmallInt32( 3712 );
            WriteData( w );
            SimpleWriteCalled = true;
        }

        public void WriteData( ICKBinaryWriter w )
        {
            // The version is externally managed.
            w.WriteNullableString( Data );

            VersionedWriteCalled = true;
        }
    }

    [Test]
    public void simple_AND_versioned_serialization_easily_coexist_and_the_BinarySerializer_use_the_Versioned_one()
    {
        var c = new CanSupportBothSimpleSerialization( "yep" );
        var backC = TestHelper.SaveAndLoadObject( c );
        backC.Data.ShouldBe( c.Data );

        c.SimpleWriteCalled.ShouldBeFalse();
        c.VersionedWriteCalled.ShouldBeTrue();

        backC.SimpleDeserializationConstructorCalled.ShouldBeFalse();
        backC.VersionedDeserializationConstructorCalled.ShouldBeTrue();
    }

}
