using CK.Core;
using System;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Primary interface that supports serialization of object graphs.
    /// </summary>
    public interface IBinarySerializer
    {
        /// <summary>
        /// Gets the basic binary writer.
        /// </summary>
        ICKBinaryWriter Writer { get; }

        /// <summary>
        /// Gets the context of this deserializer.
        /// </summary>
        BinarySerializerContext Context { get; }

        /// <summary>
        /// Writes a nullable object or value type.
        /// Note that for value type, using <see cref="WriteNullableValue{T}(T?)"/> avoids boxing.
        /// </summary>
        /// <param name="o">The object or value type to write.</param>
        /// <returns>
        /// True if it has been written, false if the object has already been
        /// written and only a reference has been written.
        /// </returns>
        bool WriteAnyNullable( object? o );

        /// <summary>
        /// Writes a non null object or value type.
        /// Note that for value type, using <see cref="WriteValue{T}(T)"/> avoids boxing.
        /// </summary>
        /// <param name="o">The object or value type to write.</param>
        /// <returns>
        /// True if it has been written, false if the object has already been
        /// written and only a reference has been written.
        /// </returns>
        bool WriteAny( object o );

        /// <summary>
        /// Writes a type information that can be read back by <see cref="IBinaryDeserializer.ReadTypeInfo"/>.
        /// The serializer doesn't need to be resolved (the type itself doesn't need to be serializable).
        /// </summary>
        /// <param name="t">The type to write.</param>
        /// <param name="nullable">When not null, ignores the actual type nullability and considers it either nullable or not nullable.</param>
        /// <returns>
        /// True if it has been written, false if the object has already been
        /// written and only a reference has been written.
        /// </returns>
        bool WriteTypeInfo( Type t, bool? nullable = null );

        /// <summary>
        /// Writes a non null object reference.
        /// </summary>
        /// <typeparam name="T">The object's type.</typeparam>
        /// <param name="o">The object to write.</param>
        /// <returns>
        /// True if the object has been written, false if it has already been
        /// written and only a reference has been written.
        /// </returns>
        bool WriteObject<T>( T o ) where T : class;

        /// <summary>
        /// Writes a nullable object reference.
        /// </summary>
        /// <typeparam name="T">The object's type.</typeparam>
        /// <param name="o">The object to write.</param>
        /// <returns>
        /// True if the object is null or has been written, false 
        /// if it has already been written and only a reference has been written.
        /// </returns>
        bool WriteNullableObject<T>( T? o ) where T : class;

        /// <summary>
        /// Writes a non null value type.
        /// </summary>
        /// <typeparam name="T">The value's type.</typeparam>
        /// <param name="o">The value to write.</param>
        /// <returns>
        /// True if it has been written, false if the object has already been
        /// written and only a reference has been written.
        /// </returns>
        void WriteValue<T>( in T value ) where T : struct;

        /// <summary>
        /// Writes a nullable value type.
        /// </summary>
        /// <typeparam name="T">The value's type.</typeparam>
        /// <param name="o">The value to write.</param>
        /// <returns>
        /// True if it has been written, false if the object has already been
        /// written and only a reference has been written.
        /// </returns>
        void WriteNullableValue<T>( in T? value ) where T : struct;

        /// <summary>
        /// Raised whenever a true <see cref="IDestroyable.IsDestroyed"/> object
        /// is met.
        /// </summary>
        event Action<IDestroyable>? OnDestroyedObject;

        /// <summary>
        /// Gets whether this serializer is currently in debug mode.
        /// Initially defaults to false.
        /// </summary>
        bool IsDebugMode { get; }

        /// <summary>
        /// Activates or deactivates the debug mode. This is cumulative so that scoped activations are handled:
        /// activation/deactivation should be paired and <see cref="IsDebugMode"/> must be used to
        /// know whether debug mode is actually active.
        /// </summary>
        /// <param name="active">Whether the debug mode should be activated, deactivated or (when null) be left as it is.</param>
        void DebugWriteMode( bool? active );

        /// <summary>
        /// Writes a sentinel that must be read back by <see cref="IBinaryDeserializer.DebugCheckSentinel"/>.
        /// If <see cref="IsDebugMode"/> is false, nothing is written.
        /// </summary>
        /// <param name="fileName">Current file name that wrote the data. Used to build the <see cref="InvalidDataException"/> message if sentinel cannot be read back.</param>
        /// <param name="line">Current line number that wrote the data. Used to build the <see cref="InvalidDataException"/> message if sentinel cannot be read back.</param>
        void DebugWriteSentinel( [CallerFilePath] string? fileName = null, [CallerLineNumber] int line = 0 );

    }
}
