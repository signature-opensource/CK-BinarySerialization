using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    public class BasicTypeDeserializerRegistry : IDeserializerResolver
    {
        static readonly Dictionary<string, object> _byName;

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly IDeserializerResolver Default = new BasicTypeDeserializerRegistry();

        static BasicTypeDeserializerRegistry()
        {
            _byName = new Dictionary<string, object>()
            {
                { "bool", Deserialization.DBool.Instance },
                { "int", Deserialization.DInt32.Instance },
                { "uint", Deserialization.DUInt32.Instance },
                { "sbyte", Deserialization.DInt8.Instance },
                { "byte", Deserialization.DUInt8.Instance },
                { "short", Deserialization.DInt16.Instance },
                { "ushort", Deserialization.DUInt16.Instance },
                { "long", Deserialization.DInt16.Instance },
                { "ulong", Deserialization.DUInt32.Instance },
                { "string", Deserialization.DString.Instance },
            };
        }

        BasicTypeDeserializerRegistry() { }

        public object? TryFindDriver( TypeReadInfo info )
        {
            return info.DriverName != null ? _byName.GetValueOrDefault( info.DriverName ) : null;
        }
    }
}
