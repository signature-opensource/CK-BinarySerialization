using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CK.BinarySerialization
{
    public static class BinarySerializer
    {
        public const int SerializerVersion = 10;

        public static readonly SerializerRegistry DefaultResolver;

        static BinarySerializer()
        {
            DefaultResolver = new SerializerRegistry( true );
            DefaultResolver.Register( CollectionSerializerRegistry.Default );
        }

        public static IBinarySerializer Create( Stream s,
                                                bool leaveOpen,
                                                ISerializerResolver? resolver = null,
                                                ISerializerKnownObject? knownObjects = null,
                                                Action<IDestroyable>? destroyedTracker = null )
        {
            var w = new CKBinaryWriter( s, Encoding.UTF8, leaveOpen );
            return Create( w, false, resolver, knownObjects, destroyedTracker );
        }
        
        public static IBinarySerializer Create( ICKBinaryWriter writer,
                                                bool leaveOpen,
                                                ISerializerResolver? resolver = null,
                                                ISerializerKnownObject? knownObjects = null,
                                                Action<IDestroyable>? destroyedTracker = null )
        {
            writer.WriteSmallInt32( SerializerVersion );
            return new BinarySerializerImpl( writer, leaveOpen, resolver ?? DefaultResolver, knownObjects ?? SerializerKnownObject.Default, destroyedTracker );
        }
    }
}
