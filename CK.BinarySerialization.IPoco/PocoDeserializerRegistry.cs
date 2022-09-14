using CK.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Registry for <see cref="IPoco"/> deserializers. 
    /// </summary>
    public sealed class PocoDeserializerRegistry : IDeserializerResolver
    {
        /// <summary>
        /// Gets the registry.
        /// </summary>
        public static readonly PocoDeserializerRegistry Instance = new PocoDeserializerRegistry();

        PocoDeserializerRegistry() { }

        sealed class PocoDeserializerDriverNominal<T> : SimpleReferenceTypeWithServicesDeserializer<T> where T : class, IPoco
        {
            protected override T ReadInstance( IServiceProvider services, ICKBinaryReader r, ITypeReadInfo readInfo )
            {
                var len = r.ReadNonNegativeSmallInt32();
                var buffer = ArrayPool<byte>.Shared.Rent( len );
                try
                {
                    r.Read( buffer, 0, len );
                    return (T)services.GetRequiredService<PocoDirectory>().JsonDeserialize( buffer.AsSpan( 0, len ) )!;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return( buffer );
                }
            }
        }

        /// <inheritdoc />
        public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
        {
            if( info.DriverName == "IPocoJson" && typeof( IPoco ).IsAssignableFrom( info.TargetType ) )
            {
                return SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers.GetOrAdd( info.TargetType, CreateCachedNominalDriver );
            }
            return null;
        }

        IDeserializationDriver CreateCachedNominalDriver( Type t )
        {
            var tV = typeof( PocoDeserializerDriverNominal<> ).MakeGenericType( t );
            return (IDeserializationDriver)Activator.CreateInstance( tV )!;
        }

    }
}
