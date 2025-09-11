using CK.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static CK.Testing.MonitorTestHelper;

namespace CK.BinarySerialization.Tests;

[TestFixture]
public class TypeSerializationTests
{
    class TypeHolder<T>
    {
        public class NestedGenTypeHolder<T1, T2> : TypeHolder<T2> { }
    }
    class SpecTypeHolder<T> : TypeHolder<T> { }

    class OpenArrayHolder<T>
    {
        public T[] A = Array.Empty<T>();
    }

    interface II { }

    interface II<T> { }

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
        typeof( II ),
        typeof( II<> ),
    };

    [TestCaseSource( nameof( SerializationTypes ) )]
    public void Type_serialization( Type t )
    {
        Type backRW = TestHelper.SaveAndLoad( t, ( type, w ) => w.WriteTypeInfo( type ), r => r.ReadTypeInfo().ResolveLocalType() );
        backRW.ShouldBeSameAs( t );

        Type backO = (Type)TestHelper.SaveAndLoadObject( t );
        backO.ShouldBeSameAs( t );

        Type tRef = t.MakeByRefType();
        Type backRef = (Type)TestHelper.SaveAndLoadObject( tRef );
        backRef.ShouldBeSameAs( tRef );

        Type tPointer = t.MakePointerType();
        Type backPointer = (Type)TestHelper.SaveAndLoadObject( tPointer );
        backPointer.ShouldBeSameAs( tPointer );
    }

    [Test]
    public void OpenArray_type_deserialization_uses_typeof_Array()
    {
        var gT = typeof( OpenArrayHolder<> ).GetField( "A" )!.FieldType;
        Type backRW = TestHelper.SaveAndLoad( gT, ( type, w ) => w.WriteTypeInfo( type ), r => r.ReadTypeInfo().ResolveLocalType() );
        backRW.ShouldBeSameAs( typeof( Array ) );
    }

    class L : List<List<L>>
    {
    }

    [Test]
    public void recursive_generic_types()
    {
        var t = typeof( L );
        Type backT = TestHelper.SaveAndLoad( t, ( type, w ) => w.WriteTypeInfo( type ), r => r.ReadTypeInfo().ResolveLocalType() );
        backT.ShouldBeSameAs( typeof( L ) );
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
                info.IsNullable.ShouldBeFalse();
                info.ResolveLocalType().ShouldBeSameAs( typeof( int ) );
                // int, nullable = true
                info = r.ReadTypeInfo();
                info.IsNullable.ShouldBeTrue();
                info.ResolveLocalType().ShouldBeSameAs( typeof( int? ) );
                // int?, nullable = false
                info = r.ReadTypeInfo();
                info.IsNullable.ShouldBeFalse();
                info.ResolveLocalType().ShouldBeSameAs( typeof( int ) );
                // string
                info = r.ReadTypeInfo();
                info.IsNullable.ShouldBeTrue();
                info.ResolveLocalType().ShouldBeSameAs( typeof( string ) );
                // string, nullable = true (no change since we work in oblivious nullable context)
                info = r.ReadTypeInfo();
                info.IsNullable.ShouldBeTrue();
                info.ResolveLocalType().ShouldBeSameAs( typeof( string ) );
                // string, nullable = false
                info = r.ReadTypeInfo();
                info.IsNullable.ShouldBeFalse();
                info.ResolveLocalType().ShouldBeSameAs( typeof( string ) );
            } );

    }

    [Test]
    public void Delegate_type_serialization()
    {
        var l = new List<int>();
        Delegate d = (Action<int>)l.Add;
        var type = d.GetType();
        Type backT = TestHelper.SaveAndLoad( type, ( type, w ) => w.WriteTypeInfo( type ), r => r.ReadTypeInfo().ResolveLocalType() );
        backT.ShouldBeSameAs( type );
    }

    class A { }

    class B<T1, T2> : A { }

    class CS : B<string, II> { }

    [Test]
    public void generic_base_with_interface()
    {
        Type backT = TestHelper.SaveAndLoad( typeof( CS ), ( type, w ) => w.WriteTypeInfo( type ), r => r.ReadTypeInfo().ResolveLocalType() );
        backT.ShouldBeSameAs( typeof( CS ) );
    }

    public interface ICachedType
    {
        Type Type { get; }

        bool IsNullable { get; }

        ICachedType NonNullable { get; }

        ICachedType Nullable { get; }
    }

    sealed class CachedTypeResolver : ISerializerResolver, IDeserializerResolver
    {
        const string _driverName = "ICachedType-Family";

        readonly ISerializationDriver _serializer = new CachedTypeSerializer();
        readonly IDeserializationDriver _deserializer = new CachedTypeDeserializer();

        public ISerializationDriver? TryFindDriver( BinarySerializerContext context, Type t )
        {
            return typeof( ICachedType ).IsAssignableFrom( t )
                    ? _serializer
                    : null;
        }

        public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
        {
            return info.DriverName == _driverName
                    ? _deserializer
                    : null;
        }

        sealed class CachedTypeSerializer : ReferenceTypeSerializer<ICachedType>
        {
            public override string DriverName => _driverName;

            public override int SerializationVersion => 0;

            protected override void Write( IBinarySerializer s, in ICachedType o )
            {
                s.WriteObject( o.Type );
                s.Writer.Write( o.IsNullable );
            }
        }

        sealed class CachedTypeDeserializer : ReferenceTypeDeserializer<ICachedType>
        {
            protected override void ReadInstance( ref RefReader r )
            {
                var f = r.DangerousDeserializer.Context.Services.GetRequiredService<CachedTypeFactory>();
                var t = r.DangerousDeserializer.ReadObject<Type>();
                r.SetInstance( f.Get( t, r.Reader.ReadBoolean() ) );
            }
        }
    }

    sealed class CachedTypeFactory
    {
        public readonly List<string> Calls = new List<string>();

        sealed class CachedType : ICachedType
        {
            [AllowNull] internal CachedType _nullable;

            public CachedType( Type type )
            {
                Type = type;
            }

            public bool IsNullable => false;

            public ICachedType NonNullable => this;

            public ICachedType Nullable => _nullable;

            public Type Type { get; }
        }

        sealed class NullableCachedType : ICachedType
        {
            readonly CachedType _nonNullable;

            public NullableCachedType( CachedType nonNullable )
            {
                _nonNullable = nonNullable;
            }

            public bool IsNullable => true;

            public ICachedType Nullable => this;

            public ICachedType NonNullable => _nonNullable;

            public Type Type => _nonNullable.Type;
        }

        public ICachedType Get( Type type, bool nullable = false )
        {
            Calls.Add( type.ToCSharpName() );

            var c = new CachedType( type );
            var n = new NullableCachedType( c );
            return nullable ? n : c;
        }
    }

    [Test]
    public void non_serializable_type_wrapper()
    {
        var cachedTypeResolver = new CachedTypeResolver();
        var sC = new BinarySerializerContext( new SharedBinarySerializerContext() );
        sC.Shared.AddResolver( cachedTypeResolver );

        var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext() );
        dC.Shared.AddResolver( cachedTypeResolver );

        var f = new CachedTypeFactory();
        dC.Services.Add( f );

        var same = f.Get( typeof( List<string> ) );
        var someCachedTypes = new List<ICachedType?>()
        {
            same,
            f.Get( typeof(int?), true ),
            null,
            f.Get( typeof(double) ),
            same,
            f.Get( typeof(Dictionary<string,int>), true ),
            same,
            null,
            f.Get( typeof(double?), true)
        };
        f.Calls.Clear();

        var back = TestHelper.SaveAndLoad( someCachedTypes,
                                           ( type, s ) => s.WriteObject( type ),
                                           r => r.ReadObject<List<ICachedType?>>(),
                                           sC,
                                           dC );
        back.Count.ShouldBe( 9 );
        back[0].ShouldNotBeNull().IsNullable.ShouldBeFalse();
        back[0].ShouldBeSameAs( back[4] ).ShouldBeSameAs( back[6] );
        back[1].ShouldNotBeNull().IsNullable.ShouldBeTrue();
        back[2].ShouldBeNull();
        back[3].ShouldNotBeNull().IsNullable.ShouldBeFalse();
        back[5].ShouldNotBeNull().IsNullable.ShouldBeTrue();
        back[8].ShouldNotBeNull().IsNullable.ShouldBeTrue();

        f.Calls.Count.ShouldBe( 5 );

    }

}

