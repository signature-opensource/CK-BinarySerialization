using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    sealed class DBool : IDeserializationDriver<bool>
    {
        public static object Instance = new DBool();

        public bool ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadBoolean();
    }

    sealed class DInt32 : IDeserializationDriver<int>
    {
        public static object Instance = new DInt32();

        public int ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadInt32();
    }

    sealed class DUInt32 : IDeserializationDriver<uint>
    {
        public static object Instance = new DUInt32();

        public uint ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadUInt32();
    }

    sealed class DInt8 : IDeserializationDriver<sbyte>
    {
        public static object Instance = new DInt8();

        public sbyte ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadSByte();
    }

    sealed class DUInt8 : IDeserializationDriver<byte>
    {
        public static object Instance = new DUInt8();

        public byte ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadByte();
    }

    sealed class DInt16 : IDeserializationDriver<short>
    {
        public static object Instance = new DInt16();

        public short ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadInt16();
    }

    sealed class DUInt16 : IDeserializationDriver<ushort>
    {
        public static object Instance = new DUInt16();

        public ushort ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadUInt16();
    }

    sealed class DInt64 : IDeserializationDriver<long>
    {
        public static object Instance = new DInt64();

        public long ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadInt64();
    }

    sealed class DUInt64 : IDeserializationDriver<ulong>
    {
        public static object Instance = new DUInt64();

        public ulong ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadUInt64();
    }


}
