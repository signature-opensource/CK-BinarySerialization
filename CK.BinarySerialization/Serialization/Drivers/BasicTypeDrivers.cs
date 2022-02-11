using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DString : ReferenceTypeSerializer<string>
    {
        public override string DriverName => "string";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in string o ) => w.Writer.Write( o );
    }

    sealed class DBool : ValueTypeSerializer<bool>
    {
        public override string DriverName => "bool";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in bool o ) => w.Writer.Write( o );
    }

    sealed class DInt32 : ValueTypeSerializer<int>
    {
        public override string DriverName => "int";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in int o ) => w.Writer.Write( o );
    }

    sealed class DUInt32 : ValueTypeSerializer<uint>
    {
        public override string DriverName => "uint";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in uint o ) => w.Writer.Write( o );
    }

    sealed class DInt8 : ValueTypeSerializer<sbyte>
    {
        public override string DriverName => "sbyte";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in sbyte o ) => w.Writer.Write( o );
    }

    sealed class DUInt8 : ValueTypeSerializer<byte>
    {
        public override string DriverName => "byte";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in byte o ) => w.Writer.Write( o );
    }

    sealed class DInt16 : ValueTypeSerializer<short>
    {
        public override string DriverName => "short";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in short o ) => w.Writer.Write( o );
    }

    sealed class DUInt16 : ValueTypeSerializer<ushort>
    {
        public override string DriverName => "ushort";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in ushort o ) => w.Writer.Write( o );
    }

    sealed class DInt64 : ValueTypeSerializer<long>
    {
        public override string DriverName => "long";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in long o ) => w.Writer.Write( o );
    }

    sealed class DUInt64 : ValueTypeSerializer<ulong>
    {
        public override string DriverName => "ulong";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in ulong o ) => w.Writer.Write( o );
    }

    sealed class DSingle : ValueTypeSerializer<float>
    {
        public override string DriverName => "float";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in float o ) => w.Writer.Write( o );
    }

    sealed class DDouble : ValueTypeSerializer<double>
    {
        public override string DriverName => "double";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in double o ) => w.Writer.Write( o );
    }

    sealed class DChar : ValueTypeSerializer<char>
    {
        public override string DriverName => "char";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in char o ) => w.Writer.Write( o );
    }

    sealed class DDateTime : ValueTypeSerializer<DateTime>
    {
        public override string DriverName => "DateTime";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in DateTime o ) => w.Writer.Write( o );
    }

    sealed class DDateTimeOffset : ValueTypeSerializer<DateTimeOffset>
    {
        public override string DriverName => "DateTimeOffset";

        public override int SerializationVersion => -1;

        protected internal override void Write( IBinarySerializer w, in DateTimeOffset o ) => w.Writer.Write( o );
    }
}
