using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
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
            return new BinaryDeserializerImpl( v, reader, leaveOpen, context );
        }

    }
}
