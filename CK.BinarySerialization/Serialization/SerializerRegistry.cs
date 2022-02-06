using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    public class SerializerRegistry : ISerializerResolver
    {
        readonly ConcurrentDictionary<Type, ITypeSerializationDriver?> _types;
        static readonly KeyValuePair<Type, ITypeSerializationDriver>[] _basic = new []
        {
            KeyValuePair.Create( typeof( bool ), Serialization.DBool.Instance ),
            KeyValuePair.Create( typeof( int ), Serialization.DInt32.Instance ),
            KeyValuePair.Create( typeof( uint ), Serialization.DUInt32.Instance ),
            KeyValuePair.Create( typeof( sbyte ), Serialization.DInt8.Instance ),
            KeyValuePair.Create( typeof( byte ), Serialization.DUInt8.Instance ),
            KeyValuePair.Create( typeof( short ), Serialization.DInt16.Instance ),
            KeyValuePair.Create( typeof( ushort ), Serialization.DUInt16.Instance ),
            KeyValuePair.Create( typeof( long ), Serialization.DInt16.Instance ),
            KeyValuePair.Create( typeof( ulong ), Serialization.DUInt32.Instance ),
        };

        public SerializerRegistry()
        {
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _types = new ConcurrentDictionary<Type, ITypeSerializationDriver?>( _basic );
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        }

        /// <inheritdoc />
        public ITypeSerializationDriver<T>? TryFindDriver<T>() where T : notnull
        {
            return (ITypeSerializationDriver<T>?)_types.GetOrAdd( typeof( T ), TryCreate );
        }

        /// <inheritdoc />
        public ITypeSerializationDriver? TryFindDriver( Type t )
        {
            return _types.GetOrAdd( t, TryCreate );
        }

        ITypeSerializationDriver? TryCreate( Type t )
        {
            return null;
        }

    }
}
