using CK.Core;
using CK.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static readonly IDeserializerResolver Instance = new SimpleBinaryDeserializableRegistry();

        SimpleBinaryDeserializableRegistry() { }

        public IDeserializationDriver? TryFindDriver( TypeReadInfo info )
        {
            var t = info.TryResolveLocalType();
            if( t == null ) return null;
            bool isSimple = info.NonNullableDriverName == "SimpleBinarySerializable";
            bool isSealed = !isSimple && info.NonNullableDriverName == "SealedVersionBinarySerializable";
            if( !isSimple && !isSealed ) return null;

            if( info.IsNullable && info.Kind == TypeReadInfo.TypeKind.Generic )
            {
                Type? nullValueType = Nullable.GetUnderlyingType( t );
                if( nullValueType != null ) t = nullValueType;
            }
            var d = FindNonNullableDriver( t );
            Debug.Assert( d == null || d.ToNonNullable == d );
            return info.IsNullable ? d?.ToNullable : d;
        }

        private static IDeserializationDriver FindNonNullableDriver( Type t )
        {
            // We allow Simple to be read back as Sealed and the opposite.
            try
            {
                if( t.GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, _sealedCPTypes, null ) != null )
                {
                    return InternalShared.Deserialization.GetOrAdd( t, CreateSealed );
                }
                if( t.GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, _simpleCPTypes, null ) != null )
                {
                    return InternalShared.Deserialization.GetOrAdd( t, CreateSimple );
                }
            }
            catch( TargetInvocationException ex )
            {
                if( ex.InnerException != null ) throw ex.InnerException;
                else throw;
            }
            throw new InvalidOperationException( $"Type '{t}' has been serialized thanks to its Write( ICBinaryWriter ) method but it has no constructor( ICBinaryReader r ) or (ICBinaryWriter r, int version)." );
        }

        static readonly Type[] _simpleCPTypes = new Type[] { typeof( ICKBinaryReader ) };
        static readonly ParameterExpression[] _simpleCPExpressions = new ParameterExpression[] { Expression.Parameter( typeof( ICKBinaryReader ) ) };

        sealed class SimpleBinaryDeserializableDriverV<T> : ValueTypeDeserializer<T> where T : struct
        {
            readonly Func<ICKBinaryReader, T> _factory;

            public SimpleBinaryDeserializableDriverV()
            {
                _factory = (Func<ICKBinaryReader, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, T> ), _simpleCPExpressions, _simpleCPTypes );
            }

            protected override T ReadInstance( IBinaryDeserializer d, TypeReadInfo readInfo ) => _factory( d.Reader );
        }

        sealed class SimpleBinaryDeserializableDriverR<T> : SimpleReferenceTypeDeserializer<T> where T : class
        {
            readonly Func<ICKBinaryReader, T> _factory;

            public SimpleBinaryDeserializableDriverR()
            {
                _factory = (Func<ICKBinaryReader, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, T> ), _simpleCPExpressions, _simpleCPTypes );
            }

            protected override T ReadInstance( ICKBinaryReader r, TypeReadInfo readInfo ) => _factory( r );
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

        sealed class SealedBinaryDeserializableDriverV<T> : ValueTypeDeserializer<T> where T : struct
        {
            readonly Func<ICKBinaryReader, int, T> _factory;

            public SealedBinaryDeserializableDriverV()
            {
                _factory = (Func<ICKBinaryReader, int, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, int, T> ), _sealedCPExpressions, _sealedCPTypes );
            }

            protected override T ReadInstance( IBinaryDeserializer d, TypeReadInfo readInfo ) => _factory( d.Reader, d.SerializerVersion );
        }

        sealed class SealedBinaryDeserializableDriverR<T> : SimpleReferenceTypeDeserializer<T> where T : class
        {
            readonly Func<ICKBinaryReader, int, T> _factory;

            public SealedBinaryDeserializableDriverR()
            {
                _factory = (Func<ICKBinaryReader, int, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, int, T> ), _sealedCPExpressions, _sealedCPTypes );
            }

            protected override T ReadInstance( ICKBinaryReader r, TypeReadInfo readInfo ) => _factory( r, readInfo.SerializationVersion );
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
