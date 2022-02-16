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
            DefaultSharedContext = new SharedBinaryDeserializerContext( 0 );
            DefaultSharedContext.Register( StandardGenericDeserializerRegistry.Default, false );
        }

        public static IBinaryDeserializer Create( Stream s,
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
