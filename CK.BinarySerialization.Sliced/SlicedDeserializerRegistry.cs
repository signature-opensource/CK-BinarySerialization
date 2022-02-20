using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Registry for "Sliced" deserializers. 
    /// <para>
    /// Since the synthesized drivers only depends on the local type and don't directly need any other resolvers, 
    /// a singleton cache is fine and it uses the <see cref="SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers"/>.
    /// </para>
    /// </summary>
    public class SlicedDeserializerRegistry : IDeserializerResolver
    {
        /// <summary>
        /// Gets the registry.
        /// </summary>
        public static readonly SlicedDeserializerRegistry Instance = new SlicedDeserializerRegistry();

        SlicedDeserializerRegistry() { }

#if NET6_0_OR_GREATER
        [ModuleInitializer]
        internal static void AutoSharedRegister()
        {
            BinaryDeserializer.DefaultSharedContext.Register( Instance, false );
        }
#endif

        /// <inheritdoc />
        public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
        {
            // Only the "SimpleBinarySerializable" or "SealedVersionBinarySerializable" drivers are handled here.
            // Reading from SimpleBinarySerializable is dangerous but since SimpleBinaryDeserializableRegistry didn't resolve it,
            // we can accept to try...
            bool isSimple = info.DriverName == "SimpleBinarySerializable";
            // Same for SealedVersionBinarySerializable (at least we will have a version).
            bool isSealed = !isSimple && info.DriverName == "SealedVersionBinarySerializable";
            // Serialized as a Sliced.
            bool isSliced = !isSimple && !isSealed && info.DriverName == "Sliced";
            if( !isSimple && !isSealed && !isSliced ) return null;

            switch( info.DriverName )
            {
                case "Sliced": return null;
            }
            return null;
        }
    }
}
