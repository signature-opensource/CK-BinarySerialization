using CK.Core;

namespace CK.BinarySerialization.Tests;

sealed class SimpleSealedDerived : SimpleBase
{
    public string? Name { get; set; }

    public SimpleSealedDerived()
    {
    }

    public SimpleSealedDerived( ICKBinaryReader r )
        : base( r )
    {
        r.ReadByte(); // Version
        Name = r.ReadNullableString();
    }

    public override void Write( ICKBinaryWriter w )
    {
        base.Write( w );
        w.Write( (byte)0 ); // Version
        w.WriteNullableString( Name );
    }
}
