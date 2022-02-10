using System;
using System.Collections.Generic;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Immutable singleton that contains default serializers for well-known types.
    /// </summary>
    public class BasicTypeSerializerRegistry : ISerializerResolver
    {
        static readonly Dictionary<Type, IUntypedSerializationDriver> _byType;

        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly ISerializerResolver Default = new BasicTypeSerializerRegistry();

        static BasicTypeSerializerRegistry()
        {
            _byType = new Dictionary<Type, IUntypedSerializationDriver>();

            RegReferenceType( new Serialization.DString() );

            RegValueType( new Serialization.DBool() );
            RegValueType( new Serialization.DInt32() );
            RegValueType( new Serialization.DUInt32() );
            RegValueType( new Serialization.DInt8() );
            RegValueType( new Serialization.DUInt8() );
            RegValueType( new Serialization.DInt16() );
            RegValueType( new Serialization.DUInt16() );
            RegValueType( new Serialization.DInt64() );
            RegValueType( new Serialization.DUInt64() );
            RegValueType( new Serialization.DSingle() );
            RegValueType( new Serialization.DDouble() );
            RegValueType( new Serialization.DChar() );
            RegValueType( new Serialization.DDateTime() );
            RegValueType( new Serialization.DDateTimeOffset() );

            void RegValueType<T>( INonNullableSerializationDriver<T> driver ) where T : struct
            {
                _byType.Add( typeof( T ), driver );
                _byType.Add( typeof( T? ), new ValueTypeNullableDriver<T>( driver ) );
            }

            void RegReferenceType<T>( INonNullableSerializationDriver<T> driver ) where T : class
            {
                _byType.Add( typeof( T ), new ReferenceTypeNullableDriver<T>( driver ) );
            }
        }

        BasicTypeSerializerRegistry() { }

        /// <inheritdoc />
        public ISerializationDriver<T>? TryFindDriver<T>()
        {
            return (ISerializationDriver<T>?)_byType.GetValueOrDefault( typeof( T ) );
        }

        /// <inheritdoc />
        public IUntypedSerializationDriver? TryFindDriver( Type t )
        {
            return _byType.GetValueOrDefault( t );
        }

    }
}
