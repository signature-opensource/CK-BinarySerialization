using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Thread safe factory that handles Array, List, Dictionary, Tuple, ValueTuple, KeyValuePair and other generics.
    /// <para>
    /// This registry doesn't cache anything: caching is handled by the <see cref="SharedBinaryDeserializerContext"/>.
    /// </para>
    /// </summary>
    public class StandardGenericSerializerFactory : ISerializerResolver
    {
        readonly SharedBinarySerializerContext _resolver;

        /// <summary>
        /// Initializes a new factory.
        /// </summary>
        /// <param name="resolver">The root resolver to use.</param>
        public StandardGenericSerializerFactory( SharedBinarySerializerContext resolver ) 
        {
            _resolver = resolver;
        }

        /// <inheritdoc />
        public ISerializationDriver? TryFindDriver( Type t )
        {
            if( t.ContainsGenericParameters ) return null;
            if( t.IsArray )
            {
                return TryCreateArray( t );
            }
            if( t.IsEnum )
            {
                return TryCreateEnum( t );
            }
            if( t.IsGenericType )
            {
                var tGen = t.GetGenericTypeDefinition();
                if( tGen.Namespace == "System" )
                {
                    if( tGen == typeof( Nullable<> ) )
                    {
                        var u = Nullable.GetUnderlyingType( t )!;
                        return _resolver.TryFindDriver( u )?.ToNullable;
                    }
                    if( tGen.Name.StartsWith( "ValueTuple`" ) )
                    {
                        return TryCreateTuple( t, true );
                    }
                    if( tGen.Name.StartsWith( "Tuple`") )
                    {
                        return TryCreateTuple( t, false );
                    }
                }
                if( tGen == typeof( List<> ) )
                {
                    return TryCreateSingleGenericParam( t, typeof( Serialization.DList<> ) );
                }
                if( tGen == typeof( Dictionary<,> ) )
                {
                    return TryCreateDoubleGenericParam( t, typeof( Serialization.DDictionary<,> ) );
                }
                if( tGen == typeof( Stack<> ) )
                {
                    return TryCreateSingleGenericParam( t, typeof( Serialization.DStack<> ) );
                }
                if( tGen == typeof( Queue<> ) )
                {
                    return TryCreateSingleGenericParam( t, typeof( Serialization.DQueue<> ) );
                }
            }
            return null;
        }

        ISerializationDriver? TryCreateArray( Type t )
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

        ISerializationDriver? TryCreateEnum( Type t )
        {
            var tU = Enum.GetUnderlyingType( t );
            var dU = _resolver.TryFindDriver( tU );
            if( dU == null ) return null;
            var tS = typeof( Serialization.DEnum<,>).MakeGenericType( t, tU );
            return (ISerializationDriver)Activator.CreateInstance( tS, dU.TypedWriter )!;
        }

        ISerializationDriver? TryCreateSingleGenericParam( Type t, Type tGenD )
        {
            var tE = t.GetGenericArguments()[0];
            var dItem = _resolver.TryFindDriver( tE );
            if( dItem == null ) return null;
            var tS = tGenD.MakeGenericType( tE );
            Debug.Assert( t.IsClass, "All single generics are reference type: oblivious rules for now." );
            return ((ISerializationDriver)Activator.CreateInstance( tS, dItem.TypedWriter )!).ToNullable;
        }

        ISerializationDriver? TryCreateDoubleGenericParam( Type t, Type tGenD )
        {
            var tE1 = t.GetGenericArguments()[0];
            var dItem1 = _resolver.TryFindDriver( tE1 );
            if( dItem1 == null ) return null;
            Debug.Assert( tGenD == typeof( Serialization.DDictionary<,> ), "Dictionary is currently the only Double params." );
            dItem1 = dItem1.ToNonNullable;

            var tE2 = t.GetGenericArguments()[1];
            var dItem2 = _resolver.TryFindDriver( tE2 );
            if( dItem2 == null ) return null;

            var tS = tGenD.MakeGenericType( tE1, tE2 );
            
            var d = (ISerializationDriver)Activator.CreateInstance( tS, dItem1.TypedWriter, dItem2.TypedWriter )!;
            return d.ToNullable;
        }

        ISerializationDriver? TryCreateTuple( Type t, bool isValue )
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
