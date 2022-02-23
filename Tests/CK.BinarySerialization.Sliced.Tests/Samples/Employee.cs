using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.Samples
{
    [SerializationVersion(0)]
    public class Employee : Person
    {
        public Employee( Garage g )
        {
            Garage = g;
            g.OnNewEmployee( this );
        }

        public int EmployeeNumber { get; set; }
        
        public Garage Garage { get; }

        public Employee? BestFriend { get; set; }

        #region Serialization

#pragma warning disable CS8618
        protected Employee( Sliced _ ) : base( _ ) { }
#pragma warning restore CS8618

        public Employee( IBinaryDeserializer d, ITypeReadInfo info )
            : base( Sliced.Instance )
        {
            EmployeeNumber = d.Reader.ReadInt32();
            Garage = d.ReadObject<Garage>();
            BestFriend = d.ReadNullableObject<Employee>();
        }

        public static void Write( IBinarySerializer s, in Employee o )
        {
            s.Writer.Write( o.EmployeeNumber );
            s.WriteObject( o.Garage );
            s.WriteNullableObject( o.BestFriend );
        }

        #endregion
    }
}
