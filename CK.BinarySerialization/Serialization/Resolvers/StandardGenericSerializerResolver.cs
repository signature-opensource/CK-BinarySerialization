using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Thread safe factory that handles System.Array, System.Enum, System.Tuple, System.ValueTuple, System.Nullable,
    /// System.Collections.Generic.List, System.Collections.Generic.Dictionary, System.Collections.Generic.HashSet,
    /// System.Collections.Generic.Stack, System.Collections.Generic.Queue, System.Collections.Generic.KeyValuePair.
    /// <para>
    /// This registry doesn't cache anything: caching is handled by the <see cref="SharedBinarySerializerContext"/>
    /// and <see cref="BinarySerializerContext"/>.
    /// </para>
    /// </summary>
    public sealed class StandardGenericSerializerResolver : ISerializerResolver
    {
        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static readonly StandardGenericSerializerResolver Instance = new StandardGenericSerializerResolver();

        StandardGenericSerializerResolver()
        {
        }

        /// <inheritdoc />
        public ISerializationDriver? TryFindDriver( BinarySerializerContext context, Type t )
        {
            if( t.ContainsGenericParameters ) return null;
            if( t.IsArray )
            {
                return TryCreateArray( context, t );
            }
            if( t.IsEnum )
            {
                return TryCreateEnum( context, t );
            }
            if( t.IsGenericType )
            {
                var tGen = t.GetGenericTypeDefinition();
                if( tGen.Namespace == "System" )
                {
                    if( tGen == typeof( Nullable<> ) )
                    {
                        var u = Nullable.GetUnderlyingType( t )!;
                        return context.TryFindDriver( u )?.ToNullable;
                    }
                    if( tGen.Name.StartsWith( "ValueTuple`" ) )
                    {
                        return TryCreateTuple( context, t, true );
                    }
                    if( tGen.Name.StartsWith( "Tuple`") )
                    {
                        return TryCreateTuple( context, t, false );
                    }
                }
                if( tGen.Namespace == "System.Collections.Generic" )
                {
                    if( tGen == typeof( List<> ) )
                    {
                        return TryCreateSingleGenericParam( context, t, typeof( Serialization.DList<> ) );
                    }
                    if( tGen == typeof( Dictionary<,> ) )
                    {
                        return TryCreateDoubleGenericParam( context, t, typeof( Serialization.DDictionary<,> ), false );
                    }
                    if( tGen == typeof( HashSet<> ) )
                    {
                        return TryCreateSingleGenericParam( context, t, typeof( Serialization.DHashSet<> ) );
                    }
                    if( tGen == typeof( Stack<> ) )
                    {
                        return TryCreateSingleGenericParam( context, t, typeof( Serialization.DStack<> ) );
                    }
                    if( tGen == typeof( Queue<> ) )
                    {
                        return TryCreateSingleGenericParam( context, t, typeof( Serialization.DQueue<> ) );
                    }
                    if( tGen == typeof( KeyValuePair<,> ) )
                    {
                        return TryCreateDoubleGenericParam( context, t, typeof( Serialization.DKeyValuePair<,> ), true );
                    }
                }
            }
            return null;
        }

        static ISerializationDriver? TryCreateArray( BinarySerializerContext context, Type t )
        {
            var tE = t.GetElementType()!;
            var dItem = context.TryFindPossiblyAbstractDriver( tE );
            if( dItem == null ) return null;
            int rank = t.GetArrayRank();
            if( rank == 1 )
            {
                var t1 = typeof( Serialization.DArray<> ).MakeGenericType( tE );
                return ((ISerializationDriver)Activator.CreateInstance( t1, dItem.TypedWriter, dItem.CacheLevel )!).ToNullable;
            }
            var tM = typeof( Serialization.DArrayMD<,> ).MakeGenericType( t, tE );
            return ((ISerializationDriver)Activator.CreateInstance( tM, dItem.TypedWriter, dItem.CacheLevel )!).ToNullable;
        }

        static ISerializationDriver? TryCreateEnum( BinarySerializerContext context, Type t )
        {
            var tU = Enum.GetUnderlyingType( t );
            var dU = context.TryFindDriver( tU );
            if( dU == null ) return null;   
            var tS = typeof( Serialization.DEnum<,>).MakeGenericType( t, tU );
            return (ISerializationDriver)Activator.CreateInstance( tS, dU.TypedWriter )!;
        }

        static ISerializationDriver? TryCreateSingleGenericParam( BinarySerializerContext context, Type t, Type tGenD )
        {
            var tE = t.GetGenericArguments()[0];
            var dItem = context.TryFindPossiblyAbstractDriver( tE );
            if( dItem == null ) return null;
            var tS = tGenD.MakeGenericType( tE );
            Debug.Assert( t.IsClass, "All single generics are reference type: oblivious rules for now." );
            return ((ISerializationDriver)Activator.CreateInstance( tS, dItem.TypedWriter, dItem.CacheLevel )!).ToNullable;
        }

        static ISerializationDriver? TryCreateDoubleGenericParam( BinarySerializerContext context, Type t, Type tGenD, bool isValue )
        {
            var tE1 = t.GetGenericArguments()[0];
            var dItem1 = context.TryFindPossiblyAbstractDriver( tE1 );
            if( dItem1 == null ) return null;
            // Awful trick for non nullable dictionary key.
            if( tGenD == typeof( Serialization.DDictionary<,> ) )
            {
                dItem1 = dItem1.ToNonNullable;
            }
            var tE2 = t.GetGenericArguments()[1];
            var dItem2 = context.TryFindPossiblyAbstractDriver( tE2 );
            if( dItem2 == null ) return null;
            var tS = tGenD.MakeGenericType( tE1, tE2 );
            
            var d = (ISerializationDriver)Activator.CreateInstance( tS, dItem1.TypedWriter, dItem2.TypedWriter, dItem1.CacheLevel.Combine( dItem2.CacheLevel ) )!;
            return isValue ? d : d.ToNullable;
        }

        static ISerializationDriver? TryCreateTuple( BinarySerializerContext context, Type t, bool isValue )
        {
            var parameters = t.GetGenericArguments();
            var p = new Delegate[parameters.Length];
            var cache = SerializationDriverCacheLevel.SharedContext;
            for( int i = 0; i < parameters.Length; i++ )
            {
                var d = context.TryFindPossiblyAbstractDriver( parameters[i] );
                if( d == null ) return null;    
                p[i] = d.UntypedWriter;
                cache = cache.Combine( d.CacheLevel );
            }
            if( isValue )
            {
                var tS = typeof( Serialization.DValueTuple<> ).MakeGenericType( t );
                return (ISerializationDriver)Activator.CreateInstance( tS, new object?[] { p, cache } )!;
            }
            var tR = typeof( Serialization.DTuple<> ).MakeGenericType( t );
            return ((ISerializationDriver)Activator.CreateInstance( tR, new object?[] { p, cache } )!).ToNullable;
        }

    }
}
