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
            public T[] A;
        }

        // For an unknown reason, TestCase sucks here. Using TestCaseSource instead.
        static Type[] SerializationTypes = new[]
        {
            typeof( int ),
            typeof( TypeSerializationTests ),
            typeof( List<TypeSerializationTests> ),
            typeof( TypeHolder<Func<List<int>, double, Action<object>>> ),
            typeof( SpecTypeHolder<Func<List<int>, double, Action<object>>> ),
            typeof( SpecTypeHolder<int> ),
            typeof( TypeHolder<byte>.NestedGenTypeHolder<short, int> ),
            typeof( TypeHolder<>.NestedGenTypeHolder<,> ),
            typeof( int[] ),
            typeof( int[,] ),
            typeof( List<(int, string)[]> ),
            typeof( List<(int, string)[,,,]> ),
            typeof( int[][] ),
            typeof( List<(int, string)[,,,]>[,,] ),
            typeof( (int, string) ),
            typeof( (int, string)[] ),
            typeof( Dictionary<(short,float),List<(int, string)[]>> ),
            typeof( OpenArrayHolder<> ),
        };

        [TestCaseSource(nameof(SerializationTypes))]
        public void Type_serialization( Type t )
        {
            Type backRW = TestHelper.SaveAndLoadObject( t, ( type, w ) => w.WriteTypeInfo( type ), r => r.ReadTypeInfo().ResolveLocalType() );
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
            Type backRW = TestHelper.SaveAndLoadObject( gT, ( type, w ) => w.WriteTypeInfo( type ), r => r.ReadTypeInfo().ResolveLocalType() );
            backRW.Should().BeSameAs( typeof(Array) );
        }

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

    }
}

