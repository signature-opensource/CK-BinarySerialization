using CK.Core;
using CSemVer;
using System;
using System.Diagnostics;

namespace CK.BinarySerialization.Serialization
{
    sealed class DSVersion : ReferenceTypeSerializer<SVersion>
    {
        public override string DriverName => "SVersion";

        public override int SerializationVersion => 0;

        protected internal override void Write( IBinarySerializer s, in SVersion o ) => Write( o, s.Writer );

        public static void Write( SVersion o, ICKBinaryWriter w )
        {
            int b;
            // Do not use equality here: we want to track the actual 2 singletons, not 
            // a parsed version of them.
            if( ReferenceEquals( o, SVersion.ZeroVersion ) ) b = 1;
            else
            {
                if( ReferenceEquals( o, SVersion.LastVersion ) ) b = 2;
                else
                {
                    b = o.ParsedText != null ? 4 : 0;
                    if( o.IsValid )
                    {
                        b |= 8;
                        if( o.Prerelease.Length > 0 ) b |= 16;
                        if( o.BuildMetaData.Length > 0 ) b |= 32;
                    }
                }
            }
            w.Write( (byte)b );
            if( (b & 3) == 0 )
            {
                if( o.ParsedText != null )
                {
                    w.Write( o.ParsedText );
                }
                if( o.ErrorMessage != null )
                {
                    w.Write( o.ErrorMessage );
                }
                else
                {
                    w.WriteNonNegativeSmallInt32( o.Major );
                    w.WriteNonNegativeSmallInt32( o.Minor );
                    w.WriteNonNegativeSmallInt32( o.Patch );
                    if( (b & 16) != 0 ) w.Write( o.Prerelease );
                    if( (b & 32) != 0 ) w.Write( o.BuildMetaData );
                }
            }
        }
    }

    sealed class DCSVersion : ReferenceTypeSerializer<CSVersion>
    {
        public override string DriverName => "CSVersion";

        public override int SerializationVersion => 0;

        protected internal override void Write( IBinarySerializer s, in CSVersion o ) => Write( s.Writer, o );

        public static void Write( ICKBinaryWriter w, CSVersion o )
        {
            int b = o.ParsedText != null ? 1 : 0;
            if( o.IsValid )
            {
                b |= 2;
                if( o.IsLongForm ) b |= 4;
                if( o.BuildMetaData.Length > 0 ) b |= 8;
            }
            w.Write( (byte)b );
            if( o.ParsedText != null )
            {
                w.Write( o.ParsedText );
            }
            if( o.IsValid )
            {
                w.Write( o.OrderedVersion );
                if( (b & 8) != 0 ) w.Write( o.BuildMetaData );
            }
            else
            {
                Debug.Assert( o.ErrorMessage != null );
                w.Write( o.ErrorMessage );
            }
        }
    }

    sealed class DSVersionBound : StaticValueTypeSerializer<SVersionBound>
    {
        public override string DriverName => "SVersionBound";

        public override int SerializationVersion => 0;

        public static void Write( IBinarySerializer s, in SVersionBound o )
        {
            s.WriteObject( o.Base );
            s.Writer.WriteEnum( o.Lock );
            s.Writer.WriteEnum( o.MinQuality );
        }
    }

    sealed class DPackageQualityVector : StaticValueTypeSerializer<PackageQualityVector>
    {
        public override string DriverName => "PackageQualityVector";

        public override int SerializationVersion => 0;

        public static void Write( IBinarySerializer s, in PackageQualityVector o )
        {
            s.Writer.Write( (byte)o.ActualCount );
            foreach( var v in o )
            {
                s.WriteObject( v );
            }
        }
    }

    sealed class DPackageQualityFilter : StaticValueTypeSerializer<PackageQualityFilter>
    {
        public override string DriverName => "PackageQualityFilter";

        public override int SerializationVersion => 0;

        public static void Write( IBinarySerializer s, in PackageQualityFilter o )
        {
            s.Writer.Write( (byte)o.Min );
            s.Writer.Write( (byte)o.Max );
        }
    }

    sealed class DVersion : ReferenceTypeSerializer<Version>
    {
        public override string DriverName => "Version";

        public override int SerializationVersion => 0;

        protected internal override void Write( IBinarySerializer s, in Version o )
        {
            s.Writer.WriteNonNegativeSmallInt32( o.Major );
            s.Writer.WriteNonNegativeSmallInt32( o.Minor );
            s.Writer.WriteSmallInt32( o.Build );
            if( o.Build != -1 ) s.Writer.WriteSmallInt32( o.Revision );
        }
    }

}
