using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    public class SlicedSerializableRegistry : ISerializerResolver
    {
        readonly ConcurrentDictionary<Type, ISerializationDriver?> _cache;
        readonly ISerializerResolver _resolver;

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly SlicedSerializableRegistry Default = new SlicedSerializableRegistry( BinarySerializer.DefaultSharedContext );

#if NET6_0_OR_GREATER
        [ModuleInitializer]
        internal static void AutoSharedRegister()
        {
            BinarySerializer.DefaultSharedContext.Register( Default, false );
        }
#endif

        /// <summary>
        /// Initializes a new registry.
        /// </summary>
        /// <param name="resolver">The root resolver to use.</param>
        public SlicedSerializableRegistry( ISerializerResolver resolver )
        {
            _cache = new ConcurrentDictionary<Type, ISerializationDriver?>();
            _resolver = resolver;
        }

        public ISerializationDriver? TryFindDriver( Type t )
        {
            if( !typeof(ICKSlicedSerializable).IsAssignableFrom( t ) ) return null;
            return null;

        }
    }
}
