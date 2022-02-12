using CK.Core;
using CK.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Static thread safe registry for <see cref="ICKSimpleBinarySerializable"/> and <see cref="ISealedVersionedSimpleSerializable"/>
    /// deserializers.
    /// <para>
    /// Since this kind on serialization don't need any other resolvers (drivers only depends on the actual type), a singleton
    /// cache is fine.
    /// </para>
    /// </summary>
    public class SimpleBinaryDeserializableRegistry : IDeserializerResolver
    {
        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly IDeserializerResolver Default = new SimpleBinaryDeserializableRegistry();

        SimpleBinaryDeserializableRegistry() { }

        public IDeserializationDriver? TryFindDriver( TypeReadInfo info )
        {
            // Cache only the driver if the local type can be resolved and is a ICKSimpleBinarySerializable
            // or a ISealedVersionedSimpleSerializable.
            try
            {
                if( info.DriverName == "SimpleBinarySerializable" )
                {
                    var t = info.TryResolveLocalType();
                    if( t == null ) return null;
                    if( !typeof( ICKSimpleBinarySerializable ).IsAssignableFrom( t ) )
                    {
                        throw new Exception( $"Type '{t}' has been serialized thanks to its ISimpleBinarySerializable implementation but it doesn't support it anymore." );
                    }
                    return SharedCache.Deserialization.GetOrAdd( t, CreateSimple );
                }
                if( info.DriverName == "SealedVersionBinarySerializable" )
                {
                    var t = info.TryResolveLocalType();
                    if( t == null ) return null;
                    if( !typeof( ISealedVersionedSimpleSerializable ).IsAssignableFrom( t ) )
                    {
                        throw new Exception( $"Type '{t}' has been serialized thanks to its ISealedVersionedSimpleSerializable implementation but it doesn't support it anymore." );
                    }
                    return SharedCache.Deserialization.GetOrAdd( t, CreateSealed );
                }
            }
            catch( TargetInvocationException ex )
            {
                if( ex.InnerException != null ) throw ex.InnerException;
                else throw;
            }
            return null;
        }

        static readonly Type[] _simpleCPTypes = new Type[] { typeof( ICKBinaryReader ) };
        static readonly ParameterExpression[] _simpleCPExpressions = new ParameterExpression[] { Expression.Parameter( typeof( ICKBinaryReader ) ) };

        sealed class SimpleBinaryDeserializableDriverV<T> : ValueTypeDeserializer<T> where T : struct, ICKSimpleBinarySerializable
        {
            readonly Func<ICKBinaryReader, T> _factory;

            public SimpleBinaryDeserializableDriverV()
            {
                _factory = (Func<ICKBinaryReader, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, T> ), _simpleCPExpressions, _simpleCPTypes );
            }

            protected override T ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => _factory( r.Reader );
        }

        sealed class SimpleBinaryDeserializableDriverR<T> : ReferenceTypeDeserializer<T> where T : class, ICKSimpleBinarySerializable
        {
            readonly Func<ICKBinaryReader, T> _factory;

            public SimpleBinaryDeserializableDriverR()
            {
                _factory = (Func<ICKBinaryReader, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, T> ), _simpleCPExpressions, _simpleCPTypes );
            }

            protected override T ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => _factory( r.Reader );
        }

        static IDeserializationDriver CreateSimple( Type t )
        {
            if( t.IsValueType )
            {
                var tV = typeof( SimpleBinaryDeserializableDriverV<> ).MakeGenericType( t );
                return (IDeserializationDriver)Activator.CreateInstance( tV )!;
            }
            var tR = typeof( SimpleBinaryDeserializableDriverR<> ).MakeGenericType( t );
            return (IDeserializationDriver)Activator.CreateInstance( tR )!;
        }

        static readonly Type[] _sealedCPTypes = new Type[] { typeof( ICKBinaryReader ), typeof( int ) };
        static readonly ParameterExpression[] _sealedCPExpressions = new ParameterExpression[] { Expression.Parameter( typeof( ICKBinaryReader ) ), Expression.Parameter( typeof( int ) ) };

        sealed class SealedBinaryDeserializableDriverV<T> : ValueTypeDeserializer<T> where T : struct, ISealedVersionedSimpleSerializable
        {
            readonly Func<ICKBinaryReader, int, T> _factory;

            public SealedBinaryDeserializableDriverV()
            {
                _factory = (Func<ICKBinaryReader, int, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, int, T> ), _sealedCPExpressions, _sealedCPTypes );
            }

            protected override T ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => _factory( r.Reader, r.SerializerVersion );
        }

        sealed class SealedBinaryDeserializableDriverR<T> : ReferenceTypeDeserializer<T> where T : class, ISealedVersionedSimpleSerializable
        {
            readonly Func<ICKBinaryReader, int, T> _factory;

            public SealedBinaryDeserializableDriverR()
            {
                _factory = (Func<ICKBinaryReader, int, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, int, T> ), _sealedCPExpressions, _sealedCPTypes );
            }

            protected override T ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => _factory( r.Reader, r.SerializerVersion );
        }

        static IDeserializationDriver CreateSealed( Type t )
        {
            if( t.IsValueType )
            {
                var tV = typeof( SealedBinaryDeserializableDriverV<> ).MakeGenericType( t );
                return (IDeserializationDriver)Activator.CreateInstance( tV )!;
            }
            var tR = typeof( SealedBinaryDeserializableDriverR<> ).MakeGenericType( t );
            return (IDeserializationDriver)Activator.CreateInstance( tR )!;
        }

        static Delegate CreateNewDelegate<T>( Type delegateType, ParameterExpression[] expressionParameters, Type[] parameterTypes )
        {
            var c = typeof( T ).GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null );
            if( c == null )
            {
                throw new InvalidOperationException( $"Type '{typeof( T )}' requires a constructor with ( {parameterTypes.Select(p=>p.Name).Concatenate()} ) parameters." );
            }
            var newExpression = Expression.Lambda( delegateType,
                                                   Expression.Convert( Expression.New( c, expressionParameters ), typeof( T ) ),
                                                   expressionParameters );
            return newExpression.Compile();
        }

    }
}
