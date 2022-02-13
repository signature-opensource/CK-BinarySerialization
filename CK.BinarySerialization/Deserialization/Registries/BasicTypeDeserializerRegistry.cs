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
        public static readonly IDeserializerResolver Instance = new BasicTypeDeserializerRegistry();

        static BasicTypeDeserializerRegistry()
        {
            _byName = new Dictionary<string, IDeserializationDriver>();
            Register( "string", new Deserialization.DString() );
            Register( "bool", new Deserialization.DBool() );
            Register( "int", new Deserialization.DInt32() );
            Register( "uint", new Deserialization.DUInt32() );
            Register( "sbyte", new Deserialization.DInt8() );
            Register( "byte", new Deserialization.DUInt8() );
            Register( "short", new Deserialization.DInt16() );
            Register( "ushort", new Deserialization.DUInt16() );
            Register( "long", new Deserialization.DInt64() );
            Register( "ulong", new Deserialization.DUInt64() );
            Register( "float", new Deserialization.DSingle() );
            Register( "double", new Deserialization.DDouble() );
            Register( "char", new Deserialization.DChar() );
            Register( "DateTime", new Deserialization.DDateTime() );
            Register( "DateTimeOffset", new Deserialization.DDateTimeOffset() );

            void Register( string driverName, IDeserializationDriver driver )
            {
                _byName.Add( driverName, driver.ToNonNullable );
                _byName.Add( driverName + '?', driver.ToNullable );
            }
        }

        BasicTypeDeserializerRegistry() { }

        public IDeserializationDriver? TryFindDriver( TypeReadInfo info )
        {
            return info.DriverName != null ? _byName.GetValueOrDefault( info.DriverName ) : null;
        }
    }
}
