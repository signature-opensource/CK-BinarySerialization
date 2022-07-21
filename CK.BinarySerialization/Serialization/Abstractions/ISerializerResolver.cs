using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Finds a serializer for a type.
    /// <para>
    /// Not all the resolvers are the same: <see cref="BasicTypeSerializerRegistry.Instance"/> relies on 
    /// immutable mappings and is exposed as a singleton, <see cref="SimpleBinarySerializableFactory"/> is 
    /// a pure factory (it doesn't cache its result).
    /// </para>
    /// </summary>
    public interface ISerializerResolver
    {
        /// <summary>
        /// Finds a serialization driver for a type.
        /// The driver's nullability is driven by the type. 
        /// Reference type defaults to nullable (rule of the oblivious nullable context).
        /// </summary>
        /// <param name="t">The type for which a driver must be found.</param>
        /// <returns>The driver or null.</returns>
        ISerializationDriver? TryFindDriver( Type t );
    }

    /// <summary>
    /// Extends <see cref="ISerializerResolver"/>.
    /// </summary>
    public static class SerializerResolverExtensions
    {
        /// <summary>
        /// Finds an untyped serialization nullable or non nullable driver for a type.
        /// </summary>
        /// <param name="r">This resolver.</param>
        /// <param name="t">The type for which a driver must be resolved.</param>
        /// <param name="nullable">
        /// When not null, requests the nullable or not nullable driver regardless of the nullability of type itself.
        /// </param>
        /// <returns>The driver or null.</returns>
        public static ISerializationDriver? TryFindDriver( this ISerializerResolver r, Type t, bool? nullable )
        {
            var d = r.TryFindDriver( t );
            return d == null || !nullable.HasValue
                    ? d 
                    : nullable.Value
                        ? d.ToNullable
                        : d.ToNonNullable;
        }

        /// <summary>
        /// Finds a driver or throws a <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="r">This resolver.</param>
        /// <param name="t">The type for which a driver must be resolved.</param>
        /// <param name="nullable">
        /// When not null, requests the nullable or not nullable driver regardless of the nullability of type itself.
        /// </param>
        /// <returns>The driver.</returns>
        public static ISerializationDriver FindDriver( this ISerializerResolver r, Type t, bool? nullable = null )
        {
            var d = r.TryFindDriver( t );
            if( d == null )
            {
                Throw.InvalidOperationException( $"Unable to find a serialization driver for type '{t}'." );
            }
            if( nullable.HasValue )
            {
                d = nullable.Value ? d.ToNullable : d.ToNonNullable;
            }
            return d;
        }

    }
}
