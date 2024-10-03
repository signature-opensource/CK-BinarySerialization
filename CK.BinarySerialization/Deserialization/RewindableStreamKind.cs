using System;
using System.IO;
using System.IO.Compression;

namespace CK.BinarySerialization;

/// <summary>
/// Defines the type of the RewindableStream used.
/// </summary>
public enum RewindableStreamKind
{
    /// <summary>
    /// A specialized RewindableStream implemented outside the library.
    /// </summary>
    Other,

    /// <summary>
    /// See <see cref="RewindableStream.FromFactory(Func{bool, Stream})"/>.
    /// </summary>
    Factory,

    /// <summary>
    /// The read stream is seekable. This is the simplest and most efficient possibility.
    /// Created by <see cref="RewindableStream.FromStream(Stream)"/>.
    /// </summary>
    SeekableStream,

    /// <summary>
    /// A <see cref="System.IO.Compression.GZipStream"/> at the start of its seekable base stream.
    /// Created by <see cref="RewindableStream.FromStream(Stream)"/> or <see cref="RewindableStream.FromGZipStream(GZipStream)"/>.
    /// </summary>
    GZipStream,

    /// <summary>
    /// A <see cref="GZipStream"/> that may not be at the start of its base stream.
    /// Created by <see cref="RewindableStream.FromGZipStream(GZipStream)"/>.
    /// </summary>
    EmbeddedGZipStream,

    /// <summary>
    /// A temporary file is duplicated during the first read. This is a totally safe fallback
    /// but is not good in terms of performance.
    /// Created by <see cref="RewindableStream.FromStream(Stream)"/> or <see cref="RewindableStream.FromGZipStream(GZipStream)"/>
    /// to handle non seekable streams.
    /// </summary>
    TemporaryFileCopy,
}
