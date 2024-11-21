using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.SamplesV2;

[SerializationVersion( 0 )]
public sealed class Manager : Employee
{
    public Manager( Garage g )
        : base( g )
    {
    }

    public int Rank { get; set; }

    #region Serialization

    public Manager( IBinaryDeserializer d, ITypeReadInfo info )
        : base( Sliced.Instance )
    {
        d.DebugCheckSentinel();
        Rank = d.Reader.ReadInt32();
    }

    public static void Write( IBinarySerializer s, in Manager o )
    {
        s.DebugWriteSentinel();
        s.Writer.Write( o.Rank );
    }

    #endregion
}
