using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.Samples
{
    [SerializationVersion(0)]
    public sealed class Garage : ICKSlicedSerializable
    {
        List<Employee> _employees;

        public Garage()
        {
            _employees = new List<Employee>();
        }

        public IReadOnlyList<Employee> Employees => _employees;

        internal void OnNewEmployee( Employee e )
        {
            _employees.Add( e );
        }

        #region Serialization
        public Garage( IBinaryDeserializer d, ITypeReadInfo info )
        {
            _employees = d.ReadObject<List<Employee>>();
        }

        public static void Write( IBinarySerializer s, in Garage o )
        {
            s.WriteObject( o._employees );
        }

        #endregion
    }
}
