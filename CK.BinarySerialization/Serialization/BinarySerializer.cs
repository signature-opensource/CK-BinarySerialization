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
#if NETCOREAPP3_1
            // Works around the lack of [ModuleInitializer] by an awful trick.
            Type? tSliced = Type.GetType( "CK.BinarySerialization.SlicedSerializableRegistry, CK.BinarySerialization.Sliced", throwOnError: false );
            if( tSliced != null )
            {
                var sliced = (ISerializerResolver)tSliced.GetField( "Default", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static )!.GetValue( null )!;
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
        
    }
}
