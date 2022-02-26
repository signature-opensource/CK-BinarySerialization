using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using static CK.Testing.MonitorTestHelper;

namespace CK.BinarySerialization.Tests
{
    [TestFixture]
    public class TypeSerializationTests
    {
        class TypeHolder<T> 
        {
            public class NestedGenTypeHolder<T1,T2> : TypeHolder<T2> { }
        }
        class SpecTypeHolder<T> : TypeHolder<T> { }

        class OpenArrayHolder<T>
        {
            public T[] A = Array.Empty<T>();
        }

        // For an unknown reason, TestCase sucks here. Using TestCaseSource instead.
        static Type[] SerializationTypes = new[]
        {
            typeof( int ),
            typeof( int? ),
            typeof( TypeSerializationTests ),
            typeof( List<TypeSerializationTests> ),
            typeof( TypeHolder<Func<List<int>, double, Action<object>>> ),
            typeof( SpecTypeHolder<Func<List<int>, double, Action<object>>> ),
            typeof( SpecTypeHolder<int> ),
            typeof( TypeHolder<byte>.NestedGenTypeHolder<short, int> ),
            typeof( TypeHolder<>.NestedGenTypeHolder<,> ),
            typeof( int[] ),
            typeof( int[,] ),
            typeof( string[] ),
            typeof( string[,] ),
            typeof( SimpleBase[] ),
            typeof( List<(int, string)[]> ),
            typeof( List<(int, string)[,,,]> ),
            typeof( int[][] ),
            typeof( List<(int, string)[,,,]>[,,] ),
            typeof( (int, string) ),
            typeof( (int, string)[] ),
            typeof( Dictionary<(short,float),List<(int, string)[]>> ),
            typeof( OpenArrayHolder<> ),
            typeof( GrantLevel ),
            typeof( GrantLevel? ),
        };

        [TestCaseSource(nameof(SerializationTypes))]
        public void Type_serialization( Type t )
        {
            Type backRW = TestHelper.SaveAndLoad( t, ( type, w ) => w.WriteTypeInfo( type ), r => r.ReadTypeInfo().ResolveLocalType() );
            backRW.Should().BeSameAs( t );

            Type backO = (Type)TestHelper.SaveAndLoadObject( t );
            backO.Should().BeSameAs( t );

            Type tRef = t.MakeByRefType();
            Type backRef = (Type)TestHelper.SaveAndLoadObject( tRef );
            backRef.Should().BeSameAs( tRef );

            Type tPointer = t.MakePointerType();
            Type backPointer = (Type)TestHelper.SaveAndLoadObject( tPointer );
            backPointer.Should().BeSameAs( tPointer );
        }

        [Test]
        public void OpenArray_type_deserialization_uses_typeof_Array()
        {
            var gT = typeof( OpenArrayHolder<> ).GetField( "A" )!.FieldType;
            Type backRW = TestHelper.SaveAndLoad( gT, ( type, w ) => w.WriteTypeInfo( type ), r => r.ReadTypeInfo().ResolveLocalType() );
            backRW.Should().BeSameAs( typeof(Array) );
        }

        class L : List<List<L>>
        {
        }

        [Test]
        public void recursive_generic_types()
        {
            var t = typeof( L );
            Type backT = TestHelper.SaveAndLoad( t, ( type, w ) => w.WriteTypeInfo( type ), r => r.ReadTypeInfo().ResolveLocalType() );
            backT.Should().BeSameAs( typeof( L ) );
        }


        [Test]
        public void nullability_type_writing()
        {
            TestHelper.SaveAndLoad(
                w =>
                {
                    w.WriteTypeInfo( typeof( int ) );
                    w.WriteTypeInfo( typeof( int ), true );
                    w.WriteTypeInfo( typeof( int? ), false );

                    w.WriteTypeInfo( typeof( string ) );
                    w.WriteTypeInfo( typeof( string ), true );
                    w.WriteTypeInfo( typeof( string ), false );
                },
                r =>
                {
                    ITypeReadInfo info;
                    // int
                    info = r.ReadTypeInfo();
                    info.IsNullable.Should().BeFalse();
                    info.ResolveLocalType().Should().BeSameAs( typeof(int) );
                    // int, nullable = true
                    info = r.ReadTypeInfo();
                    info.IsNullable.Should().BeTrue();
                    info.ResolveLocalType().Should().BeSameAs( typeof( int? ) );
                    // int?, nullable = false
                    info = r.ReadTypeInfo();
                    info.IsNullable.Should().BeFalse();
                    info.ResolveLocalType().Should().BeSameAs( typeof( int ) );
                    // string
                    info = r.ReadTypeInfo();
                    info.IsNullable.Should().BeTrue();
                    info.ResolveLocalType().Should().BeSameAs( typeof( string ) );
                    // string, nullable = true (no change since we work in oblivious nullable context)
                    info = r.ReadTypeInfo();
                    info.IsNullable.Should().BeTrue();
                    info.ResolveLocalType().Should().BeSameAs( typeof( string ) );
                    // string, nullable = false
                    info = r.ReadTypeInfo();
                    info.IsNullable.Should().BeFalse();
                    info.ResolveLocalType().Should().BeSameAs( typeof( string ) );
                } );

        }


    }
}

