using NUnit.Framework;
using CK.Core;
using System.Collections;
using System.Collections.Generic;
using static CK.Testing.MonitorTestHelper;
using FluentAssertions;
using System.IO;
using System.IO.Compression;
using System;

namespace CK.BinarySerialization.Tests
{
    [TestFixture]
    public class GZipSerializationTests
    {
        [TestCase( "FromFactory" )]
        [TestCase( "FromGZipedFile" )]
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

            // SamplesV2: class Car (ICKSlicedSerializable) becomes a struct (ICKVersionedBinarySerializable).
            static void SwitchToV2( IMutableTypeReadInfo i )
            {
                if( i.ReadInfo.TypeNamespace == "CK.BinarySerialization.Tests.Samples" )
                {
                    i.SetLocalTypeNamespace( "CK.BinarySerialization.Tests.SamplesV2" );
                }
            }
            var dC = new BinaryDeserializerContext( new SharedBinaryDeserializerContext(), null );
            dC.Shared.AddDeserializationHook( SwitchToV2 );

            BinaryDeserializer.Result<SamplesV2.Town>? result;
            if( rewindableKind == "FromFactory" )
            {
                result = BinaryDeserializer.Deserialize( secondPass =>
                                                         {
                                                             if( secondPass ) memory.Position = 0;
                                                             return new GZipStream( memory, CompressionMode.Decompress, leaveOpen: true );
                                                         },
                                                         dC,
                                                         d => d.ReadObject<SamplesV2.Town>() );
            }
            else
            {
                throw new NotImplementedException();
            }
            result.IsValid.Should().BeTrue();
            result.StreamInfo.SecondPass.Should().BeTrue();
            result.GetResult().Stats.Should().Be( t.Stats );
        }
    }
}
