using System;
using System.Collections.Generic;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Immutable singleton that contains default serializers for well-known types.
    /// <para>
    /// A simple dictionary is enough since it is only read.
    /// </para>
    /// </summary>
    public class BasicTypeSerializerRegistry : ISerializerResolver
    {
        static readonly Dictionary<Type, ISerializationDriver> _byType;

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static readonly BasicTypeSerializerRegistry Instance = new BasicTypeSerializerRegistry();

        BasicTypeSerializerRegistry() { }

        static BasicTypeSerializerRegistry()
        {
            _byType = new Dictionary<Type, ISerializationDriver>();
            Register( new Serialization.DString() );
            Register( new Serialization.DByteArray() );

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
            Register( new Serialization.DTimeSpan() );
            Register( new Serialization.DGuid() );
            Register( new Serialization.DDecimal() );
        }

        static void Register<T>( StaticValueTypeSerializer<T> driver ) where T : struct
        {
            _byType.Add( typeof(T), driver );
            _byType.Add( typeof( Nullable<> ).MakeGenericType( typeof( T ) ), driver.ToNullable );
        }

        static void Register<T>( ReferenceTypeSerializer<T> driver ) where T : class
        {
            _byType.Add( typeof(T), driver.ToNullable );
        }

        /// <inheritdoc />
        public ISerializationDriver? TryFindDriver( Type t ) => _byType.GetValueOrDefault( t );

    }
}
