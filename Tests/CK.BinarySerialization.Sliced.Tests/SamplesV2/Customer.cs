using CK.Core;

namespace CK.BinarySerialization.Tests.SamplesV2;

[SerializationVersion( 0 )]
public sealed class Customer : Person
{
    public Customer( Town town )
        : base( town )
    {
    }

    public Employee? Contact { get; set; }

    public Car? Car { get; set; }

    #region Serialization

    public Customer( IBinaryDeserializer d, ITypeReadInfo info )
        : base( Sliced.Instance )
    {
        d.DebugCheckSentinel();
        Contact = d.ReadNullableObject<Employee>();
        d.DebugCheckSentinel();
        Car = d.ReadNullableValue<Car>();
        d.DebugCheckSentinel();
    }

    public static void Write( IBinarySerializer s, in Customer o )
    {
        s.DebugWriteSentinel();
        s.WriteNullableObject( o.Contact );
        s.DebugWriteSentinel();
        s.WriteNullableValue( o.Car );
        s.DebugWriteSentinel();
    }

    #endregion
}
