using CK.Core;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Main interface that handles object graph deserialization.
    /// </summary>
    public interface IBinaryDeserializer
    {
        /// <summary>
        /// Exposes informations related to the way the stream to deserialize has been 
        /// saved.
        /// </summary>
        public interface IStreamInfo
        {
            /// <summary>
            /// Gets the version of the serializer used to serialize this data.
            /// Current version is <see cref="BinarySerializer.SerializerVersion"/>.
            /// </summary>
            int SerializerVersion { get; }

            /// <summary>
            /// Gets whether the stream has been serialized on a <see cref="BitConverter.IsLittleEndian"/>
            /// platform.
            /// </summary>
            bool IsLittleEndian { get; }

            /// <summary>
            /// Gets whether the stream has been serialized with a '\r\n' end-of-line default.
            /// </summary>
            bool IsCRLF { get; }

            /// <summary>
            /// Gets the kind of rewindable stream being used.
            /// </summary>
            RewindableStreamKind Kind { get; }

            /// <summary>
            /// Gets whether we are currently in the second pass that handles a class to struct mutation
            /// (happens if and only if one instance of the written class has been used to cut the recursion).
            /// </summary>
            bool SecondPass { get; }
        }

        /// <summary>
        /// Gets the stream information.
        /// </summary>
        IStreamInfo StreamInfo { get; }

        /// <summary>
        /// Gets the context of this deserializer.
        /// </summary>
        BinaryDeserializerContext Context { get; }

        /// <summary>
        /// Gets the basic binary reader.
        /// </summary>
        ICKBinaryReader Reader { get; }

        /// <summary>
        /// Reads a <see cref="ITypeReadInfo"/> written by <see cref="IBinarySerializer.WriteTypeInfo(Type, bool?)"/>.
        /// </summary>
        /// <returns>The type information.</returns>
        ITypeReadInfo ReadTypeInfo();

        /// <summary>
        /// Reads a non null object or value type. This deserialize the written type since no
        /// type information is provided. Use <see cref="ReadAnyNullable(){T}()"/> to select the deserialized
        /// type (and its deserialization driver).
        /// </summary>
        /// <returns>The object or value type (possibly in an intermediate state) or null.</returns>
        object? ReadAnyNullable();

        /// <summary>
        /// Reads a non null object or value type. This deserialize the written type since no
        /// type information is provided. Use <see cref="ReadAny{T}()"/> to select the deserialized type (and its
        /// deserialization driver).
        /// </summary>
        /// <returns>The object read, possibly in an intermediate state.</returns>
        object ReadAny();

        /// <summary>
        /// Reads a nullable object or a value type, providing the deserialized target type.
        /// </summary>
        /// <returns>The object or value type (possibly in an intermediate state) or null.</returns>
        T? ReadAnyNullable<T>();

        /// <summary>
        /// Reads a non null object or value type, providing the deserialized target type.
        /// </summary>
        /// <returns>The object read, possibly in an intermediate state.</returns>
        T ReadAny<T>();

        /// <summary>
        /// Reads a non null object reference written by <see cref="IBinarySerializer.WriteObject{T}(T)"/> 
        /// or <see cref="IBinarySerializer.WriteAny(object)"/>.
        /// </summary>
        /// <typeparam name="T">The object's expected type.</typeparam>
        /// <returns>The object read, possibly in an intermediate state.</returns>
        T ReadObject<T>() where T : class;

        /// <summary>
        /// Reads a nullable object reference.
        /// </summary>
        /// <typeparam name="T">The object's expected type.</typeparam>
        /// <returns>The object or value type (possibly in an intermediate state) or null.</returns>
        T? ReadNullableObject<T>() where T : class;

        /// <summary>
        /// Reads a non null value type written by <see cref="IBinarySerializer.WriteValue{T}(in T)"/>.
        /// </summary>
        /// <typeparam name="T">The value's expected type.</typeparam>
        /// <returns>The value read. If this value has references to objects, these objects may be in an intermediate state.</returns>
        T ReadValue<T>() where T : struct;

        /// <summary>
        /// Reads a nullable value type.
        /// </summary>
        /// <typeparam name="T">The value's expected type.</typeparam>
        /// <returns>The value read or null. If this value has references to objects, these objects may be in an intermediate state.</returns>
        T? ReadNullableValue<T>() where T : struct;

        /// <summary>
        /// Gets a simple deferred container of <see cref="Action"/> that 
        /// enables deserialization constructors to execute any code once the 
        /// whole object graph is deserialized and all the objects are properly restored.
        /// </summary>
        Deserialization.PostActions PostActions { get; }

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
        /// <returns>A disposable that will pop the message or null if not in debug mode.</returns>
        IDisposable? OpenDebugPushContext( string ctx );
    }
}
