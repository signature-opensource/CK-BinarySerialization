using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DBool : ITypeSerializationDriver<bool>
    {
        public static readonly ITypeSerializationDriver Instance = new DBool();

        public string DriverName => "bool";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in bool o ) => w.Writer.Write( o );
    }

    sealed class DInt32 : ITypeSerializationDriver<int>
    {
        public static ITypeSerializationDriver Instance = new DInt32();

        public string DriverName => "int";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in int o ) => w.Writer.Write( o );
    }

    sealed class DUInt32 : ITypeSerializationDriver<uint>
    {
        public static ITypeSerializationDriver Instance = new DUInt32();

        public string DriverName => "uint";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in uint o ) => w.Writer.Write( o );
    }

    sealed class DInt8 : ITypeSerializationDriver<sbyte>
    {
        public static ITypeSerializationDriver Instance = new DInt8();

        public string DriverName => "sbyte";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in sbyte o ) => w.Writer.Write( o );
    }

    sealed class DUInt8 : ITypeSerializationDriver<byte>
    {
        public static ITypeSerializationDriver Instance = new DUInt8();

        public string DriverName => "byte";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in byte o ) => w.Writer.Write( o );
    }

    sealed class DInt16 : ITypeSerializationDriver<short>
    {
        public static ITypeSerializationDriver Instance = new DInt16();

        public string DriverName => "short";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in short o ) => w.Writer.Write( o );
    }

    sealed class DUInt16 : ITypeSerializationDriver<ushort>
    {
        public static ITypeSerializationDriver Instance = new DUInt16();

        public string DriverName => "ushort";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in ushort o ) => w.Writer.Write( o );
    }

    sealed class DInt64 : ITypeSerializationDriver<long>
    {
        public static ITypeSerializationDriver Instance = new DInt64();

        public string DriverName => "long";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in long o ) => w.Writer.Write( o );
    }

    sealed class DUInt64 : ITypeSerializationDriver<ulong>
    {
        public static ITypeSerializationDriver Instance = new DUInt64();

        public string DriverName => "ulong";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in ulong o ) => w.Writer.Write( o );
    }

    sealed class DString : ITypeSerializationDriver<string>
    {
        public static ITypeSerializationDriver Instance = new DString();

        public string DriverName => "string";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in string o ) => w.Writer.Write( o );
    }
}
