using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CK.BinarySerialization;

/// <summary>
/// A rewindable stream is required to support the class to struct mutations.
/// Probability that <see cref="GetSecondStream(out bool)"/> is called is very small
/// but unfortunately not 0.
/// <para>
/// This library offers 3 factories
/// methods: <see cref="FromFactory(Func{bool,Stream})"/>, <see cref="FromStream(Stream)"/> and <see cref="FromGZipStream(GZipStream)"/>.
/// </para>
/// <para>
/// This base class is public to enable alternate implementations.
/// </para>
/// </summary>
public abstract partial class RewindableStream : IBinaryDeserializer.IStreamInfo, IDisposable
{
    /// <inheritdoc />
    public int SerializerVersion { get; protected set; }

    /// <inheritdoc />
    public bool IsLittleEndian { get; protected set; }

    /// <inheritdoc />
    public bool IsCRLF { get; protected set; }

    /// <summary>
    /// Gets whether the header has been correctly read and the version is valid.
    /// </summary>
    public bool IsValid { get; }

    /// <inheritdoc />
    /// <remarks>
    /// This is set to true when <see cref="GetSecondStream(out bool)"/> is called.
    /// </remarks>
    public bool SecondPass { get; private set; }

    /// <inheritdoc />
    public abstract RewindableStreamKind Kind { get; }

