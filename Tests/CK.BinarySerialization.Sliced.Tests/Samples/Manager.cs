﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.Samples
{
    [SerializationVersion(0)]
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
            Rank = d.Reader.ReadInt32();
        }

        public static void Write( IBinarySerializer s, in Manager o )
        {
            s.Writer.Write( o.Rank );
        }

        #endregion
    }
}