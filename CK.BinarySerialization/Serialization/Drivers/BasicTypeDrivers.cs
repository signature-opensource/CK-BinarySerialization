using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DString : INonNullableSerializationDriver<string>
    {
        public string DriverName => "string";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in string o ) => w.Writer.Write( o );
    }

    sealed class DBool : INonNullableSerializationDriver<bool>
    {
        public string DriverName => "bool";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in bool o ) => w.Writer.Write( o );
    }

    sealed class DInt32 : INonNullableSerializationDriver<int>
    {
        public string DriverName => "int";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in int o ) => w.Writer.Write( o );
    }

    sealed class DUInt32 : INonNullableSerializationDriver<uint>
    {
        public string DriverName => "uint";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in uint o ) => w.Writer.Write( o );
    }

    sealed class DInt8 : INonNullableSerializationDriver<sbyte>
    {
        public string DriverName => "sbyte";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in sbyte o ) => w.Writer.Write( o );
    }

    sealed class DUInt8 : INonNullableSerializationDriver<byte>
    {
        public string DriverName => "byte";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in byte o ) => w.Writer.Write( o );
    }

    sealed class DInt16 : INonNullableSerializationDriver<short>
    {
        public string DriverName => "short";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in short o ) => w.Writer.Write( o );
    }

    sealed class DUInt16 : INonNullableSerializationDriver<ushort>
    {
        public string DriverName => "ushort";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in ushort o ) => w.Writer.Write( o );
    }

    sealed class DInt64 : INonNullableSerializationDriver<long>
    {
        public string DriverName => "long";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in long o ) => w.Writer.Write( o );
    }

    sealed class DUInt64 : INonNullableSerializationDriver<ulong>
    {
        public string DriverName => "ulong";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in ulong o ) => w.Writer.Write( o );
    }

    sealed class DSingle : INonNullableSerializationDriver<float>
    {
        public string DriverName => "float";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in float o ) => w.Writer.Write( o );
    }

    sealed class DDouble : INonNullableSerializationDriver<double>
    {
        public string DriverName => "double";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in double o ) => w.Writer.Write( o );
    }

    sealed class DChar : INonNullableSerializationDriver<char>
    {
        public string DriverName => "char";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in char o ) => w.Writer.Write( o );
    }

    sealed class DDateTime : INonNullableSerializationDriver<DateTime>
    {
        public string DriverName => "DateTime";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in DateTime o ) => w.Writer.Write( o );
    }

    sealed class DDateTimeOffset : INonNullableSerializationDriver<DateTimeOffset>
    {
        public string DriverName => "DateTimeOffset";

        public int SerializationVersion => -1;

        public void WriteData( IBinarySerializer w, in DateTimeOffset o ) => w.Writer.Write( o );
    }
}
