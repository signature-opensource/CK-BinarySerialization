using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Thread safe registry that handles Array, List, Dictionary, Tuple, ValueTuple, KeyValuePair and other generics.
    /// <para>
    /// This registry is bound to a root resolver (that must also be thread safe).
    /// </para>
    /// </summary>
    public class StandardGenericSerializerRegistry : ISerializerResolver
    {
        readonly ConcurrentDictionary<Type, ISerializationDriver?> _cache;
        readonly ISerializerResolver _resolver;

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly StandardGenericSerializerRegistry Default = new StandardGenericSerializerRegistry( BinarySerializer.DefaultSharedContext );

        /// <summary>
        /// Initializes a new registry.
        /// </summary>
        /// <param name="resolver">The root resolver to use.</param>
        public StandardGenericSerializerRegistry( ISerializerResolver resolver ) 
        {
            _cache = new ConcurrentDictionary<Type, ISerializationDriver?>();
            _resolver = resolver;
        }

        /// <inheritdoc />
        public ISerializationDriver? TryFindDriver( Type t )
        {
            if( t.ContainsGenericParameters ) return null;
            if( t.IsArray )
            {
                return _cache.GetOrAdd( t, CreateArray );
            }
            if( t.IsEnum )
            {
                return _cache.GetOrAdd( t, CreateEnum );
            }
            if( t.IsGenericType )
            {
                var tGen = t.GetGenericTypeDefinition();
                if( tGen == typeof( List<> ) )
                {
                    return _cache.GetOrAdd( t, CreateSingleGenericParam, typeof( Serialization.DList<> ) );
                }
                if( tGen == typeof( Stack<> ) )
                {
                    return _cache.GetOrAdd( t, CreateSingleGenericParam, typeof( Serialization.DStack<> ) );
                }
                if( tGen == typeof( Queue<> ) )
                {
                    return _cache.GetOrAdd( t, CreateSingleGenericParam, typeof( Serialization.DQueue<> ) );
                }
            }
            return null;
        }

        ISerializationDriver? CreateArray( Type t )
        {
            var tE = t.GetElementType()!;
            var dItem = _resolver.TryFindDriver( tE );
            if( dItem == null ) return null;    
            var tS = typeof( Serialization.DArray<> ).MakeGenericType( tE );
            return (ISerializationDriver)Activator.CreateInstance( tS, dItem.TypedWriter )!;
        }

        ISerializationDriver? CreateSingleGenericParam( Type t, Type tGenD )
        {
            var tE = t.GetGenericArguments()[0];
            var dItem = _resolver.TryFindDriver( tE );
            if( dItem == null ) return null;
            var tS = tGenD.MakeGenericType( tE );
            return (ISerializationDriver)Activator.CreateInstance( tS, dItem.TypedWriter )!;
        }

        ISerializationDriver? CreateEnum( Type t )
        {
            var tU = Enum.GetUnderlyingType( t );
            var dU = _resolver.TryFindDriver( tU );
            if( dU == null ) return null;
            var tS = typeof( Serialization.DEnum<,>).MakeGenericType( t, tU );
            return (ISerializationDriver)Activator.CreateInstance( tS, dU.TypedWriter )!;
        }
    }
}
