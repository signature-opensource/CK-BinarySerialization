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
using System.Threading;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Registry for <see cref="IPoco"/> deserializers.
    /// <para>
    /// 
    /// </para>
    /// </summary>
    public sealed class PocoDeserializerResolver : IDeserializerResolver
    {
        /// <summary>
        /// Gets the registry.
        /// </summary>
        public static readonly PocoDeserializerResolver Instance = new PocoDeserializerResolver();

        PocoDeserializerResolver() { }

        sealed class PocoDeserializerDriver<T> : SimpleReferenceTypeDeserializer<T> where T : class, IPoco
        {
            readonly IPocoFactory<T> _factory;

            public PocoDeserializerDriver( IPocoFactory<T> factory, bool isCacheable )
                : base( isCacheable )
            {
                _factory = factory;
            }

            protected override T ReadInstance( ICKBinaryReader r, ITypeReadInfo readInfo )
            {
                var len = r.ReadNonNegativeSmallInt32();
                var buffer = ArrayPool<byte>.Shared.Rent( len );
                try
                {
                    r.Read( buffer, 0, len );
                    return _factory.JsonDeserialize( buffer.AsSpan( 0, len ) )!;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return( buffer );
                }
            }
        }

        static PocoDirectory? _winner;
        static IDeserializationDriver? _winnerDriver;

        /// <inheritdoc />
        public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
        {
            if( info.DriverName == "IPocoJson" && typeof( IPoco ).IsAssignableFrom( info.ExpectedType ) )
            {
                var d = info.Context.Services.GetRequiredService<PocoDirectory>();

                var factory = d.Find( info.ExpectedType );
                if( factory == null ) return null;

                bool isWinner = Interlocked.CompareExchange( ref _winner, d, null ) == null;
                if( isWinner )
                {
                    return _winnerDriver ?? CreateCached( info.ExpectedType, factory );
                }
                var tV = typeof( PocoDeserializerDriver<> ).MakeGenericType( info.ExpectedType );
                return (IDeserializationDriver)Activator.CreateInstance( tV, factory, false )!;
            }
            return null;
        }

        static IDeserializationDriver CreateCached( Type targetType, IPocoFactory factory )
        {
            var tV = typeof( PocoDeserializerDriver<> ).MakeGenericType( targetType );
            return (IDeserializationDriver)Activator.CreateInstance( tV, factory, true )!;
        }
    }
}
