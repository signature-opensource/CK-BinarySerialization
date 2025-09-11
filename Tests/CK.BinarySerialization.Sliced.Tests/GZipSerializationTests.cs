using NUnit.Framework;
using Shouldly;
using System.IO;
using System.IO.Compression;
using System;

namespace CK.BinarySerialization.Tests;


[TestFixture]
public class GZipSerializationTests
{
    [TestCase( "FromFactory" )]
    [TestCase( "FromStream" )]
    [TestCase( "FromGZipStream" )]
    public void using_gzip_compression( string rewindableKind )
    {
        var t = Samples.Town.CreateTown( 2 );
        using var memory = new MemoryStream();

        // Using MaxRecursionDepth set to 0 to be sure to have a second pass (thanks to, at least, the Town.CityCar).
        using( var g = new GZipStream( memory, CompressionMode.Compress, leaveOpen: true ) )
        using( var s = BinarySerializer.Create( g, new BinarySerializerContext() { MaxRecursionDepth = 0 } ) )
        {
            s.WriteObject( t );
        }
        memory.Position = 0;

        GZipStream? toDispose = null;

        RewindableStream rewindable;
        if( rewindableKind == "FromFactory" )
        {
            rewindable = RewindableStream.FromFactory( secondPass =>
            {
                if( secondPass ) memory.Position = 0;
                return new GZipStream( memory, CompressionMode.Decompress, leaveOpen: true );
            } );
        }
        else
        {
            var g = toDispose = new GZipStream( memory, CompressionMode.Decompress );
            if( rewindableKind == "FromStream" )
            {
                rewindable = RewindableStream.FromStream( g );
            }
            else if( rewindableKind == "FromGZipStream" )
            {
                rewindable = RewindableStream.FromGZipStream( g );
            }
            else
            {
                throw new ArgumentException();
            }
        }

        BinaryDeserializer.Result<SamplesV2.Town>? result;
        // SamplesV2: class Car (ICKSlicedSerializable) becomes a struct (ICKVersionedBinarySerializable).
        static void SwitchToV2( IMutableTypeReadInfo i )
        {
            if( i.WrittenInfo.TypeNamespace == "CK.BinarySerialization.Tests.Samples" )
            {
                i.SetLocalTypeNamespace( "CK.BinarySerialization.Tests.SamplesV2" );
            }
        }
        var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
        dC.Shared.AddDeserializationHook( SwitchToV2 );
        result = BinaryDeserializer.Deserialize( rewindable, dC, d => d.ReadObject<SamplesV2.Town>() );
        result.IsValid.ShouldBeTrue();
        result.StreamInfo.SecondPass.ShouldBeTrue();
        result.GetResult().Stats.Equals( t.Stats ).ShouldBeTrue();
    }

}
