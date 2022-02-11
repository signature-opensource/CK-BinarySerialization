using System;
using System.Collections.Generic;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Immutable singleton that contains default serializers for well-known types.
    /// </summary>
    public class BasicTypeSerializerRegistry : ISerializerResolver
    {
        static readonly Dictionary<Type, ISerializationDriver> _byType;

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly ISerializerResolver Default = new BasicTypeSerializerRegistry();

        static BasicTypeSerializerRegistry()
        {
            _byType = new Dictionary<Type, ISerializationDriver>();
            Register( new Serialization.DString() );
            Register( new Serialization.DBool() );
            Register( new Serialization.DInt32() );
            Register( new Serialization.DUInt32() );
            Register( new Serialization.DInt8() );
            Register( new Serialization.DUInt8() );
            Register( new Serialization.DInt16() );
            Register( new Serialization.DUInt16() );
            Register( new Serialization.DInt64() );
            Register( new Serialization.DUInt64() );
            Register( new Serialization.DSingle() );
            Register( new Serialization.DDouble() );
            Register( new Serialization.DChar() );
            Register( new Serialization.DDateTime() );
            Register( new Serialization.DDateTimeOffset() );
        }

        static void Register<T>( ValueTypeSerializer<T> driver ) where T : struct
        {
            _byType.Add( driver.Type, driver );
            _byType.Add( driver.ToNullable.Type, driver.ToNullable );
        }

        static void Register<T>( ReferenceTypeSerializer<T> driver ) where T : class
        {
            _byType.Add( driver.Type, driver );
        }

        BasicTypeSerializerRegistry() { }

        /// <inheritdoc />
        public IValueTypeSerializationDriver<T>? TryFindValueTypeDriver<T>() where T : struct
        {
            return (IValueTypeSerializationDriver<T>?)_byType.GetValueOrDefault( typeof( T ) );
        }

        /// <inheritdoc />
        public IReferenceTypeSerializationDriver<T>? TryFindReferenceTypeDriver<T>() where T : class
        {
            return (IReferenceTypeSerializationDriver<T>?)_byType.GetValueOrDefault( typeof( T ) );
        }

        /// <inheritdoc />
        public ISerializationDriver? TryFindDriver( Type t )
        {
            return _byType.GetValueOrDefault( t );
        }

    }
}
