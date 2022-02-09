﻿using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CK.BinarySerialization
{
    public static class BinaryDeserializer
    {
        public static readonly DeserializerRegistry DefaultResolver = new DeserializerRegistry();

        public static IBinaryDeserializer Create( Stream s,
                                                  bool leaveOpen,
                                                  IDeserializerResolver? resolver = null,
                                                  IServiceProvider? services = null )
        {
            var r = new CKBinaryReader( s, Encoding.UTF8, leaveOpen );
            return Create( r, false, resolver, services );
        }

        public static IBinaryDeserializer Create( ICKBinaryReader reader, 
                                                  bool leaveOpen, 
                                                  IDeserializerResolver? resolver = null, 
                                                  IServiceProvider? services = null )
        {
            var v = reader.ReadSmallInt32();
            if( v < 10 || v > BinarySerializer.SerializerVersion )
            {
                throw new InvalidDataException( $"Invalid deserializer version: {v}. Minimal is 10 and current is {BinarySerializer.SerializerVersion}." );
            }
            return new BinaryDeserializerImpl( v, reader, leaveOpen, resolver ?? DefaultResolver, services );
        }
    }
}
