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
        /// Finds a serialization driver for a Type.
        /// </summary>
        /// <typeparam name="T">The type for which a driver must be found.</typeparam>
        /// <returns>The driver or null.</returns>
        ISerializationDriver<T>? TryFindDriver<T>();

        /// <summary>
        /// Finds a serialization driver for a Type.
        /// </summary>
        /// <param name="t">The type for which a driver must be found.</param>
        /// <returns>The driver or null.</returns>
        IUntypedSerializationDriver? TryFindDriver( Type t );
    }

    public static class SerializerResolverExtensions
    {
        public static ISerializationDriver<T> FindDriver<T>( this ISerializerResolver r )
        {
            var d = r.TryFindDriver<T>();
            if( d == null )
            {
                throw new InvalidOperationException( $"Unable to find a serialization driver for type '{typeof( T )}'." );
            }
            return d;
        }

        public static IUntypedSerializationDriver FindDriver( this ISerializerResolver r, Type t )
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
