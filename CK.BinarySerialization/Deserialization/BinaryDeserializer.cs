﻿using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
        }

        public class Result
        {
            readonly RewindableStream _s;
            internal BinaryDeserializerImpl? Deserializer;

            ExceptionDispatchInfo? _exception;
            string? _error;

            public IBinaryDeserializer.IStreamInfo StreamInfo => _s;
                        
            public bool IsValid => _error == null;

            public bool SecondPassRequired { get; internal set; }

            public string? Error => _error;

            public void ThrowOnInvalidResult()
            {
                if( !IsValid )
                {
                    if( _exception != null ) _exception.Throw();
                    throw new InvalidOperationException( _error );
                }
            }

            /// <summary>
            /// Gets the exception that made this result invalid.
            /// </summary>
            public Exception? Exception => _exception?.SourceException;

            internal Result( RewindableStream s, BinaryDeserializerContext context )
            {
                _s = s;
                if( !s.IsValid )
                {
                    if( s.SerializerVersion < 10 || s.SerializerVersion > BinarySerializer.SerializerVersion )
                    {
                        _error = $"Invalid deserializer version: {s.SerializerVersion}. Minimal is 10 and the current is {BinarySerializer.SerializerVersion}.";
                    }
                    else
                    {
                        _error = "The header of the stream cannot be read.";
                    }
                }
                else
                {
                    Deserializer = new BinaryDeserializerImpl( s, context );
                }
            }

            internal void SetException( ExceptionDispatchInfo ex )
            {
                _error = $"An exception occurred{(SecondPassRequired ? " during the second pass" : "")}: {ex.SourceException.Message}";
                _exception = ex;
            }

            internal bool ShouldRetry()
            {
                Debug.Assert( Deserializer != null );
                if( IsValid && !SecondPassRequired && Deserializer.ShouldStartSecondPass() )
                {
                    SecondPassRequired = true;
                    return true;
                }
                return false;
            }

            internal virtual void Terminate()
            {
                Debug.Assert( Deserializer != null );
                if( IsValid )
                {
                    try
                    {
                        Deserializer.PostActions.Execute();
                    }
                    catch( Exception ex )
                    {
                        _error = $"An exception occurred{(SecondPassRequired ? " during the second pass" : "")} while executing PostActions: {ex.Message}";
                        _exception = ExceptionDispatchInfo.Capture( ex );
                    }
                }
                Deserializer.Dispose();
                Deserializer = null;
            }
        }

        public sealed class Result<T> : Result
        {
            T? _result;

            internal Result( RewindableStream s, BinaryDeserializerContext context )
                : base( s, context )
            {
            }

            public T GetResult()
            {
                ThrowOnInvalidResult();
                return _result!;
            }

            public T? GetResult( bool throwOnInvalid )
            {
                if( throwOnInvalid )
                {
                    ThrowOnInvalidResult();
                }
                return _result;
            }

            public void SetResult( T result ) => _result = result;

            internal override void Terminate()
            {
                base.Terminate();
                if( !IsValid ) _result = default;
            }

        }


        /// <summary>
        /// Attempts to deserialize the content of a <see cref="RewindableStream"/> (that must be <see cref="RewindableStream.IsValid"/>) 
        /// bound to a <see cref="BinaryDeserializerContext"/> that can be reused once done.
        /// </summary>
        /// <param name="s">The rewindable stream.</param>
        /// <param name="context">The context to use.</param>
        /// <param name="deserializer">The deserializer action.</param>
        /// <returns>A result with a true <see cref="Result.IsValid"/> or that has captured errors.</returns>
        public static Result Deserialize( RewindableStream s, BinaryDeserializerContext context, Action<IBinaryDeserializer> deserializer )
        {
            var r = new Result( s, context );
            if( r.Deserializer != null )
            {
                retry:
                try
                {
                    deserializer( r.Deserializer );
                }
                catch( Exception ex )
                {
                    r.SetException( ExceptionDispatchInfo.Capture( ex ) );
                }
                if( r.ShouldRetry() ) goto retry;
                r.Terminate();
            }
            return r;
        }

        /// <summary>
        /// Attempts to deserialize the content of a <see cref="RewindableStream"/> (that must be <see cref="RewindableStream.IsValid"/>) 
        /// bound to a <see cref="BinaryDeserializerContext"/> that can be reused once done.
        /// </summary>
        /// <param name="s">The rewindable stream.</param>
        /// <param name="context">The context to use.</param>
        /// <param name="deserializer">The deserializer function.</param>
        /// <returns>A result with a true <see cref="Result.IsValid"/> and a non default <see cref="Result{T}.GetResult"/> or that has captured errors.</returns>
        public static Result<T> Deserialize<T>( RewindableStream s, BinaryDeserializerContext context, Func<IBinaryDeserializer, T> deserializer )
        {
            var r = new Result<T>( s, context );
            if( r.Deserializer != null )
            {
                retry:
                try
                {
                    r.SetResult( deserializer( r.Deserializer ) );
                }
                catch( Exception ex )
                {
                    r.SetException( System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture( ex ) );
                }
                if( r.ShouldRetry() ) goto retry;
                r.Terminate();
            }
            return r;
        }

        /// <summary>
        /// Helper that calls <see cref="RewindableStream.FromStream(Stream)"/> and <see cref="Deserialize(RewindableStream, BinaryDeserializerContext, Action{IBinaryDeserializer})"/>.
        /// </summary>
        /// <param name="s">Opened stream to use.</param>
        /// <param name="context">The context to use.</param>
        /// <param name="deserializer">The deserializer action.</param>
        /// <returns>A result with a true <see cref="Result.IsValid"/> or that has captured errors.</returns>
        public static Result Deserialize( Stream s, BinaryDeserializerContext context, Action<IBinaryDeserializer> deserializer )
        {
            return Deserialize( RewindableStream.FromStream( s ), context, deserializer );
        }

        /// <summary>
        /// Helper that calls <see cref="RewindableStream.FromFactory(Func{Stream})"/> and <see cref="Deserialize(RewindableStream, BinaryDeserializerContext, Action{IBinaryDeserializer})"/>.
        /// </summary>
        /// <param name="opener">See <see cref="RewindableStream.FromFactory(Func{Stream})"/>.</param>
        /// <param name="context">The context to use.</param>
        /// <param name="deserializer">The deserializer action.</param>
        /// <returns>A result with a true <see cref="Result.IsValid"/> or that has captured errors.</returns>
        public static Result Deserialize( Func<Stream> opener, BinaryDeserializerContext context, Action<IBinaryDeserializer> deserializer )
        {
            return Deserialize( RewindableStream.FromFactory( opener ), context, deserializer );
        }

        /// <summary>
        /// Helper that calls <see cref="RewindableStream.FromStream(Stream)"/> and <see cref="Deserialize{T}(RewindableStream, BinaryDeserializerContext, Func{IBinaryDeserializer, T})"/>.
        /// </summary>
        /// <param name="s">Opened stream to use.</param>
        /// <param name="context">The context to use.</param>
        /// <param name="deserializer">The deserializer function.</param>
        /// <returns>A result with a true <see cref="Result.IsValid"/> or that has captured errors.</returns>
        public static Result<T> Deserialize<T>( Stream s, BinaryDeserializerContext context, Func<IBinaryDeserializer, T> deserializer )
        {
            return Deserialize( RewindableStream.FromStream( s ), context, deserializer );
        }

        /// <summary>
        /// Helper that calls <see cref="RewindableStream.FromFactory(Func{Stream})"/> and <see cref="Deserialize{T}(RewindableStream, BinaryDeserializerContext, Func{IBinaryDeserializer, T})"/>.
        /// </summary>
        /// <param name="opener">See <see cref="RewindableStream.FromFactory(Func{Stream})"/>.</param>
        /// <param name="context">The context to use.</param>
        /// <param name="deserializer">The deserializer function.</param>
        /// <returns>A result with a true <see cref="Result.IsValid"/> and a non default <see cref="Result{T}.GetResult"/>or  that has captured errors.</returns>
        public static Result<T> Deserialize<T>( Func<Stream> opener, BinaryDeserializerContext context, Func<IBinaryDeserializer, T> deserializer )
        {
            return Deserialize( RewindableStream.FromFactory( opener ), context, deserializer );
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
