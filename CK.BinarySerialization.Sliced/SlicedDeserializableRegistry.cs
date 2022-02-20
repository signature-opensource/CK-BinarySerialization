using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CK.BinarySerialization
{
    public class SlicedDeserializableRegistry : IDeserializerResolver
    {
        readonly IDeserializerResolver _resolver;

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly SlicedDeserializableRegistry Default = new SlicedDeserializableRegistry( BinaryDeserializer.DefaultSharedContext );

#if NET6_0_OR_GREATER
        [ModuleInitializer]
        internal static void AutoSharedRegister()
        {
            BinaryDeserializer.DefaultSharedContext.Register( Default, false );
        }
#endif

        /// <summary>
        /// Initializes a new registry.
        /// </summary>
        /// <param name="resolver">The root resolver to use.</param>
        public SlicedDeserializableRegistry( IDeserializerResolver resolver )
        {
            _resolver = resolver;
        }
        
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
