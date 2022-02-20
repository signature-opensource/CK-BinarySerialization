using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// This singleton type is a marker used by the "empty reversed deserializer constructor".
    /// </summary>
    public class Sliced
    {
        public static readonly Sliced Instance = new Sliced();

        Sliced() {}
    }
}
