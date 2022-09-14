using CK.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using static CK.Core.PocoJsonSerializer;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Factory for <see cref="IPoco"/> serialization drivers.
    /// </summary>
    public sealed class PocoSerializerFactory : ISerializerResolver
    {
        /// <summary>
        /// Gets the default factory that is automatically registered in the <see cref="BinarySerializer.DefaultSharedContext"/>.
        /// </summary>
        public static readonly PocoSerializerFactory Default = new PocoSerializerFactory();

        /// <summary>
        /// Initializes a new factory.
        /// </summary>
        public PocoSerializerFactory()
        {
        }

        /// <inheritdoc />
        public ISerializationDriver? TryFindDriver( Type t )
        {
            if( !typeof(IPoco).IsAssignableFrom( t ) ) return null;
            var tR = typeof( PocoSerializableDriver<> ).MakeGenericType( t );
            return ((ISerializationDriver)Activator.CreateInstance( tR )!).ToNullable;
        }

        sealed class PocoSerializableDriver<T> : ReferenceTypeSerializer<T>, ISerializationDriverTypeRewriter where T : class, IPoco
        {
            public override string DriverName => "IPocoJson";

            public override int SerializationVersion => -1;

            public Type GetTypeToWrite( Type type )
            {
                return type;
            }

            protected override void Write( IBinarySerializer s, in T o )
            {
                var m = o.JsonSerialize();
                s.Writer.WriteNonNegativeSmallInt32( m.Length );
                s.Writer.Write( m.Span );
            }
        }

    }
}
