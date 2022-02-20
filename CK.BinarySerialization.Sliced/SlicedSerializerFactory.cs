using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CK.BinarySerialization
{
    public class SlicedSerializerFactory : ISerializerResolver
    {
        readonly SharedBinarySerializerContext _resolver;

#if NET6_0_OR_GREATER
        [ModuleInitializer]
        internal static void AutoSharedRegister()
        {
            BinarySerializer.DefaultSharedContext.Register( Default, false );
        }
#endif

        /// <summary>
        /// Initializes a new factory.
        /// </summary>
        /// <param name="resolver">The root resolver to use.</param>
        public SlicedSerializerFactory( SharedBinarySerializerContext resolver )
        {
            _resolver = resolver;
        }

        /// <inheritdoc />
        public ISerializationDriver? TryFindDriver( Type t )
        {
            if( t.ContainsGenericParameters || !typeof(ICKSlicedSerializable).IsAssignableFrom( t ) ) return null;
            return TryCreate( t );
        }

        sealed class SlicedValueTypeSerializableDriver<T> : StaticValueTypeSerializer<T> where T : struct, ICKSlicedSerializable
        {
            public override string DriverName => "Sliced";

            public SlicedValueTypeSerializableDriver( int version )
                : base( typeof( T ) )
            {
                SerializationVersion = version;
            }

            public override int SerializationVersion { get; }
        }

        ISerializationDriver? TryCreate( Type t )
        {
            var version = SerializationVersionAttribute.GetRequiredVersion( t );
            if( t.IsClass )
            {
                return null;
            }
            var tS = typeof( SlicedValueTypeSerializableDriver<> ).MakeGenericType( t );
            return (ISerializationDriver?)Activator.CreateInstance( tS, version );
        }



    }
}
