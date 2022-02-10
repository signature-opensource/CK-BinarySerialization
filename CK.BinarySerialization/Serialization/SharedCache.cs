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
    class SharedCache
    {
        static public readonly ConcurrentDictionary<Type, IUntypedSerializationDriver> Serialization;
        static public readonly ConcurrentDictionary<Type, IDeserializationDriver> Deserialization;

        static SharedCache()
        {
            Serialization = new ConcurrentDictionary<Type, IUntypedSerializationDriver>();
            Deserialization = new ConcurrentDictionary<Type, IDeserializationDriver>();
        }

    }
}
