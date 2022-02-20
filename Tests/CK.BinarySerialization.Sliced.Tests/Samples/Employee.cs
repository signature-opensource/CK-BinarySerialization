using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.Samples
{
    [SerializationVersion(0)]
    public class Employee : Person
    {
        public Employee()
        {

        }

        public int EmployeeNumber { get; set; }

        #region Serialization
        protected Employee( Sliced _ ) : base( _ ) { }

        public Employee( IBinaryDeserializer d, ITypeReadInfo info )
            : base( Sliced.Instance )
        {
            EmployeeNumber = d.Reader.ReadInt32();
        }

        public static void Write( IBinarySerializer s, in Employee o )
        {
            s.Writer.Write( o.EmployeeNumber );
        }

        #endregion
    }
}
