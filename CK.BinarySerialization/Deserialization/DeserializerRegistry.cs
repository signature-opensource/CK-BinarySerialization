using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    public class DeserializerRegistry : IDeserializerResolver
    {
        readonly ConcurrentDictionary<string, object> _types;
        static readonly KeyValuePair<string, object>[] _basic = new[]
        {
            KeyValuePair.Create( "bool", Deserialization.DBool.Instance ),
            KeyValuePair.Create( "int", Deserialization.DInt32.Instance ),
            KeyValuePair.Create( "uint", Deserialization.DUInt32.Instance ),
            KeyValuePair.Create( "sbyte", Deserialization.DInt8.Instance ),
            KeyValuePair.Create( "byte", Deserialization.DUInt8.Instance ),
            KeyValuePair.Create( "short", Deserialization.DInt16.Instance ),
            KeyValuePair.Create( "ushort", Deserialization.DUInt16.Instance ),
            KeyValuePair.Create( "long", Deserialization.DInt16.Instance ),
            KeyValuePair.Create( "ulong", Deserialization.DUInt32.Instance ),
        };

        public DeserializerRegistry()
        {
            _types = new ConcurrentDictionary<string, object>( _basic );
        }

        /// <inheritdoc />
        public object TryFindDriver( TypeReadInfo info )
        {
            throw new NotImplementedException();
        }
    }
}
