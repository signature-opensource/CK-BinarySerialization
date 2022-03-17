using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// A rewindable stream is required to support the class to struct mutations.
    /// Probability that <see cref="GetSecondStream(out bool)"/> is called is very small
    /// but unfortunately not 0.
    /// <para>
    /// By default 3 kind of implementations are available through the two factory 
    /// methods: <see cref="FromFactory(Func{Stream})"/> and <see cref="FromStream(Stream)"/>.
    /// </para>
    /// <para>
    /// This base class is public to enable alternate implementations. For instance GZipStream or other deflate stream
    /// bound to an inner seek-able stream may be cleverly handled by recreating
    /// a deflater onto the repositioned inner stream. The initial stream may even be left bound to its inner stream
    /// so that it will be able to continue the reading after the second pass. But since this requires the 
    /// compression level to be known, this cannot be implemented here and should be done outside with more knowledge of 
    /// the context.
    /// </para>
    /// </summary>
    public abstract class RewindableStream : IBinaryDeserializer.IStreamInfo, IDisposable
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
        /// <see cref="IsValid"/> is false but <paramref name="SerializerVersion"/> is updated so
        /// that alternate way of reading may be tried, BUT <see cref="ICKBinaryReader.ReadSmallInt32(int)"/> has
        /// been called: one or more bytes of the stream have been consumed.
        /// </para>
        /// <para>
        /// If <see cref="IsValid"/> is false but the version is, its because an <see cref="EndOfStreamException"/>
        /// occurred during the read of the header.
        /// </para>
        /// </summary>
        /// <param name="initial">The initial stream.</param>
        protected RewindableStream( Stream initial )
        {
            var r = new CKBinaryReader( initial, Encoding.UTF8, leaveOpen : true );
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
            var r = new CKBinaryReader( second, Encoding.UTF8, leaveOpen : true );
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
        sealed class ResetableWithFileStream : RewindableStream
        {
            readonly HookFileStream _s;
            Stream? _second;

            public ResetableWithFileStream( HookFileStream s )
                : base( s )
            {
                _s = s;
            }

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

        sealed class FactoryBased : RewindableStream
        {
            Func<Stream> _opener;
            Stream? _second;

            public FactoryBased( Func<Stream> opener )
                : base( opener() )
            {
                if( !IsValid )
                {
                    Reader.BaseStream.Dispose();
                }
                _opener = opener;
            }

            protected override Stream GetSecondStream( out bool shouldSkipHeader )
            {
                Debug.Assert( !SecondPass );
                Reader.BaseStream.Dispose();
                shouldSkipHeader = true;
                return _second = _opener();
            }

            public override void Dispose()
            {
                _second?.Dispose();
            }
        }

        /// <summary>
        /// Creates a rewindable stream from an initial stream.
        /// <list type="bullet">
        /// <item>If <see cref="Stream.CanSeek"/> is true, this is the most efficient: the initial stream will be read twice if a second pass is required.</item>
        /// <item>Otherwise, a temporary file will be created that will be written during the first read, and read again if a second pass is required.</item>
        /// </list>
        /// </summary>
        /// <param name="s">The initial stream.</param>
        /// <returns>A rewindable stream.</returns>
        public static RewindableStream FromStream( Stream s )
        {
            if( s == null ) throw new ArgumentNullException( nameof( s ) );
            if( s.CanSeek )
            {
                return new Seekable( s );
            }
            return new ResetableWithFileStream( new HookFileStream( s ) );
        }

        /// <summary>
        /// Creates a rewindable stream from a factory of streams.
        /// <para>
        /// The factory must be able to return at most two opened identical streams (that will be 
        /// disposed).
        /// </para>
        /// </summary>
        /// <param name="opener">The factory for the stream that may be called twice.</param>
        /// <returns>A rewindable stream.</returns>
        public static RewindableStream FromFactory( Func<Stream> opener )
        {
            if( opener == null ) throw new ArgumentNullException( nameof( opener ) );
            return new FactoryBased( opener );
        }

    }
}
