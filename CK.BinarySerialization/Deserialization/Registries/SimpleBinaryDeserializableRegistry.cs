﻿using CK.Core;
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
    /// Static thread safe registry for <see cref="ICKSimpleBinarySerializable"/> and <see cref="ICKVersionedBinarySerializable"/>
    /// deserializers.
    /// <para>
    /// Since this kind on serialization don't need any other resolvers (drivers only depends on the actual type), a singleton
    /// cache is fine and it uses the <see cref="SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers"/>
    /// cache since the synthesized drivers only depends on the local type.
    /// </para>
    /// </summary>
    public sealed class SimpleBinaryDeserializableRegistry : IDeserializerResolver
    {
        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly IDeserializerResolver Instance = new SimpleBinaryDeserializableRegistry();

        SimpleBinaryDeserializableRegistry() { }

        public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
        {
            // Only the "SimpleBinarySerializable" or "SealedVersionBinarySerializable" drivers are handled here.
            bool isSimple = info.DriverName == "SimpleBinarySerializable";
            bool isSealed = !isSimple && info.DriverName == "SealedVersionBinarySerializable";
            if( !isSimple && !isSealed ) return null;

            // We allow Simple to be read back as Sealed and the opposite.
            // We lookup the TargetType constructors here. Should we handle the case where the TargetType has no 
            // simple deserializer constructors? 
            // We may lookup the LocalType simple deserializer constructors as well and handle the conversion
            // from Local to TargetType via a constructor TargetType( LocalType ) (and/or Convert.ChangeType and
            // standard .Net TypeConverters).
            // For the moment, there's no such conversions.

            // We cache the driver only if the TargetType is the LocalType ("nominal" deserializers).
            // We may have duplicate calls to Create here (that should barely happen but who knows), but GetOrAdd
            // will return the winner.
            // 
            var ctor = info.TargetType.GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, _sealedCPTypes, null );
            if( ctor != null )
            {
                if( info.IsTargetSameAsLocalType )
                {
                    return SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers.GetOrAdd( info.TargetType, CreateCachedSealed, ctor );
                }
                if( info.TargetType.IsValueType )
                {
                    var tGen = info.ReadInfo.IsValueType
                                ? typeof( SealedBinaryDeserializableDriverV<> )
                                : typeof( SealedBinaryDeserializableDriverVFromRef<> );
                    return (IDeserializationDriver)Activator.CreateInstance( tGen.MakeGenericType( info.TargetType ), ctor )!;
                }
                var tR = typeof( SealedBinaryDeserializableDriverR<> ).MakeGenericType( info.TargetType );
                return (IDeserializationDriver)Activator.CreateInstance( tR, ctor )!;
            }
            ctor = info.TargetType.GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, _simpleCPTypes, null );
            if( ctor != null )
            {
                if( info.IsTargetSameAsLocalType )
                {
                    return SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers.GetOrAdd( info.TargetType, CreateCachedSimple, ctor );
                }
                if( info.TargetType.IsValueType )
                {
                    var tGen = info.ReadInfo.IsValueType
                                ? typeof( SimpleBinaryDeserializableDriverV<> )
                                : typeof( SimpleBinaryDeserializableDriverVFromRef<> );
                    return (IDeserializationDriver)Activator.CreateInstance( tGen.MakeGenericType( info.TargetType ), ctor )!;
                }
                var tR = typeof( SimpleBinaryDeserializableDriverR<> ).MakeGenericType( info.TargetType );
                return (IDeserializationDriver)Activator.CreateInstance( tR, ctor )!;
            }
            return null;
        }

        static readonly Type[] _simpleCPTypes = new Type[] { typeof( ICKBinaryReader ) };
        static readonly ParameterExpression[] _simpleCPExpressions = new ParameterExpression[] { Expression.Parameter( typeof( ICKBinaryReader ) ) };

        sealed class SimpleBinaryDeserializableDriverVFromRef<T> : ValueTypeDeserializerWithRef<T> where T : struct
        {
            readonly Func<ICKBinaryReader, T> _factory;

            public SimpleBinaryDeserializableDriverVFromRef( ConstructorInfo ctor )
            {
                _factory = (Func<ICKBinaryReader, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, T> ), _simpleCPExpressions, ctor );
            }

            protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _factory( d.Reader );
        }

        sealed class SimpleBinaryDeserializableDriverV<T> : ValueTypeDeserializer<T> where T : struct
        {
            readonly Func<ICKBinaryReader, T> _factory;

            public SimpleBinaryDeserializableDriverV( ConstructorInfo ctor )
            {
                _factory = (Func<ICKBinaryReader, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, T> ), _simpleCPExpressions, ctor );
            }

            protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _factory( d.Reader );
        }

        sealed class SimpleBinaryDeserializableDriverR<T> : SimpleReferenceTypeDeserializer<T> where T : class
        {
            readonly Func<ICKBinaryReader, T> _factory;

            public SimpleBinaryDeserializableDriverR( ConstructorInfo ctor )
            {
                _factory = (Func<ICKBinaryReader, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, T> ), _simpleCPExpressions, ctor );
            }

            protected override T ReadInstance( ICKBinaryReader r, ITypeReadInfo readInfo ) => _factory( r );
        }

        static IDeserializationDriver CreateCachedSimple( Type t, ConstructorInfo ctor )
        {
            if( t.IsValueType )
            {
                var tV = typeof( SimpleBinaryDeserializableDriverV<> ).MakeGenericType( t );
                return (IDeserializationDriver)Activator.CreateInstance( tV, ctor )!;
            }
            var tR = typeof( SimpleBinaryDeserializableDriverR<> ).MakeGenericType( t );
            return (IDeserializationDriver)Activator.CreateInstance( tR, ctor )!;
        }

        static readonly Type[] _sealedCPTypes = new Type[] { typeof( ICKBinaryReader ), typeof( int ) };
        static readonly ParameterExpression[] _sealedCPExpressions = new ParameterExpression[] { Expression.Parameter( typeof( ICKBinaryReader ) ), Expression.Parameter( typeof( int ) ) };

        sealed class SealedBinaryDeserializableDriverVFromRef<T> : ValueTypeDeserializerWithRef<T> where T : struct
        {
            readonly Func<ICKBinaryReader, int, T> _factory;

            public SealedBinaryDeserializableDriverVFromRef( ConstructorInfo ctor )
            {
                _factory = (Func<ICKBinaryReader, int, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, int, T> ), _sealedCPExpressions, ctor );
            }

            protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _factory( d.Reader, d.SerializerVersion );
        }

        sealed class SealedBinaryDeserializableDriverV<T> : ValueTypeDeserializer<T> where T : struct
        {
            readonly Func<ICKBinaryReader, int, T> _factory;

            public SealedBinaryDeserializableDriverV( ConstructorInfo ctor )
            {
                _factory = (Func<ICKBinaryReader, int, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, int, T> ), _sealedCPExpressions, ctor );
            }

            protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _factory( d.Reader, d.SerializerVersion );
        }

        sealed class SealedBinaryDeserializableDriverR<T> : SimpleReferenceTypeDeserializer<T> where T : class
        {
            readonly Func<ICKBinaryReader, int, T> _factory;

            public SealedBinaryDeserializableDriverR( ConstructorInfo ctor )
            {
                _factory = (Func<ICKBinaryReader, int, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, int, T> ), _sealedCPExpressions, ctor );
            }

            protected override T ReadInstance( ICKBinaryReader r, ITypeReadInfo readInfo ) => _factory( r, readInfo.SerializationVersion );
        }

        static IDeserializationDriver CreateCachedSealed( Type t, ConstructorInfo ctor )
        {
            if( t.IsValueType )
            {
                var tV = typeof( SealedBinaryDeserializableDriverV<> ).MakeGenericType( t );
                return (IDeserializationDriver)Activator.CreateInstance( tV, ctor )!;
            }
            var tR = typeof( SealedBinaryDeserializableDriverR<> ).MakeGenericType( t );
            return (IDeserializationDriver)Activator.CreateInstance( tR , ctor )!;
        }

        /// <summary>
        /// Helper that generates a new call.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="delegateType">The signature of the delegate.</param>
        /// <param name="expressionParameters">The parameters of the delegate.</param>
        /// <param name="c">The constructor to call.</param>
        /// <returns>The delegate.</returns>
        public static Delegate CreateNewDelegate<T>( Type delegateType, ParameterExpression[] expressionParameters, ConstructorInfo c )
        {
            var newExpression = Expression.Lambda( delegateType,
                                                   Expression.Convert( Expression.New( c, expressionParameters ), typeof( T ) ),
                                                   expressionParameters );
            return newExpression.Compile();
        }

    }
}
