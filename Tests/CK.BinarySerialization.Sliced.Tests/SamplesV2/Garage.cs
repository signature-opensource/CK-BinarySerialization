using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.SamplesV2
{
    [SerializationVersion(0)]
    public sealed class Garage : ICKSlicedSerializable
    {
        readonly List<Employee> _employees;

        public Garage( Town town )
        {
            Town = town;
            _employees = new List<Employee>();
            town.OnNewGarage( this );
        }

        public Town Town { get; }

        public IReadOnlyList<Employee> Employees => _employees;

        internal void OnNewEmployee( Employee e )
        {
            _employees.Add( e );
        }

        #region Serialization
        public Garage( IBinaryDeserializer d, ITypeReadInfo info )
        {
            d.DebugCheckSentinel();
            _employees = d.ReadObject<List<Employee>>();
            Town = d.ReadObject<Town>();
        }

        public static void Write( IBinarySerializer s, in Garage o )
        {
            s.DebugWriteSentinel();
            s.WriteObject( o._employees );
            s.WriteObject( o.Town );
        }

        #endregion
    }
}
