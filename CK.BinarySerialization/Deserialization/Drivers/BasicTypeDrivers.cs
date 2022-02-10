using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    sealed class DBool : Deserializer<bool>
    {
        protected override bool ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadBoolean();
    }

    sealed class DInt32 : Deserializer<int>
    {
        public static IDeserializationDriver Instance = new DInt32();

        protected override int ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadInt32();
    }

    sealed class DUInt32 : Deserializer<uint>
    {
        protected override uint ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadUInt32();
    }

    sealed class DInt8 : Deserializer<sbyte>
    {
        protected override sbyte ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadSByte();
    }

    sealed class DUInt8 : Deserializer<byte>
    {
        protected override byte ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadByte();
    }

    sealed class DInt16 : Deserializer<short>
    {
        protected override short ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadInt16();
    }

    sealed class DUInt16 : Deserializer<ushort>
    {
        protected override ushort ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadUInt16();
    }

    sealed class DInt64 : Deserializer<long>
    {
        protected override long ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadInt64();
    }

    sealed class DUInt64 : Deserializer<ulong>
    {
        protected override ulong ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadUInt64();
    }

    sealed class DString : Deserializer<string>
    {
        protected override string ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadString();
    }

    sealed class DSingle : Deserializer<float>
    {
        protected override float ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadSingle();
    }

    sealed class DDouble : Deserializer<double>
    {
        protected override double ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadDouble();
    }

    sealed class DChar : Deserializer<char>
    {
        protected override char ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadChar();
    }

    sealed class DDateTime : Deserializer<DateTime>
    {
        protected override DateTime ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadDateTime();
    }

    sealed class DDateTimeOffset : Deserializer<DateTimeOffset>
    {
        protected override DateTimeOffset ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadDateTimeOffset();
    }
}
