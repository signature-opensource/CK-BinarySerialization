using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Factory methods for <see cref="IBinarySerializer"/> and default <see cref="SerializerRegistry"/> 
    /// and <see cref="SerializerKnownObject"/>.
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
        /// Gets the default thread safe static registry of <see cref="ISerializerResolver"/>.
        /// </summary>
        public static readonly SerializerRegistry DefaultResolver;

        /// <summary>
        /// Gets a shared thread safe instance of <see cref="ISserializerKnownObject"/>.
        /// <para>
        /// If dynamic registration is used (during serialization), it's more efficient to first
        /// call <see cref="ISerializerKnownObject.GetKnownObjectKey(object)"/> with the object and if null is returned
        /// then call <see cref="ISerializerKnownObject.RegisterKnownObject(object, string)"/>.
        /// </para>
        /// <para>
        /// It is recommended to register the known objects once for all in static constructors whenever possible.
        /// </para>
        /// </summary>
        public static readonly ISerializerKnownObject DefaultKnownObjects;

        static BinarySerializer()
        {
            DefaultKnownObjects = new SerializerKnownObject();
            DefaultResolver = new SerializerRegistry( true );
            DefaultResolver.Register( StandardGenericSerializerRegistry.Default );
        }

        public static IBinarySerializer Create( Stream s,
                                                bool leaveOpen,
                                                BinarySerializerContext context )
        {
            var writer = new CKBinaryWriter( s, Encoding.UTF8, leaveOpen );
            writer.WriteSmallInt32( SerializerVersion );
            return new BinarySerializerImpl( writer, leaveOpen, context );
        }
        
        public static IBinarySerializer Create( Stream s,
                                                bool leaveOpen,
                                                ISerializerResolver? resolver = null,
                                                ISerializerKnownObject? knownObjects = null )
        {
            var w = new CKBinaryWriter( s, Encoding.UTF8, leaveOpen );
            return Create( w, false, resolver, knownObjects );
        }
        
        public static IBinarySerializer Create( ICKBinaryWriter writer,
                                                bool leaveOpen,
                                                ISerializerResolver? resolver = null,
                                                ISerializerKnownObject? knownObjects = null )
        {
            writer.WriteSmallInt32( SerializerVersion );
            return new BinarySerializerImpl( writer, leaveOpen, resolver ?? DefaultResolver, knownObjects ?? DefaultKnownObjects );
        }
    }
}
