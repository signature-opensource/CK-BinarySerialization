using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.Samples
{
    /// <summary>
    /// Car has no object reference: it can be a <see cref="ICKVersionedBinarySerializable"/>.
    /// </summary>
    [SerializationVersion(0)]
    public sealed class Car : ICKSlicedSerializable
    {
        public Car( string model, DateTime buildDate )
        {
            Model = model;
            BuildDate = buildDate;
        }

        public string Model { get; set; }

        public DateTime BuildDate { get; set; }

        Car( IBinaryDeserializer d, ITypeReadInfo info )
        {
            Model = d.Reader.ReadString();
            BuildDate = d.Reader.ReadDateTime();
        }

        public static void Write( IBinarySerializer s, in Car o )
        {
            // Use only the writer since we want to transform it into a Versioned serializable.
            s.Writer.Write( o.Model );
            s.Writer.Write( o.BuildDate );
        }
    }
}
