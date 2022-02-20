using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.Samples
{
    [SerializationVersion(0)]
    public sealed class Garage : ICKSlicedSerializable
    {
        public Garage()
        {
            Employees = new List<Employee>();
        }

        public List<Employee> Employees { get; }

        public Garage( IBinaryDeserializer d, ITypeReadInfo info )
        {
            Employees = d.ReadObject<List<Employee>>();
        }

        public static void Write( IBinarySerializer s, in Garage o )
        {
            s.WriteObject( o );
        }

    }
}
