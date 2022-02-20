using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Factory methods for <see cref="IBinarySerializer"/> and default <see cref="SharedBinarySerializerContext"/> 
    /// and <see cref="SharedSerializerKnownObject"/>.
    /// </summary>
    public static class BinarySerializer
    {
        /// <summary>
        /// Gets the current serializer version.
        /// This version is always written and is available for information while 
        /// deserializing on <see cref="IBinaryDeserializer.SerializerVersion"/> instances.
        /// </summary>
        public const int SerializerVersion = 10;

        /// <summary>
        /// Gets the default thread safe static context initialized with the <see cref="BasicTypeSerializerRegistry.Instance"/>,
        /// <see cref="SimpleBinarySerializableFactory.Instance"/> and a <see cref="StandardGenericSerializerFactory"/>
        /// deserializer resolvers and <see cref="SharedSerializerKnownObject.Default"/>.
        /// </summary>
        public static readonly SharedBinarySerializerContext DefaultSharedContext;

        static BinarySerializer()
        {
            DefaultSharedContext = new SharedBinarySerializerContext();
#if NETCOREAPP3_1
            // Works around the lack of [ModuleInitializer] by an awful trick.
            Type? tSliced = Type.GetType( "CK.BinarySerialization.SlicedSerializerFactory, CK.BinarySerialization.Sliced", throwOnError: false );
            if( tSliced != null )
            {
                var sliced = (ISerializerResolver)Activator.CreateInstance( tSliced, DefaultSharedContext )!;
                DefaultSharedContext.Register( sliced, false );
            }
#endif
        }


        /// <summary>
        /// Creates a new disposable serializer bound to a <see cref="BinarySerializerContext"/>
        /// that can be reused when the serializer is disposed.
        /// </summary>
        /// <param name="s">The target stream.</param>
        /// <param name="leaveOpen">True to leave the stream opened, false to close it when the serializer is disposed.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A disposable serializer.</returns>
        public static IDisposableBinarySerializer Create( Stream s,
                                                          bool leaveOpen,
                                                          BinarySerializerContext context )
        {
            var writer = new CKBinaryWriter( s, Encoding.UTF8, leaveOpen );
            writer.WriteSmallInt32( SerializerVersion );
            return new BinarySerializerImpl( writer, leaveOpen, context );
        }

        internal class CheckedWriteStream : Stream
        {
            readonly byte[] _already;
            int _position;

            public CheckedWriteStream( byte[] already )
            {
                _already = already;
            }

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotSupportedException();

            public override long Position { get => _position; set => throw new NotSupportedException(); }

            public override void Flush()
            {
            }

            public override int Read( byte[] buffer, int offset, int count )
            {
                throw new NotSupportedException();
            }

            public override long Seek( long offset, SeekOrigin origin )
            {
                throw new NotSupportedException();
            }

            public override void SetLength( long value )
            {
                throw new NotSupportedException();
            }

            public override void Write( byte[] buffer, int offset, int count )
            {
                for( int i = offset; i < count; ++i )
                {
                    if( _position == _already.Length )
                    {
                        throw new CKException( $"Rewrite is longer than first write: length = {_position}." );
                    }
                    var actual = _already[_position++];
                    if( buffer[i] != actual )
                    {
                        throw new CKException( $"Write stream differ @{_position - 1}. Expected byte '{actual}', got '{buffer[i]}'." );
                    }
                }
            }
        }

        /// <summary>
        /// Magic yet simple helper to check the serialization implementation: the object (and potentially the whole graph behind)
        /// is serialized then deserialized and the result of the deserialization is then serialized again but in a special stream
        /// that throws a <see cref="CKException"/> as soon as a byte differ.
        /// </summary>
        /// <param name="o">The object to check.</param>
        /// <param name="serializerContext">Optional serializer context.</param>
        /// <param name="deserializerContext">Optional deserializer context.</param>
        /// <param name="throwOnFailure">False to log silently fail and return false.</param>
        /// <returns>True on success, false on error (if <paramref name="throwOnFailure"/> is false).</returns>
        public static bool IdempotenceCheck( object o, 
                                             BinarySerializerContext? serializerContext = null,
                                             BinaryDeserializerContext? deserializerContext = null, 
                                             bool throwOnFailure = true )
        {
            try
            {
                if( serializerContext == null ) serializerContext = new BinarySerializerContext();
                using( var s = new MemoryStream() )
                {
                    using( var w = Create( s, true, serializerContext ) )
                    {
                        w.WriteAny( o );
                    }
                    var originalBytes = s.ToArray();
                    s.Position = 0;
                    using( var r = BinaryDeserializer.Create( s, true, deserializerContext ?? new BinaryDeserializerContext() ) )
                    {
                        var o2 = r.ReadAny();
                        using( var checker = new CheckedWriteStream( originalBytes ) )
                        using( var w2 = Create( s, true, serializerContext ) )
                        {
                            w2.WriteObject( o2 );
                        }
                    }

                }
                return true;
            }
            catch
            {
                if( throwOnFailure ) throw;
            }
            return false;
        }

    }
}
