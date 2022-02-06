using CK.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization
{
    public interface IBinaryDeserializer : IDisposable
    {
        /// <summary>
        /// Gets the basic binary reader.
        /// </summary>
        ICKBinaryReader Reader { get; }

        /// <summary>
        /// Reads a <see cref="TypeReadInfo"/> from a <see cref="IBinarySerializer.WriteTypeInfo(Type)"/>.
        /// </summary>
        /// <returns>The type information.</returns>
        TypeReadInfo ReadTypeInfo();

        /// <summary>
        /// Gets a configurable container of services available for constructor
        /// injection in the deserialized instances.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Reads an object previously written by <see cref="IBinarySerializer.WriteObject(object)"/>.
        /// </summary>
        /// <returns>The object read, possibly in an intermediate state.</returns>
        object ReadObject();

        /// <summary>
        /// Reads an object previously written by <see cref="IBinarySerializer.WriteNullableObject(object?)"/>.
        /// </summary>
        /// <returns>The object read (possibly in an intermediate state) or null.</returns>
        object? ReadNullableObject();

        /// <summary>
        /// Gets whether this deserializer is currently in debug mode.
        /// </summary>
        bool IsDebugMode { get; }

        /// <summary>
        /// Updates the current debug mode that must have been written by <see cref="IBinarySerializer.DebugWriteMode(bool?)"/>.
        /// </summary>
        /// <returns>Whether the debug mode is currently active or not.</returns>
        bool DebugReadMode();

        /// <summary>
        /// Checks the existence of a sentinel written by <see cref="IBinarySerializer.DebugWriteSentinel"/>.
        /// An <see cref="InvalidDataException"/> is thrown if <see cref="IsDebugMode"/> is true and the sentinel cannot be read.
        /// </summary>
        /// <param name="fileName">Current file name used to build the <see cref="InvalidDataException"/> message if sentinel cannot be read back.</param>
        /// <param name="line">Current line number used to build the <see cref="InvalidDataException"/> message if sentinel cannot be read back.</param>
        void DebugCheckSentinel( [CallerFilePath] string? fileName = null, [CallerLineNumber] int line = 0 );

        /// <summary>
        /// When <see cref="IsDebugMode"/> is true, records the <paramref name="ctx"/> in a stack
        /// that will be dumped on error and returns a disposable to pop the stack.
        /// When <see cref="IsDebugMode"/> is false, returns null.
        /// </summary>
        /// <param name="ctx">The stacked message.</param>
        /// <returns>A disposable that will pop the message or null is not in debug mode.</returns>
        IDisposable? OpenDebugPushContext( string ctx );
    }
}
