using System;

namespace CK.BinarySerialization.Serialization;

sealed class DString : ReferenceTypeSerializer<string>
{
    public override string DriverName => "string";

    public override int SerializationVersion => -1;

    protected internal override void Write( IBinarySerializer s, in string o ) => s.Writer.Write( o );
}

sealed class DByteArray : ReferenceTypeSerializer<byte[]>
{
    public override string DriverName => "byte[]";

    public override int SerializationVersion => -1;

    protected internal override void Write( IBinarySerializer s, in byte[] o )
    {
        s.Writer.WriteNonNegativeSmallInt32( o.Length );
        s.Writer.Write( o );
    }
}

sealed class DBool : StaticValueTypeSerializer<bool>
{
    public override string DriverName => "bool";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in bool o ) => s.Writer.Write( o );
}

sealed class DInt32 : StaticValueTypeSerializer<int>
{
    public override string DriverName => "int";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in int o ) => s.Writer.Write( o );
}

sealed class DUInt32 : StaticValueTypeSerializer<uint>
{
    public override string DriverName => "uint";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in uint o ) => s.Writer.Write( o );
}

sealed class DInt8 : StaticValueTypeSerializer<sbyte>
{
    public override string DriverName => "sbyte";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in sbyte o ) => s.Writer.Write( o );
}

sealed class DUInt8 : StaticValueTypeSerializer<byte>
{
    public override string DriverName => "byte";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in byte o ) => s.Writer.Write( o );
}

sealed class DInt16 : StaticValueTypeSerializer<short>
{
    public override string DriverName => "short";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in short o ) => s.Writer.Write( o );
}

sealed class DUInt16 : StaticValueTypeSerializer<ushort>
{
    public override string DriverName => "ushort";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in ushort o ) => s.Writer.Write( o );
}

sealed class DInt64 : StaticValueTypeSerializer<long>
{
    public override string DriverName => "long";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in long o ) => s.Writer.Write( o );
}

sealed class DUInt64 : StaticValueTypeSerializer<ulong>
{
    public override string DriverName => "ulong";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in ulong o ) => s.Writer.Write( o );
}

sealed class DSingle : StaticValueTypeSerializer<float>
{
    public override string DriverName => "float";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in float o ) => s.Writer.Write( o );
}

sealed class DDouble : StaticValueTypeSerializer<double>
{
    public override string DriverName => "double";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in double o ) => s.Writer.Write( o );
}

sealed class DChar : StaticValueTypeSerializer<char>
{
    public override string DriverName => "char";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in char o ) => s.Writer.Write( o );
}

sealed class DDateTime : StaticValueTypeSerializer<DateTime>
{
    public override string DriverName => "DateTime";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in DateTime o ) => s.Writer.Write( o );
}

sealed class DDateTimeOffset : StaticValueTypeSerializer<DateTimeOffset>
{
    public override string DriverName => "DateTimeOffset";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in DateTimeOffset o ) => s.Writer.Write( o );
}

sealed class DTimeSpan : StaticValueTypeSerializer<TimeSpan>
{
    public override string DriverName => "TimeSpan";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in TimeSpan o ) => s.Writer.Write( o );
}

sealed class DGuid : StaticValueTypeSerializer<Guid>
{
    public override string DriverName => "Guid";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in Guid o ) => s.Writer.Write( o );
}

sealed class DDecimal : StaticValueTypeSerializer<decimal>
{
    public override string DriverName => "decimal";

    public override int SerializationVersion => -1;

    public static void Write( IBinarySerializer s, in decimal o ) => s.Writer.Write( o );
}
