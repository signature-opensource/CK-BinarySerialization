using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Factory methods for <see cref="IBinaryDeserializer"/> and default <see cref="SharedBinaryDeserializerContext"/>
    /// and <see cref="SharedDeserializerKnownObject"/>.
    /// </summary>
    public static class BinaryDeserializer
    {
        /// <summary>
        /// Gets the default thread safe static registry of <see cref="IDeserializerResolver"/>.
        /// </summary>
        public static readonly SharedBinaryDeserializerContext DefaultSharedContext;

        static BinaryDeserializer()
        {
            DefaultSharedContext = new SharedBinaryDeserializerContext();
#if NETCOREAPP3_1
            // Works around the lack of [ModuleInitializer] by an awful trick.
            Type? tSliced = Type.GetType( "CK.BinarySerialization.SlicedDeserializerRegistry, CK.BinarySerialization.Sliced", throwOnError: false );
            if( tSliced != null )
            {
                var sliced = (IDeserializerResolver)tSliced.GetField( "Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static )!.GetValue( null )!;
                DefaultSharedContext.Register( sliced, false );
            }
#endif
        }

        /// <summary>
        /// Creates a new disposable deserializer bound to a <see cref="BinaryDeserializerContext"/>
        /// that can be reused when the deserializer is disposed.
        /// </summary>
        /// <param name="s">The stream.</param>
        /// <param name="leaveOpen">True to leave the stream opened, false to close it when the deserializer is disposed.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A disposable deserializer.</returns>
        public static IDisposableBinaryDeserializer Create( Stream s,
                                                            bool leaveOpen,
                                                            BinaryDeserializerContext context )
        {
            var reader = new CKBinaryReader( s, Encoding.UTF8, leaveOpen );
            var v = reader.ReadSmallInt32();
            if( v < 10 || v > BinarySerializer.SerializerVersion )
            {
                throw new InvalidDataException( $"Invalid deserializer version: {v}. Minimal is 10 and current is {BinarySerializer.SerializerVersion}." );
            }
            var sameEndianness = reader.ReadBoolean() == BitConverter.IsLittleEndian;
            return new BinaryDeserializerImpl( v, reader, leaveOpen, context, sameEndianness );
        }

        /// <summary>
        /// Deserializers implementation helpers.
        /// </summary>
        public static class Helper
        {
            static readonly Type[] _simpleTypes = new Type[] { typeof( ICKBinaryReader ) };
            static readonly ParameterExpression[] _simpleExpressions = new ParameterExpression[] { Expression.Parameter( typeof( ICKBinaryReader ) ) };

            static readonly Type[] _versionedTypes = new Type[] { typeof( ICKBinaryReader ), typeof( int ) };
            static readonly ParameterExpression[] _versionedExpressions = new ParameterExpression[] { Expression.Parameter( typeof( ICKBinaryReader ) ), Expression.Parameter( typeof( int ) ) };

            static readonly Type[] _typedReaderTypes = new Type[] { typeof( IBinaryDeserializer ), typeof( ITypeReadInfo ) };
            static readonly ParameterExpression[] _typedReaderExpressions = new ParameterExpression[] { Expression.Parameter( typeof( IBinaryDeserializer ) ), Expression.Parameter( typeof( ITypeReadInfo ) ) };

            /// <summary>
            /// Tries to get the simple constructor with a single <see cref="ICKBinaryReader"/> parameter.
            /// </summary>
            /// <param name="t">The type.</param>
            /// <returns>The simple constructor or null.</returns>
            public static ConstructorInfo? GetSimpleConstructor( Type t )
            {
                return t.GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, _simpleTypes, null );
            }

            /// <summary>
            /// Creates a delegate that can instantiate a new <typeparamref name="T"/> by calling the simple 
            /// constructor (see <see cref="GetSimpleConstructor(Type)"/>).
            /// </summary>
            /// <typeparam name="T">The type to deserialize.</typeparam>
            /// <param name="ctor">The constructor.</param>
            /// <returns>A delegate that calls new with the simple constructor.</returns>
            public static Func<ICKBinaryReader, T> CreateSimpleNewDelegate<T>( ConstructorInfo ctor )
            {
                return (Func<ICKBinaryReader, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, T> ), _simpleExpressions, ctor );
            }

            /// <summary>
            /// Tries to get the versioned constructor with <see cref="ICKBinaryReader"/> and int version parameters.
            /// </summary>
            /// <param name="t">The type.</param>
            /// <returns>The versioned constructor or null.</returns>
            public static ConstructorInfo? GetVersionedConstructor( Type t )
            {
                return t.GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, _versionedTypes, null );
            }

            /// <summary>
            /// Creates a delegate that can instantiate a new <typeparamref name="T"/> by calling the versioned 
            /// constructor (see <see cref="GetVersionedConstructor(Type)"/>).
            /// </summary>
            /// <typeparam name="T">The type to deserialize.</typeparam>
            /// <param name="ctor">The constructor.</param>
            /// <returns>A delegate that calls new with the versioned constructor.</returns>
            public static Func<ICKBinaryReader, int, T> CreateVersionedNewDelegate<T>( ConstructorInfo ctor )
            {
                return (Func<ICKBinaryReader, int, T>)CreateNewDelegate<T>( typeof( Func<ICKBinaryReader, int, T> ), _versionedExpressions, ctor );
            }

            /// <summary>
            /// Tries to get the constructor with <see cref="IBinaryDeserializer"/> and <see cref="ITypeReadInfo"/> parameters.
            /// </summary>
            /// <param name="t">The type.</param>
            /// <returns>The deserialization constructor or null.</returns>
            public static ConstructorInfo? GetTypedReaderConstructor( Type t )
            {
                return t.GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, _typedReaderTypes, null );
            }

            /// <summary>
            /// Creates a delegate that can instantiate a new <typeparamref name="T"/> by calling the deserialization 
            /// constructor (see <see cref="GetTypedReaderConstructor(Type)"/>).
            /// </summary>
            /// <typeparam name="T">The type to deserialize.</typeparam>
            /// <param name="ctor">The constructor.</param>
            /// <returns>A delegate that calls new with the versioned constructor.</returns>
            public static TypedReader<T> CreateTypedReaderNewDelegate<T>( ConstructorInfo ctor )
            {
                return (TypedReader<T>)CreateNewDelegate<T>( typeof( TypedReader<T> ), _typedReaderExpressions, ctor );
            }


            static Delegate CreateNewDelegate<T>( Type delegateType, ParameterExpression[] expressionParameters, ConstructorInfo c )
            {
                var newExpression = Expression.Lambda( delegateType,
                                                       Expression.Convert( Expression.New( c, expressionParameters ), typeof( T ) ),
                                                       expressionParameters );
                return newExpression.Compile();
            }
        }
    }
}
