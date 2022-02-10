using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Deserializer for type <typeparamref name="T"/> that handles nullable as well as non nullable written instances.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class Deserializer<T> : INonNullableDeserializationDriver<T>, INullableDeserializationDriver<T> where T : notnull
    {
        /// <summary>
        /// Must read a non null instance from the binary reader.
        /// </summary>
        /// <param name="r">The binary reader.</param>
        /// <param name="readInfo">The read type info.</param>
        /// <returns>The new instance.</returns>
        protected abstract T ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo );

        /// <inheritdoc />
        public Type ResolvedType => typeof( T );

        /// <inheritdoc />
        public INullableDeserializationDriver<T> ToNullable => this;

        /// <inheritdoc />
        public INonNullableDeserializationDriver<T> ToNonNullable => this;

        INullableDeserializationDriver IDeserializationDriver.ToNullable => this;

        INonNullableDeserializationDriver IDeserializationDriver.ToNonNullable => this;

        T INonNullableDeserializationDriver<T>.ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => ReadInstance( r, readInfo );

        T? INullableDeserializationDriver<T>.ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo )
        {
            if( r.Reader.ReadBoolean() )
            {
                return ReadInstance( r, readInfo );
            }
            return default;
        }

        object INonNullableDeserializationDriver.ReadAsObject( IBinaryDeserializer r, TypeReadInfo readInfo ) => ReadInstance( r, readInfo );

        object? INullableDeserializationDriver.ReadAsObject( IBinaryDeserializer r, TypeReadInfo readInfo ) => ReadInstance( r, readInfo );
    }
}
