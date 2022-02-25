using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Serialization
{
    sealed class DAbstract : ISerializationDriver
    {
        TypedWriter<object?> _reader;

        public static DAbstract Instance = new DAbstract();

        DAbstract()
        {
            _reader = Write;
        }

        public string DriverName => "object";

        public int SerializationVersion => -1;

        public Delegate UntypedWriter => _reader;

        public Delegate TypedWriter => _reader;

        void Write( IBinarySerializer s, in object? o )
        {
            if( o == null )
            {
                s.Writer.Write( (byte)SerializationMarker.Null );
            }
            else
            {
                s.WriteObject( o );
            }
        }

        public ISerializationDriver ToNullable => this;

        public ISerializationDriver ToNonNullable => this;
    }
}
