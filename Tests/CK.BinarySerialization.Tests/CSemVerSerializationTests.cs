using NUnit.Framework;
using System;
using CK.Core;
using Shouldly;
using static CK.Testing.MonitorTestHelper;
using CSemVer;
using System.Linq;

namespace CK.BinarySerialization.Tests;

[TestFixture]
public class CSemVerSerializationTests
{
    [TestCase( "0.0.0-0" )]
    [TestCase( "0.0.0" )]
    [TestCase( "0.0.0+m.e.t.a" )]
    [TestCase( "0.0.0+m.e.t.a" )]
    [TestCase( "0.0.0-alpha+meta" )]
    [TestCase( "2147483647.2147483647.2147483647" )]
    public void valid_SVersion( string text )
    {
        var v = SVersion.Parse( text, handleCSVersion: false, checkBuildMetaDataSyntax: false );
        v.ParsedText.ShouldBe( text );

        var backV = TestHelper.SaveAndLoadObject( v );
        backV.ShouldBe( v );
        backV.ParsedText.ShouldBe( text );
    }

    [TestCase( "0.0.a-0" )]
    [TestCase( "0.0" )]
    [TestCase( "" )]
    [TestCase( " not a all " )]
    [TestCase( "0.0.0-+++" )]
    [TestCase( "2147483648.0.0" )]
    public void invalid_SVersion( string text )
    {
        var v = SVersion.TryParse( text, handleCSVersion: false, checkBuildMetaDataSyntax: false );
        v.ParsedText.ShouldBe( text );
        v.IsValid.ShouldBeFalse();

        var backV = TestHelper.SaveAndLoadObject( v );
        backV.ShouldBe( v );
        backV.ParsedText.ShouldBe( text );
        backV.ErrorMessage.ShouldBe( v.ErrorMessage );
    }

    [Test]
    public void zero_and_last_singletons_SVersion()
    {
        var backZ = TestHelper.SaveAndLoadObject( SVersion.ZeroVersion );
        backZ.ShouldBeSameAs( SVersion.ZeroVersion );
        var backL = TestHelper.SaveAndLoadObject( SVersion.LastVersion );
        backL.ShouldBeSameAs( SVersion.LastVersion );
    }

    [TestCase( "0.0.0-alpha" )]
    [TestCase( "1.2.3-rc.89" )]
    [TestCase( "1.2.3-a01-02" )]
    [TestCase( "0.0.0-alpha+meta" )]
    [TestCase( "0.0.0-alpha.5.1+m.e.t.a" )]
    public void valid_CSVersion( string text )
    {
        var v = CSVersion.Parse( text, checkBuildMetaDataSyntax: false );
        v.ParsedText.ShouldBe( text );
        var backV = TestHelper.SaveAndLoadObject( v );
        backV.ShouldBe( v );
        backV.ParsedText.ShouldBe( text );
    }

    [TestCase( "1.2.3", SVersionLock.LockPatch, PackageQuality.Exploratory )]
    [TestCase( "0.0.0", SVersionLock.NoLock, PackageQuality.CI )]
    public void SVersionBound_serialization( string v, SVersionLock l, PackageQuality q )
    {
        var b = new SVersionBound( SVersion.Parse( v ), l, q );
        var backB = TestHelper.SaveAndLoadValue( b );
        backB.ShouldBe( b );
    }

    [TestCase( "1.0.0" )]
    [TestCase( "1.0.0, 2.0.0-rc" )]
    [TestCase( "1.0.0, 1.0.3-alpha, 1.0.2-pre, 1.0.1-rc, 1.0.3-ci" )]
    public void PackageQualityVector_serialization( string versions )
    {
        var q = new PackageQualityVector( versions.Split( ',' ).Select( v => SVersion.Parse( v.Trim() ) ) );
        var backQ = TestHelper.SaveAndLoadValue( q );
        backQ.ShouldBe( q );
    }

    [Test]
    public void empty_PackageQualityVector_serialization()
    {
        var q = new PackageQualityVector();
        var backQ = TestHelper.SaveAndLoadValue( q );
        backQ.ShouldBe( q );
    }

    [TestCase( "" )]
    [TestCase( "Stable" )]
    [TestCase( "Exploratory-Stable" )]
    [TestCase( "Exploratory-Preview" )]
    public void PackageQualityFilter_serialization( string f )
    {
        var q = f.Length > 0 ? new PackageQualityFilter( f ) : new PackageQualityFilter();
        var backQ = TestHelper.SaveAndLoadValue( q );
        backQ.ShouldBe( q );
    }

    [Test]
    public void System_Version_serialization()
    {
        {
            var v = new Version();
            var backV = TestHelper.SaveAndLoadObject( v );
            backV.ShouldBe( v );
        }
        {
            var v = new Version( 1, 2 );
            var backV = TestHelper.SaveAndLoadObject( v );
            backV.ShouldBe( v );
        }
        {
            var v = new Version( 1, 2, 3 );
            var backV = TestHelper.SaveAndLoadObject( v );
            backV.ShouldBe( v );
        }
        {
            var v = new Version( 1, 2, 3, 4 );
            var backV = TestHelper.SaveAndLoadObject( v );
            backV.ShouldBe( v );
        }
    }
}
