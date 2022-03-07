using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.Samples
{
    [SerializationVersion(0)]
    public sealed class Customer : Person
    {
        public Customer( Town town )
            : base( town )
        {
        }

        public Employee? Contact { get; set; }

        #region Serialization

        public Customer( IBinaryDeserializer d, ITypeReadInfo info )
            : base( Sliced.Instance )
        {
            Contact = d.ReadNullableObject<Employee>();
        }

        public static void Write( IBinarySerializer s, in Customer o )
        {
            s.WriteNullableObject( o.Contact );
        }

        #endregion
    }
}
