using CK.Core;

namespace CK.BinarySerialization.Tests;

class SimpleBase : ICKSimpleBinarySerializable
{
    public int Power { get; set; }

    public SimpleBase()
    {
    }

    public SimpleBase( ICKBinaryReader r )
    {
        r.ReadByte(); // Version
        Power = r.ReadInt32();
    }

    public virtual void Write( ICKBinaryWriter w )
    {
        w.Write( (byte)0 ); // Version
        w.Write( Power );
    }
}
