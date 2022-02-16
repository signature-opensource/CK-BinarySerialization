using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
                if( tGen.Namespace == "System" )
                {
                    if( tGen == typeof( Nullable<> ) )
                    {
                        var u = Nullable.GetUnderlyingType( t )!;
                        var d = _resolver.TryFindDriver( u, true );
                        _cache.TryAdd( t, d );
                        return d;
                    }
                    if( tGen.Name.StartsWith( "ValueTuple`" ) )
                    {
                        return _cache.GetOrAdd( t, CreateTuple, true );
                    }
                    if( tGen.Name.StartsWith( "Tuple`") )
                    {
                        return _cache.GetOrAdd( t, CreateTuple, false );
                    }
                }
                if( tGen == typeof( List<> ) )
                {
                    return _cache.GetOrAdd( t, CreateSingleGenericParam, typeof( Serialization.DList<> ) );
                }
                if( tGen == typeof( Dictionary<,> ) )
                {
                    return _cache.GetOrAdd( t, CreateDoubleGenericParam, typeof( Serialization.DDictionary<,> ) );
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
            int rank = t.GetArrayRank();
            if( rank == 1 )
            {
                var t1 = typeof( Serialization.DArray<> ).MakeGenericType( tE );
                return ((ISerializationDriver)Activator.CreateInstance( t1, dItem.TypedWriter )!).ToNullable;
            }
            var tM = typeof( Serialization.DArrayMD<,> ).MakeGenericType( t, tE );
            return ((ISerializationDriver)Activator.CreateInstance( tM, dItem.TypedWriter )!).ToNullable;
        }

        ISerializationDriver? CreateEnum( Type t )
        {
            var tU = Enum.GetUnderlyingType( t );
            var dU = _resolver.TryFindDriver( tU );
            if( dU == null ) return null;
            var tS = typeof( Serialization.DEnum<,>).MakeGenericType( t, tU );
            return (ISerializationDriver)Activator.CreateInstance( tS, dU.TypedWriter )!;
        }

        ISerializationDriver? CreateSingleGenericParam( Type t, Type tGenD )
        {
            var tE = t.GetGenericArguments()[0];
            var dItem = _resolver.TryFindDriver( tE );
            if( dItem == null ) return null;
            var tS = tGenD.MakeGenericType( tE );
            Debug.Assert( t.IsClass, "All single generics are reference type: oblivious rules for now." );
            return ((ISerializationDriver)Activator.CreateInstance( tS, dItem.TypedWriter )!).ToNullable;
        }

        ISerializationDriver? CreateDoubleGenericParam( Type t, Type tGenD )
        {
            Debug.Assert( t.IsClass, "All single generics are reference type: oblivious rules for now except for the Dictionary key that is hard coded here." );
            
            var tE1 = t.GetGenericArguments()[0];
            var dItem1 = _resolver.TryFindDriver( tE1 );
            if( dItem1 == null ) return null;

            var tE2 = t.GetGenericArguments()[1];
            var dItem2 = _resolver.TryFindDriver( tE2 );
            if( dItem2 == null ) return null;

            var tS = tGenD.MakeGenericType( tE1, tE2 );
            
            var d = (ISerializationDriver)Activator.CreateInstance( tS, dItem1.TypedWriter, dItem2.TypedWriter )!;
            return tGenD == typeof( Dictionary<,> )
                    ? d
                    : d.ToNullable;
        }

        ISerializationDriver? CreateTuple( Type t, bool isValue )
        {
            var parameters = t.GetGenericArguments();
            var p = new UntypedWriter[parameters.Length];
            for( int i = 0; i < parameters.Length; i++ )
            {
                var d = _resolver.TryFindDriver( parameters[i] );
                if( d == null ) return null;
                p[i] = d.UntypedWriter;
            }
            if( isValue )
            {
                var tS = typeof( Serialization.DValueTuple<> ).MakeGenericType( t );
                return (ISerializationDriver?)Activator.CreateInstance( tS, new object?[] { p } );
            }
            var tR = typeof( Serialization.DTuple<> ).MakeGenericType( t );
            return ((ISerializationDriver?)Activator.CreateInstance( tR, new object?[] { p } )!).ToNullable;
        }

    }
}
