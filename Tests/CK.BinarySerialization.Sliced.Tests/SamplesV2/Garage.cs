using CK.Core;
using System.Collections.Generic;

namespace CK.BinarySerialization.Tests.SamplesV2;

[SerializationVersion( 0 )]
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

    public GarageQuality Quality { get; set; }

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
        Quality = d.ReadValue<GarageQuality>();
    }

    public static void Write( IBinarySerializer s, in Garage o )
    {
        s.DebugWriteSentinel();
        s.WriteObject( o._employees );
        s.WriteObject( o.Town );
        s.WriteValue( o.Quality );
    }

    #endregion
}
