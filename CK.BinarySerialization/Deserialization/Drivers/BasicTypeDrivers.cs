using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    sealed class DString : SimpleReferenceTypeDeserializer<string>
    {
        public DString() : base( true ) { }

        protected override string ReadInstance( ICKBinaryReader r, ITypeReadInfo readInfo ) => r.ReadString();
    }

    sealed class DByteArray : SimpleReferenceTypeDeserializer<byte[]>
    {
        public DByteArray() : base( true ) { }
        
        protected override byte[] ReadInstance( ICKBinaryReader r, ITypeReadInfo readInfo ) => r.ReadBytes( r.ReadNonNegativeSmallInt32() );
    }

    sealed class DBool : ValueTypeDeserializer<bool>
    {
        protected override bool ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadBoolean();
    }

    sealed class DInt32 : ValueTypeDeserializer<int>
    {
        public static IDeserializationDriver Instance = new DInt32();

        protected override int ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadInt32();
    }

    sealed class DUInt32 : ValueTypeDeserializer<uint>
    {
        protected override uint ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadUInt32();
    }

    sealed class DInt8 : ValueTypeDeserializer<sbyte>
    {
        protected override sbyte ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadSByte();
    }

    sealed class DUInt8 : ValueTypeDeserializer<byte>
    {
        protected override byte ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadByte();
    }

    sealed class DInt16 : ValueTypeDeserializer<short>
    {
        protected override short ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadInt16();
    }

    sealed class DUInt16 : ValueTypeDeserializer<ushort>
    {
        protected override ushort ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadUInt16();
    }

    sealed class DInt64 : ValueTypeDeserializer<long>
    {
        protected override long ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadInt64();
    }

    sealed class DUInt64 : ValueTypeDeserializer<ulong>
    {
        protected override ulong ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadUInt64();
    }

    sealed class DSingle : ValueTypeDeserializer<float>
    {
        protected override float ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadSingle();
    }

    sealed class DDouble : ValueTypeDeserializer<double>
    {
        protected override double ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadDouble();
    }

    sealed class DChar : ValueTypeDeserializer<char>
    {
        protected override char ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadChar();
    }

    sealed class DDateTime : ValueTypeDeserializer<DateTime>
    {
        protected override DateTime ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadDateTime();
    }

    sealed class DDateTimeOffset : ValueTypeDeserializer<DateTimeOffset>
    {
        protected override DateTimeOffset ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadDateTimeOffset();
    }

    sealed class DTimeSpan : ValueTypeDeserializer<TimeSpan>
    {
        protected override TimeSpan ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadTimeSpan();
    }

    sealed class DGuid : ValueTypeDeserializer<Guid>
    {
        protected override Guid ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadGuid();
    }

    sealed class DDecimal : ValueTypeDeserializer<decimal>
    {
        protected override decimal ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => d.Reader.ReadDecimal();
    }
}
