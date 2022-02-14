using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Internal shared concurrent dictionary of discovered and instantiated serialization and deserialization drivers
    /// for types that depends on nothing else than themselves.
    /// <para>
    /// This is currently used only by <see cref="SimpleBinarySerializableRegistry"/> and <see cref="SimpleBinaryDeserializableRegistry"/>
    /// since this is, as of today, the only ones that can do their work without any other resolvers. 
    /// </para>
    /// </summary>
    class InternalShared
    {
        static public readonly ConcurrentDictionary<Type, ISerializationDriver> Serialization;
        static public readonly ConcurrentDictionary<Type, IDeserializationDriver> Deserialization;

        static InternalShared()
        {
            Serialization = new ConcurrentDictionary<Type, ISerializationDriver>();
            Deserialization = new ConcurrentDictionary<Type, IDeserializationDriver>();
        }

        internal static bool NextInArray( int[] coords, int[] lengths )
        {
            int i = coords.Length - 1;
            while( ++coords[i] >= lengths[i] )
            {
                if( i == 0 ) return false;
                coords[i--] = 0;
            }
            return true;
        }

    }
}
