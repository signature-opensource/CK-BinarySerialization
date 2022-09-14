using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Deserialization driver that knows how to instantiate an instance of a <see cref="ResolvedType"/> 
    /// and initializes it from a <see cref="IBinaryDeserializer"/> or handles null thanks to its 2 drivers.
    /// </summary>
    public interface IDeserializationDriver
    {
        /// <summary>
        /// Gets the type that this drivers is able to resolve.
        /// </summary>
        Type ResolvedType { get; }

        /// <summary>
        /// Gets a <see cref="TypedReader{T}"/> for this type and nullability.
        /// </summary>
        Delegate TypedReader { get; }

        /// <summary>
        /// Gets whether this driver can be cached and reused.
        /// <para>
        /// Note that this caching is not done at the <see cref="BinaryDeserializerContext"/> level nor at
        /// the <see cref="SharedBinaryDeserializerContext"/>: this can only be done by resolvers and, ultimately,
        /// the deserializer is cached in the <see cref="ITypeReadInfo"/> per deserialization session instance.
        /// </para>
        /// <para>
        /// A driver that relies on other drivers can only be cached and reused if 
        /// all the drivers it relies on are cached (this is a necessary and but not a sufficient condition
        /// to actually be cached).
        /// </para>
        /// <para>
        /// Resolvers should use <see cref="SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers"/> concurrent
        /// dictionary to cache drivers that can be cached and when the deserializer depends only on the local type to
        /// deserialize. For more complex cache condition (typically when the deserialization uses the <see cref="BinaryDeserializerContext.Services"/>),
        /// it is up to the resolver to handle its caching if it can.
        /// </para>
        /// </summary>
        bool IsCacheable { get; }

        /// <summary>
        /// Gets the nullable driver.
        /// </summary>
        IDeserializationDriver ToNullable { get; }
        
        /// <summary>
        /// Gets the non nullable driver.
        /// </summary>
        IDeserializationDriver ToNonNullable { get; }
    }
}
