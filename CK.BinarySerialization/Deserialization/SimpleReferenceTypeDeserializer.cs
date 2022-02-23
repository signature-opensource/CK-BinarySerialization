using CK.Core;
using System;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Deserializer for reference type <typeparamref name="T"/> from a <see cref="ICKBinaryReader"/>.
    /// The object cannot have references to other objects in the graph.
    /// <para>
    /// This deserializer handles the value to reference type mutation natively.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class SimpleReferenceTypeDeserializer<T> : ReferenceTypeDeserializerBase<T> where T : class
    {
        protected override sealed T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
        {
            var o = ReadInstance( d.Reader, readInfo );
            if( !readInfo.IsValueType ) Unsafe.As<BinaryDeserializerImpl>( d ).Track( o );
            return o;
        }

        /// <summary>
        /// Must read a non null instance from the binary reader.
        /// </summary>
        /// <param name="r">The binary reader.</param>
        /// <param name="readInfo">The read type info.</param>
        /// <returns>The new instance.</returns>
        protected abstract T ReadInstance( ICKBinaryReader r, ITypeReadInfo readInfo );
    }
}
