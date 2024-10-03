using NUnit.Framework;
using System;
using System.Collections.Generic;
using CK.Core;
using FluentAssertions;
using static CK.Testing.MonitorTestHelper;

namespace CK.BinarySerialization.Tests;

[TestFixture]
public class BasicSerializationTests
{
    static object?[] Objects = new object?[]
    {
        null,
        3712,
        (uint)3712,
        3712.42,
        3712.42f,
        (byte)42,
        (sbyte)-5,
        (short)-1504,
        (ushort)987,
        (long)897121442586,
        (ulong)8971214425444444486,
        true,
        'C',
        "DateTime.UtcNow",
        "DateTime.Now",
    };

    [TestCaseSource( nameof( Objects ) )]
    public void simple_serialization( object? o )
    {
        if( o is string s )
        {
            if( s == "DateTime.UtcNow" ) o = DateTime.UtcNow;
            if( s == "DateTime.Now" ) o = DateTime.Now;
        }
        object? backO = TestHelper.SaveAndLoadAny( o );
        backO.Should().Be( o );
    }

    static int a = 3712;
    static uint b = 3712;
    static short c = 3712;
    static ushort d = 3712;
    static byte e = 42;
    static sbyte f = 42;
    static long g = 42;
    static ulong h = 42;
    static char i = 'c';
    static DateTime j = DateTime.UtcNow;
    static DateTimeOffset k = DateTimeOffset.UtcNow;

    [Test]
    public void value_types_serialization()
    {
        Check( a );
        Check( b );
        Check( c );
        Check( d );
        Check( e );
        Check( f );
        Check( g );
        Check( h );
        Check( i );
        Check( j );
        Check( k );

        static void Check<T>( in T x ) where T : struct
        {
            T back = TestHelper.SaveAndLoadValue( x );
            back.Should().Be( x );
        }
    }

    static int? aN = 3712;
    static uint? bN = 3712;
    static short? cN = 3712;
    static ushort? dN = 3712;
    static byte? eN = 42;
    static sbyte? fN = 42;
    static long? gN = 42;
    static ulong? hN = 42;
    static char? iN = 'c';
    static DateTime? jN = DateTime.UtcNow;
    static DateTimeOffset? kN = DateTimeOffset.UtcNow;
    static GrantLevel? lN = GrantLevel.Editor;


    [Test]
    public void nullable_value_types_serialization()
    {
        Check( aN ); Check( default( int? ) );
        Check( bN ); Check( default( uint? ) );
        Check( cN ); Check( default( short? ) );
        Check( dN ); Check( default( ushort? ) );
        Check( eN ); Check( default( byte? ) );
        Check( fN ); Check( default( sbyte? ) );
        Check( gN ); Check( default( long? ) );
        Check( hN ); Check( default( ulong? ) );
        Check( iN ); Check( default( char? ) );
        Check( jN ); Check( default( DateTime? ) );
        Check( kN ); Check( default( DateTimeOffset? ) );
        Check( lN ); Check( default( GrantLevel? ) );

        static void Check<T>( in T? x ) where T : struct
        {
            T? back = TestHelper.SaveAndLoadNullableValue( x );
            back.Should().Be( x );
        }
    }

    [Test]
    public void enum_serialization()
    {
        var o = GrantLevel.Administrator;
        GrantLevel? oN = GrantLevel.User;
        GrantLevel? oNN = default;
        {
            GrantLevel b = TestHelper.SaveAndLoadValue( o );
            b.Should().Be( o );
        }
        {
            GrantLevel? b = TestHelper.SaveAndLoadNullableValue( oN );
            b.Should().Be( oN );
        }
        {
            GrantLevel? b = TestHelper.SaveAndLoadNullableValue( oNN );
            b.Should().Be( oNN );
        }
        {
            object? b = TestHelper.SaveAndLoadAny( oN );
            b.Should().Be( oN );
        }
        {
            object? b = TestHelper.SaveAndLoadAny( oNN );
            b.Should().Be( oNN );
        }
    }

}
