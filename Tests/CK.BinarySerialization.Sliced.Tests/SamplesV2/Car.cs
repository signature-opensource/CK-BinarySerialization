using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.SamplesV2;

/// <summary>
/// Since Car has no object reference, it can be a <see cref="ICKVersionedBinarySerializable"/>.
/// And we also decide to make it immutable since setters was not used.
/// And finally to use a readonly struct for it.
/// </summary>
[SerializationVersion(0)]
public readonly struct Car : ICKVersionedBinarySerializable
{
    public Car( string model, DateTime buildDate )
    {
        Model = model;
        BuildDate = buildDate;
    }

    public string Model { get; }

    public DateTime BuildDate { get; }

    Car( ICKBinaryReader r, int version )
    {
        Model = r.ReadString();
        BuildDate = r.ReadDateTime();
    }

    public void WriteData( ICKBinaryWriter w )
    {
        w.Write( Model );
        w.Write( BuildDate );
    }
}
