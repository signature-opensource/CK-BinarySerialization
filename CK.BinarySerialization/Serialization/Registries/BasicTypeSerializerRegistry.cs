using CK.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Immutable singleton that contains default serializers for well-known types.
    /// </summary>
    public class BasicTypeSerializerRegistry : ISerializerResolver
    {
        static readonly Dictionary<Type, ITypeSerializationDriver> _byType;

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly ISerializerResolver Default = new BasicTypeSerializerRegistry();

        static BasicTypeSerializerRegistry()
        {
            _byType = new Dictionary<Type, ITypeSerializationDriver>()
            {
                { typeof( bool ), Serialization.DBool.Instance },
                { typeof( int ), Serialization.DInt32.Instance },
                { typeof( uint ), Serialization.DUInt32.Instance },
                { typeof( sbyte ), Serialization.DInt8.Instance },
                { typeof( byte ), Serialization.DUInt8.Instance },
                { typeof( short ), Serialization.DInt16.Instance },
                { typeof( ushort ), Serialization.DUInt16.Instance },
                { typeof( long ), Serialization.DInt16.Instance },
                { typeof( ulong ), Serialization.DUInt32.Instance },
                { typeof( string ), Serialization.DString.Instance },
            };
        }

        BasicTypeSerializerRegistry() { }

        /// <inheritdoc />
        public ITypeSerializationDriver<T>? TryFindDriver<T>() where T : notnull
        {
            return (ITypeSerializationDriver<T>?)_byType.GetValueOrDefault( typeof( T ) );
        }

        /// <inheritdoc />
        public ITypeSerializationDriver? TryFindDriver( Type t )
        {
            return _byType.GetValueOrDefault( t );
        }

    }
}
