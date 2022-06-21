using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Immutable singleton that contains default deserializers for well-known types.
    /// <para>
    /// A simple dictionary is enough since it is only read.
    /// </para>
    /// </summary>
    public sealed class BasicTypeDeserializerRegistry : IDeserializerResolver
    {
        static readonly Dictionary<string, IDeserializationDriver> _byName;

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly IDeserializerResolver Instance = new BasicTypeDeserializerRegistry();

        BasicTypeDeserializerRegistry() { }

        static BasicTypeDeserializerRegistry()
        {
            _byName = new Dictionary<string, IDeserializationDriver>()
            {
                { "string", new Deserialization.DString() },
                { "byte[]", new Deserialization.DByteArray() },
                
                { "bool", new Deserialization.DBool() },
                { "int", new Deserialization.DInt32() },
                { "uint", new Deserialization.DUInt32() },
                { "sbyte", new Deserialization.DInt8() },
                { "byte", new Deserialization.DUInt8() },
                { "short", new Deserialization.DInt16() },
                { "ushort", new Deserialization.DUInt16() },
                { "long", new Deserialization.DInt64() },
                { "ulong", new Deserialization.DUInt64() },
                { "float", new Deserialization.DSingle() },
                { "double", new Deserialization.DDouble() },
                { "char", new Deserialization.DChar() },
                { "DateTime", new Deserialization.DDateTime() },
                { "DateTimeOffset", new Deserialization.DDateTimeOffset() },
                { "TimeSpan", new Deserialization.DTimeSpan() },
                { "Guid", new Deserialization.DGuid() },
                { "decimal", new Deserialization.DDecimal() },

                { "SVersion", new Deserialization.DSVersion() },
                { "CSVersion", new Deserialization.DCSVersion() },
                { "SVersionBound", new Deserialization.DSVersionBound() },
                { "PackageQualityVector", new Deserialization.DPackageQualityVector() },
                { "PackageQualityFilter", new Deserialization.DPackageQualityFilter() },
                { "Version", new Deserialization.DVersion() },
            };
        }

        /// <summary>
        /// Simple lookup in type to deserialization drivers lookup.
        /// </summary>
        /// <param name="info">The info to resolve.</param>
        /// <returns>A deserialization driver or null.</returns>
        public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info ) => _byName.GetValueOrDefault( info.DriverName );

    }
}
