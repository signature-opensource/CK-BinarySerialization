using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Finds a serializer for a type.
    /// </summary>
    public interface ISerializerResolver
    {
        /// <summary>
        /// Finds an untyped serialization driver for a type.
        /// </summary>
        /// <param name="t">The type for which a driver must be found.</param>
        /// <returns>The driver or null.</returns>
        ISerializationDriver? TryFindDriver( Type t );
    }

    public static class SerializerResolverExtensions
    {
        public static TypedWriter<T> FindWriter<T>( this ISerializerResolver r )
        {
            var d = FindDriver( r, typeof( T ) );
            return (TypedWriter<T>)d.TypedWriter;
        }

        public static ISerializationDriver FindDriver( this ISerializerResolver r, Type t )
        {
            var d = r.TryFindDriver( t );
            if( d == null )
            {
                throw new InvalidOperationException( $"Unable to find a serialization driver for type '{t}'." );
            }
            return d;
        }

    }
}
