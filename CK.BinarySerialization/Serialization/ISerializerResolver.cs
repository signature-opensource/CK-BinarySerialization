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
    /// a pure factory (doesn't cache its result) that relies on the <see cref="SharedBinarySerializerContext"/>
    /// 
    /// </para>
    /// </summary>
    public interface ISerializerResolver
    {
        /// <summary>
        /// Finds an untyped serialization driver for a type.
        /// The driver's nullability is driven by the type. 
        /// Reference type defaults to nullable (rule of the oblivious nullable context).
        /// </summary>
        /// <param name="t">The type for which a driver must be found.</param>
        /// <returns>The driver or null.</returns>
        ISerializationDriver? TryFindDriver( Type t );
    }

    public static class SerializerResolverExtensions
    {
        public static TypedWriter<T> FindWriter<T>( this ISerializerResolver r, bool? nullable = null )
        {
            var d = FindDriver( r, typeof( T ), nullable );
            return (TypedWriter<T>)d.TypedWriter;
        }

        /// <summary>
        /// Finds an untyped serialization nullable or non nullable driver for a type.
        /// </summary>
        /// <param name="r">This resolver.</param>
        /// <param name="t">The type for which a driver must be resolved.</param>
        /// <param name="nullable">
        /// When not null, requests the <see cref="INullableSerializationDriver"/> or <see cref="INonNullableSerializationDriver"/>
        /// regardless of the nullability of type itself.
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
        
        public static ISerializationDriver FindDriver( this ISerializerResolver r, Type t, bool? nullable = null )
        {
            var d = r.TryFindDriver( t );
            if( d == null )
            {
                throw new InvalidOperationException( $"Unable to find a serialization driver for type '{t}'." );
            }
            if( nullable.HasValue )
            {
                d = nullable.Value ? d.ToNullable : d.ToNonNullable;
            }
            return d;
        }

    }
}
