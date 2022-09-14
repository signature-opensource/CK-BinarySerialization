using System;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Defines a serialization driver cache behavior.
    /// </summary>
    public enum SerializationDriverCacheLevel
    {
        /// <summary>
        /// The serialization driver can be cached in the <see cref="SharedBinarySerializerContext"/>.
        /// This is the default and the most efficient.
        /// </summary>
        SharedContext,

        /// <summary>
        /// The serialization driver can be cached in the <see cref="BinarySerializerContext"/>.
        /// This must typically be used if the serialization driver depends on a stable service
        /// provided by <see cref="BinarySerializerContext.Services"/>.
        /// </summary>
        Context,

        /// <summary>
        /// The serialization driver must not be cached and must always be retrieved from
        /// the <see cref="ISerializerResolver"/> even in the same serialization session: each
        /// instance will have its own serialization driver.
        /// </summary>
        Never
    }

    /// <summary>
    /// Extends <see cref="SerializationDriverCacheLevel"/>.
    /// </summary>
    public static class SerializationDriverCacheLevelExtensions
    {
        /// <summary>
        /// Combines this level with another one: the greatest wins.
        /// </summary>
        /// <param name="this">This level.</param>
        /// <param name="other">The other level.</param>
        /// <returns>The max of the two levels.</returns>
        public static SerializationDriverCacheLevel Combine( this SerializationDriverCacheLevel @this, SerializationDriverCacheLevel other )
        {
            return (SerializationDriverCacheLevel)Math.Max( (int)@this, (int)other );
        }

    }

}
