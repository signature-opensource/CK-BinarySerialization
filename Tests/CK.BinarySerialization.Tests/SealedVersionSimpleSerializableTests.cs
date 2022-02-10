﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using FluentAssertions;

namespace CK.BinarySerialization.Tests
{
    [TestFixture]
    public class SealedVersionSimpleSerializableTests
    {
        [SerializationVersion(1)]
        readonly struct ValueType : ISealedVersionedSimpleSerializable
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

            public void Write( ICKBinaryWriter w )
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
            object? backO = TestHelper.SaveAndLoadObject( v );
            backO.Should().Be( v );
        }

        [SerializationVersion( 0 )]
        sealed class SimpleSealed : ISealedVersionedSimpleSerializable
        {
            public int Power { get; set; }

            public SimpleSealed()
            {
            }

            public SimpleSealed( ICKBinaryReader r, int version )
            {
                Power = r.ReadInt32();
            }

            public void Write( ICKBinaryWriter w )
            {
                w.Write( Power );
            }
        }

        [Test]
        public void reference_type_sealed_serializable()
        {
            var b = new SimpleSealed() { Power = 3712 };
            object? backB = TestHelper.SaveAndLoadObject( b );
            backB.Should().BeEquivalentTo( b );
        }

        struct MissingVersionValueType : ISealedVersionedSimpleSerializable
        {
            public void Write( ICKBinaryWriter w )
            {
            }
        }

        sealed class MissingVersionReferenceType : ISealedVersionedSimpleSerializable
        {
            public void Write( ICKBinaryWriter w )
            {
            }
        }

        [Test]
        public void version_attribute_is_required()
        {
            var v = new MissingVersionValueType();
            FluentActions.Invoking( () => TestHelper.SaveAndLoadObject( v ) )
                .Should().Throw<InvalidOperationException>()
                .WithMessage( "*must be decorated with a [SerializationVersion()]*" );
            
            var o = new MissingVersionReferenceType();
            FluentActions.Invoking( () => TestHelper.SaveAndLoadObject( o ) )
                .Should().Throw<InvalidOperationException>()
                .WithMessage( "*must be decorated with a [SerializationVersion()]*" );
        }

        class MissingSealedReferenceType : ISealedVersionedSimpleSerializable
        {
            public void Write( ICKBinaryWriter w )
            {
            }
        }

        [Test]
        public void reference_type_MUST_be_sealed()
        {
            var o = new MissingSealedReferenceType();
            FluentActions.Invoking( () => TestHelper.SaveAndLoadObject( o ) )
                .Should().Throw<InvalidOperationException>()
                .WithMessage( "*It must be a sealed class or a value type*" );
        }


        [SerializationVersion(0)]
        struct MissingCtorValueType : ISealedVersionedSimpleSerializable
        {
            public void Write( ICKBinaryWriter w )
            {
            }
        }

        [SerializationVersion( 0 )]
        sealed class MissingCtorReferenceType : ISealedVersionedSimpleSerializable
        {
            public void Write( ICKBinaryWriter w )
            {
            }
        }

        [Test]
        public void constructor_with_IBinaryReader_and_int_version_is_required()
        {
            var v = new MissingCtorValueType();
            FluentActions.Invoking( () => TestHelper.SaveAndLoadObject( v ) )
                .Should().Throw<InvalidOperationException>()
                .WithMessage( "*requires a constructor with ( ICKBinaryReader, Int32 ) parameters*" );

            var o = new MissingCtorReferenceType();
            FluentActions.Invoking( () => TestHelper.SaveAndLoadObject( o ) )
                .Should().Throw<InvalidOperationException>()
                .WithMessage( "*requires a constructor with ( ICKBinaryReader, Int32 ) parameters*" );
        }


    }
}