    /// <summary>
    /// Gets the initial or second reader.
    /// When <see cref="IsValid"/> is false, this reader has been disposed and must not be used.
    /// </summary>
    public ICKBinaryReader Reader { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="RewindableStream"/> with the initial stream
    /// and reads the header.
    /// <para>
    /// The first byte must be a between 10 and <see cref="BinarySerializer.SerializerVersion"/> otherwise
    /// <see cref="IsValid"/> is false. <see cref="SerializerVersion"/> is always updated so
    /// that alternate way of reading may be tried, BUT <see cref="ICKBinaryReader.ReadSmallInt32(int)"/> has
    /// been called: one or more bytes of the stream have been consumed.
    /// </para>
    /// <para>
    /// If <see cref="IsValid"/> is false but the version is itself a valid one, its because an <see cref="EndOfStreamException"/>
    /// occurred during the read of the header.
    /// </para>
    /// </summary>
    /// <param name="initial">The initial stream.</param>
    protected RewindableStream( Stream initial )
    {
        var r = new CKBinaryReader( initial, Encoding.UTF8, leaveOpen: true );
        Reader = r;
        SerializerVersion = r.ReadSmallInt32();
        if( IsValid = (SerializerVersion >= 10 && SerializerVersion <= BinarySerializer.SerializerVersion) )
        {
            try
            {
                int b = r.ReadByte();
                IsLittleEndian = (b & 1) != 0;
                IsCRLF = (b & 2) != 0;
                Reader = r;
            }
            catch( EndOfStreamException )
            {
                IsValid = false;
            }
        }
        if( !IsValid ) r.Dispose();
    }

    internal void Reset()
    {
        Debug.Assert( !SecondPass );
        var second = GetSecondStream( out var skipHeader );
        var r = new CKBinaryReader( second, Encoding.UTF8, leaveOpen: true );
        if( skipHeader )
        {
            r.ReadSmallInt32();
            r.ReadByte();
        }
        Reader = r;
        SecondPass = true;
    }

    /// <summary>
    /// Called if and only if a second pass is required.
    /// This must return a stream (that may be the initial one) positioned
    /// before the header (it will be automatically skipped) if <paramref name="mustSkipHeader"/> 
    /// is true, or already positioned after the header.
    /// </summary>
    /// <param name="mustSkipHeader">Whether the returned stream starts with the header.</param>
    /// <returns>The second stream to use.</returns>
    protected abstract Stream GetSecondStream( out bool mustSkipHeader );

    /// <summary>
    /// Must dispose what's needed (depends on whether <see cref="SecondPass"/> is true or not).
    /// </summary>
    public abstract void Dispose();

    // Easy one: the stream CanSeek.
    sealed class Seekable : RewindableStream
    {
        readonly Stream _s;
        readonly long _start;

        public Seekable( Stream s )
            : base( s )
        {
            // Initialize even if IsValid is false.
            _s = s;
            _start = s.Position;
        }

        protected override Stream GetSecondStream( out bool shouldSkipHeader )
        {
            shouldSkipHeader = false;
            _s.Position = _start;
            return _s;
        }

        public override RewindableStreamKind Kind => RewindableStreamKind.SeekableStream;

        /// <summary>
        /// Nothing to do since the initial stream must be left opened.
        /// </summary>
        public override void Dispose() { }
    }

    // Wraps the original non seek-able stream and writes every byte
    // read into a temporary file: worst performances but far less pressure on
    // the memory and GC.
    sealed class HookFileStream : Stream
    {
        readonly Stream _input;
        readonly FileStream _output;

        public HookFileStream( Stream input )
        {
            TemporaryFile = new TemporaryFile();
            _input = input;
            _output = new FileStream( TemporaryFile.Path, FileMode.Open, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan );
        }

        public TemporaryFile TemporaryFile { get; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
            _output.Flush();
            _output.Dispose();
        }

        protected override void Dispose( bool disposing )
        {
            base.Dispose( disposing );
            if( disposing ) TemporaryFile.Dispose();
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            int len = _input.Read( buffer, offset, count );
            if( len >= 0 ) _output.Write( buffer, offset, len );
            return len;
        }

        public override long Seek( long offset, SeekOrigin origin ) => throw new NotSupportedException();

        public override void SetLength( long value ) => throw new NotSupportedException();

        public override void Write( byte[] buffer, int offset, int count ) => throw new NotSupportedException();
    }

    // Uses a HookStream and its temporary file to read the second pass content if 
    // needed.
    sealed class RewindableWithFileStream : RewindableStream
    {
        readonly HookFileStream _s;
        Stream? _second;

        public RewindableWithFileStream( HookFileStream s )
            : base( s )
        {
            _s = s;
        }

        public override RewindableStreamKind Kind => RewindableStreamKind.TemporaryFileCopy;

        public override void Dispose()
        {
            if( SecondPass )
            {
                Debug.Assert( _second != null );
                _second.Dispose();
            }
            else
            {
                _s.Flush();
            }
            _s.Dispose();
        }

        protected override Stream GetSecondStream( out bool shouldSkipHeader )
        {
            shouldSkipHeader = true;
            _s.Flush();
            _second = new FileStream( _s.TemporaryFile.Path, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.SequentialScan );
            return _second;
        }
    }

    // When a GZipStream is bound to the stream that CanSeek.
    sealed class GZipOnSeekable : RewindableStream
    {
        readonly GZipStream _s;
        readonly long _start;
        GZipStream? _second;

        public GZipOnSeekable( GZipStream s, long intialBasePosition )
            : base( s )
        {
            // Initialize even if IsValid is false.
            _s = s;
            _start = intialBasePosition;
        }

        public override RewindableStreamKind Kind => _start == 0 ? RewindableStreamKind.GZipStream : RewindableStreamKind.EmbeddedGZipStream;

        protected override Stream GetSecondStream( out bool shouldSkipHeader )
        {
            shouldSkipHeader = true;
            _s.BaseStream.Position = _start;
            return _second = new GZipStream( _s.BaseStream, CompressionMode.Decompress, leaveOpen: true );
        }

        /// <summary>
        /// The initial stream is left opened, only closing the 
        /// second one if it has been created.
        /// </summary>
        public override void Dispose()
        {
            _second?.Dispose();
        }
    }

    /// <summary>
    /// Creates a rewindable stream from an initial stream.
    /// <list type="bullet">
    /// <item>
    /// If <see cref="Stream.CanSeek"/> is true, this is the most efficient: the initial stream will be read twice if a second pass is required.
    /// </item>
    /// <item>
    /// If the stream is a GzipStream that wraps a seekable base stream, AND the base stream is at its start (Position is 0) then a new GZipStream is
    /// created on the repositioned base stream and read if a second pass is required.
    /// </item>
    /// <item>
    /// Otherwise, a temporary file will be created that will be written during the first read, and read again if a second pass is required.
    /// </item>
    /// </list>
    /// </summary>
    /// <param name="s">The initial stream.</param>
    /// <returns>A rewindable stream.</returns>
    public static RewindableStream FromStream( Stream s )
    {
        Throw.CheckNotNullArgument( s );
        Throw.CheckArgument( "Stream must be readable.", s.CanRead );
        if( s.CanSeek )
        {
            return new Seekable( s );
        }
        // Take no risk here: if the inner stream is not at its start, we don't consider
        // that the GzipStream could be rewind.
        if( s is GZipStream g
            && g.BaseStream.CanSeek
            && g.BaseStream.Position == 0 )
        {
            return new GZipOnSeekable( g, 0 );
        }
        // Fallback to safest mode.
        return new RewindableWithFileStream( new HookFileStream( s ) );
    }

    /// <summary>
    /// Creates a <see cref="RewindableStream"/> on a GZipStream that MUST BE at its start,
    /// regardless of the position of the <see cref="GZipStream.BaseStream"/>.
    /// <para>
    /// We have no way to enforce or check this constraint: if any data has been read from the GZipStream 
    /// prior to call this, it will fail.
    /// </para>
    /// </summary>
    /// <param name="s">The GZipStream that must be at its start.</param>
    /// <returns>A rewindable stream.</returns>
    public static RewindableStream FromGZipStream( GZipStream s )
    {
        Throw.CheckNotNullArgument( s );
        Throw.CheckArgument( "Stream must be readable.", s.CanRead );
        if( s.BaseStream.CanSeek )
        {
            return new GZipOnSeekable( s, s.BaseStream.Position );
        }
        else
        {
            return new RewindableWithFileStream( new HookFileStream( s ) );
        }
    }

    sealed class FactoryBased : RewindableStream
    {
        readonly Func<bool, Stream> _opener;
        Stream? _second;

        public FactoryBased( Func<bool, Stream> opener )
            : base( opener( false ) )
        {
            if( !IsValid )
            {
                Reader.BaseStream.Dispose();
            }
            _opener = opener;
        }

        public override RewindableStreamKind Kind => RewindableStreamKind.Factory;

        protected override Stream GetSecondStream( out bool shouldSkipHeader )
        {
            Debug.Assert( !SecondPass );
            Reader.BaseStream.Dispose();
            shouldSkipHeader = true;
            return _second = _opener( true );
        }

        public override void Dispose()
        {
            _second?.Dispose();
        }
    }

    /// <summary>
    /// Creates a rewindable stream from a factory of streams.
    /// <para>
    /// The factory that is called with the <see cref="RewindableStream.SecondPass"/> value must be able to return 
    /// at most two opened identical streams (that will be automatically disposed).
    /// </para>
    /// </summary>
    /// <param name="opener">
    /// The factory for the stream that may be called twice.
    /// The boolean parameter is false for the initial stream and true for the second pass.
    /// </param>
    /// <returns>A rewindable stream.</returns>
    public static RewindableStream FromFactory( Func<bool, Stream> opener )
    {
        Throw.CheckNotNullArgument( opener );
        return new FactoryBased( opener );
    }

}
