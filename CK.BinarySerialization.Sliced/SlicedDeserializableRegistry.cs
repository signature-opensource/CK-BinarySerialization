using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public IDeserializationDriver? TryFindDriver( TypeReadInfo info )
        {
            throw new NotImplementedException();
        }
    }
}
