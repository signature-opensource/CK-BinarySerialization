using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.Samples
{
    [SerializationVersion(0)]
    public class Employee : Person
    {
        public Employee( Garage g )
            : base( g.Town )
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
            BestFriend = d.ReadNullableObject<Employee>();
            EmployeeNumber = d.Reader.ReadInt32();
            Garage = d.ReadObject<Garage>();
        }

        public static void Write( IBinarySerializer s, in Employee o )
        {
            // Writes the BestFriend first: this enters a recursion on the stack
            // that is handled thanks to the IDeserializationDeferredDriver.
            s.WriteNullableObject( o.BestFriend );
            s.Writer.Write( o.EmployeeNumber );
            s.WriteObject( o.Garage );
        }

        #endregion
    }
}
