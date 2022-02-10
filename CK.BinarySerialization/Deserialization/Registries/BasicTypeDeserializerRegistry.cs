using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    public class BasicTypeDeserializerRegistry : IDeserializerResolver
    {
        static readonly Dictionary<string, IDeserializationDriver> _byName;

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly IDeserializerResolver Default = new BasicTypeDeserializerRegistry();

        static BasicTypeDeserializerRegistry()
        {
            _byName = new Dictionary<string, IDeserializationDriver>()
            {
                { "bool", new Deserialization.DBool() },
                { "int", new Deserialization.DInt32() },
                { "uint", new Deserialization.DUInt32() },
                { "sbyte", new Deserialization.DInt8() },
                { "byte", new Deserialization.DUInt8() },
                { "short", new Deserialization.DInt16() },
                { "ushort", new Deserialization.DUInt16() },
                { "long", new Deserialization.DInt64() },
                { "ulong", new Deserialization.DUInt64() },
                { "string", new Deserialization.DString() },
                { "float", new Deserialization.DSingle() },
                { "double", new Deserialization.DDouble() },
                { "char", new Deserialization.DChar() },
                { "DateTime", new Deserialization.DDateTime() },
                { "DateTimeOffset", new Deserialization.DDateTimeOffset() },
            };
        }

        BasicTypeDeserializerRegistry() { }

        public IDeserializationDriver? TryFindDriver( TypeReadInfo info )
        {
            return info.DriverName != null ? _byName.GetValueOrDefault( info.DriverName ) : null;
        }
    }
}
