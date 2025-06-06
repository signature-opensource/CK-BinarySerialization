using CK.Core;
using CSemVer;
using System;

namespace CK.BinarySerialization.Deserialization;

sealed class DSVersion : SimpleReferenceTypeDeserializer<SVersion>
{
    public DSVersion() : base( true ) { }

    protected override SVersion ReadInstance( ICKBinaryReader r, ITypeReadInfo readInfo )
    {
        int b = r.ReadByte();
        if( b == 1 ) return SVersion.ZeroVersion;
        if( b == 2 ) return SVersion.LastVersion;
        string? parsed = (b & 4) != 0 ? r.ReadString() : null;
        if( (b & 8) != 0 )
        {
            return SVersion.Create( r.ReadNonNegativeSmallInt32(),
                                    r.ReadNonNegativeSmallInt32(),
                                    r.ReadNonNegativeSmallInt32(),
                                    (b & 16) != 0 ? r.ReadString() : null,
                                    (b & 32) != 0 ? r.ReadString() : null,
                                    handleCSVersion: false,
                                    checkBuildMetaDataSyntax: false,
                                    parsed );
        }
        else
        {
            return new SVersion( r.ReadString(), parsed );
        }
    }
}

sealed class DCSVersion : SimpleReferenceTypeDeserializer<CSVersion>
{
    public DCSVersion() : base( true ) { }

    protected override CSVersion ReadInstance( ICKBinaryReader r, ITypeReadInfo readInfo )
    {
        int b = r.ReadByte();
        string? parsed = (b & 1) != 0 ? r.ReadString() : null;
        if( (b & 2) != 0 )
        {
            return CSVersion.Create( r.ReadInt64(), (b & 4) != 0, (b & 8) != 0 ? r.ReadString() : null, parsed );
        }
        return new CSVersion( r.ReadString(), parsed );
    }
}

sealed class DSVersionBound : ValueTypeDeserializer<SVersionBound>
{
    protected override SVersionBound ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
    {
        if( readInfo.Version == 0 )
        {
            return new SVersionBound( d.ReadObject<SVersion>(), (SVersionLock)d.Reader.ReadByte(), ReadV0Quality( d ) );
        }
        return new SVersionBound( d.ReadObject<SVersion>(), (SVersionLock)d.Reader.ReadByte(), (PackageQuality)d.Reader.ReadByte() );
    }

    // CSemVer v11.x: PackageQuality had None = 0, CI = 1, Exploratory = 3, etc.
    internal static PackageQuality ReadV0Quality( IBinaryDeserializer d )
    {
        return d.Reader.ReadByte() switch
        {
            1 => PackageQuality.CI,
            3 => PackageQuality.Exploratory,
            7 => PackageQuality.Preview,
            15 => PackageQuality.ReleaseCandidate,
            31 => PackageQuality.Stable,
            _ => PackageQuality.CI
        };
    }
}

sealed class DPackageQualityVector : ValueTypeDeserializer<PackageQualityVector>
{
    protected override PackageQualityVector ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
    {
        int c = d.Reader.ReadByte();
        var q = new PackageQualityVector();
        while( --c >= 0 ) q = q.WithVersion( d.ReadObject<SVersion>() );
        return q;
    }
}

sealed class DPackageQualityFilter : ValueTypeDeserializer<PackageQualityFilter>
{
    protected override PackageQualityFilter ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
    {
        if( readInfo.Version == 0 )
        {
            return new PackageQualityFilter( DSVersionBound.ReadV0Quality( d ), DSVersionBound.ReadV0Quality( d ) );
        }
        return new PackageQualityFilter( (PackageQuality)d.Reader.ReadByte(), (PackageQuality)d.Reader.ReadByte() );
    }
}

sealed class DVersion : SimpleReferenceTypeDeserializer<Version>
{
    protected override Version ReadInstance( ICKBinaryReader r, ITypeReadInfo readInfo )
    {
        int major = r.ReadNonNegativeSmallInt32();
        int minor = r.ReadNonNegativeSmallInt32();
        int build = r.ReadSmallInt32();
        if( build != -1 )
        {
            int revision = r.ReadSmallInt32();
            if( revision != -1 )
            {
                return new Version( major, minor, build, revision );
            }
            return new Version( major, minor, build );
        }
        return new Version( major, minor );
    }
}
