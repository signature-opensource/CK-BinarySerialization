using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    sealed class DString : ReferenceTypeDeserializer<string>
    {
        protected override string ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadString();
    }

    sealed class DBool : ValueTypeDeserializer<bool>
    {
        protected override bool ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadBoolean();
    }

    sealed class DInt32 : ValueTypeDeserializer<int>
    {
        public static IDeserializationDriver Instance = new DInt32();

        protected override int ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadInt32();
    }

    sealed class DUInt32 : ValueTypeDeserializer<uint>
    {
        protected override uint ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadUInt32();
    }

    sealed class DInt8 : ValueTypeDeserializer<sbyte>
    {
        protected override sbyte ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadSByte();
    }

    sealed class DUInt8 : ValueTypeDeserializer<byte>
    {
        protected override byte ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadByte();
    }

    sealed class DInt16 : ValueTypeDeserializer<short>
    {
        protected override short ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadInt16();
    }

    sealed class DUInt16 : ValueTypeDeserializer<ushort>
    {
        protected override ushort ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadUInt16();
    }

    sealed class DInt64 : ValueTypeDeserializer<long>
    {
        protected override long ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadInt64();
    }

    sealed class DUInt64 : ValueTypeDeserializer<ulong>
    {
        protected override ulong ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadUInt64();
    }

    sealed class DSingle : ValueTypeDeserializer<float>
    {
        protected override float ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadSingle();
    }

    sealed class DDouble : ValueTypeDeserializer<double>
    {
        protected override double ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadDouble();
    }

    sealed class DChar : ValueTypeDeserializer<char>
    {
        protected override char ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadChar();
    }

    sealed class DDateTime : ValueTypeDeserializer<DateTime>
    {
        protected override DateTime ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadDateTime();
    }

    sealed class DDateTimeOffset : ValueTypeDeserializer<DateTimeOffset>
    {
        protected override DateTimeOffset ReadInstance( IBinaryDeserializer r, TypeReadInfo readInfo ) => r.Reader.ReadDateTimeOffset();
    }
}
