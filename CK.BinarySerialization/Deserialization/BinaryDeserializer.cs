using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Factory methods for <see cref="IBinaryDeserializer"/> and default <see cref="DeserializerRegistry"/>
    /// and <see cref="DeserializerKnownObject"/>.
    /// </summary>
    public static class BinaryDeserializer
    {
        /// <summary>
        /// Gets the default thread safe static registry of <see cref="IDeserializerResolver"/>.
        /// </summary>
        public static readonly DeserializerRegistry DefaultResolver;

        /// <summary>
        /// Gets a shared thread safe instance of <see cref="IDeserializerKnownObject"/>.
        /// <para>
        /// If dynamic registration is used (during deserialization), it's more efficient to first
        /// call <see cref="IDeserializerKnownObject.GetKnownObject(string)"/> with the key and if null is returned
        /// then call <see cref="IDeserializerKnownObject.RegisterKnownKey(string, object)"/>.
        /// </para>
        /// <para>
        /// It is recommended to register the known objects once for all in static constructors whenever possible.
        /// </para>
        /// </summary>
        public static readonly DeserializerKnownObject DefaultKnownObjects;

        static BinaryDeserializer()
        {
            DefaultKnownObjects = new DeserializerKnownObject();
            DefaultResolver = new DeserializerRegistry( true );
            DefaultResolver.Register( StandardGenericDeserializerRegistry.Default, false );
        }

        public static IBinaryDeserializer Create( Stream s,
                                                  bool leaveOpen,
                                                  IDeserializerResolver? resolver = null,
                                                  IDeserializerKnownObject? knownObjects = null,
                                                  IServiceProvider? services = null )
        {
            var r = new CKBinaryReader( s, Encoding.UTF8, leaveOpen );
            return Create( r, false, resolver, knownObjects, services );
        }

        public static IBinaryDeserializer Create( ICKBinaryReader reader, 
                                                  bool leaveOpen, 
                                                  IDeserializerResolver? resolver = null,
                                                  IDeserializerKnownObject? knownObjects = null,
                                                  IServiceProvider? services = null )
        {
            var v = reader.ReadSmallInt32();
            if( v < 10 || v > BinarySerializer.SerializerVersion )
            {
                throw new InvalidDataException( $"Invalid deserializer version: {v}. Minimal is 10 and current is {BinarySerializer.SerializerVersion}." );
            }
            return new BinaryDeserializerImpl( v, reader, leaveOpen, resolver ?? DefaultResolver, knownObjects ?? DefaultKnownObjects, services );
        }
    }
}
