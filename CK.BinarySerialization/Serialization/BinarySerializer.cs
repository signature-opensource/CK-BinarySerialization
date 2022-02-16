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
        /// <see cref="SimpleBinarySerializableRegistry.Instance"/> and <see cref="StandardGenericSerializerRegistry.Default"/>
        /// deserializer resolvers and <see cref="SharedSerializerKnownObject.Default"/>.
        /// </summary>
        public static readonly SharedBinarySerializerContext DefaultSharedContext;


        static BinarySerializer()
        {
            DefaultSharedContext = new SharedBinarySerializerContext( 0 );
            DefaultSharedContext.Register( StandardGenericSerializerRegistry.Default, false );
        }

        public static IBinarySerializer Create( Stream s,
                                                bool leaveOpen,
                                                BinarySerializerContext context )
        {
            var writer = new CKBinaryWriter( s, Encoding.UTF8, leaveOpen );
            writer.WriteSmallInt32( SerializerVersion );
            return new BinarySerializerImpl( writer, leaveOpen, context );
        }
        
    }
}
