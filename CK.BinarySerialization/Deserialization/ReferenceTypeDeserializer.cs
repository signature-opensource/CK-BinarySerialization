using CK.Core;
using System;
using System.Runtime.CompilerServices;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Deserializer for type <typeparamref name="T"/> that handles nullable as well as non nullable written instances.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    public abstract class ReferenceTypeDeserializer<T> : INonNullableDeserializationDriver where T : class
    {
        class NullableAdapter : INullableDeserializationDriver
        {
            readonly ReferenceTypeDeserializer<T> _deserializer;
            readonly TypedReader<T?> _reader;

            public NullableAdapter( ReferenceTypeDeserializer<T> deserializer )
            {
                _deserializer = deserializer;
                _reader = ReadInstance;
            }

            public Type ResolvedType => _deserializer.ResolvedType;

            public Delegate TypedReader => _reader;

            INullableDeserializationDriver IDeserializationDriver.ToNullable => this;

            INonNullableDeserializationDriver IDeserializationDriver.ToNonNullable => _deserializer;

            public object? ReadAsObject( IBinaryDeserializer d, TypeReadInfo readInfo ) => ReadInstance( d, readInfo );

            public T? ReadInstance( IBinaryDeserializer d, TypeReadInfo readInfo )
            {
                if( d.Reader.ReadBoolean() )
                {
                    return _deserializer.ReadInstance( d, readInfo );
                }
                return default;
            }

        }

        readonly NullableAdapter _null;
        readonly TypedReader<T> _reader;

        protected ReferenceTypeDeserializer()
        {
            _null = new NullableAdapter( this );
            _reader = ReadInstance;
        }

        /// <summary>
        /// Secures object tracking by requiring the deserialized object to first be 
        /// instantiated before enabling the reading of its content.
        /// </summary>
        public ref struct RefReader
        {
            /// <summary>
            /// Gets the basic reader than can be used any time, typically 
            /// before calling <see cref="SetInstance(T)"/> to read data required to 
            /// instantiate the new object to read.
            /// </summary>
            public readonly ICKBinaryReader Reader;

            /// <summary>
            /// Gets the type read information.
            /// </summary>
            public readonly TypeReadInfo ReadInfo;

            /// <summary>
            /// Sets the unitialized instance and returns the deserializer to use
            /// to read the object's content.
            /// <para>
            /// This must be called once and only once 
            /// </para>
            /// </summary>
            /// <param name="o">The unitialized object.</param>
            /// <returns>The deserializer to use.</returns>
            public IBinaryDeserializer SetInstance( T o )
            {
                if( o == null ) throw new ArgumentNullException( "o" );
                if( Instance != null ) throw new InvalidOperationException( "Result already set." );
                Instance = o;
                return Unsafe.As<BinaryDeserializerImpl>( _d ).Track( o );
            }

            internal RefReader( IBinaryDeserializer d, TypeReadInfo i )
            {
                _d = d;
                ReadInfo = i;
                Reader = d.Reader;
                Instance = null;
            }
            internal T? Instance;
            readonly IBinaryDeserializer _d;
        }

        T ReadInstance( IBinaryDeserializer d, TypeReadInfo readInfo )
        {
            var c = new RefReader( d, readInfo );
            ReadInstance( ref c );
            if( c.Instance == null ) throw new InvalidOperationException( "ReadInstance must set a non null instance." );
            return c.Instance;
        }

        /// <summary>
        /// Must instantiate a new object, call <see cref="RefReader.SetInstance(T)"/> and
        /// then read its content.
        /// </summary>
        /// <param name="r">Deserialization context to use.</param>
        protected abstract void ReadInstance( ref RefReader r );

        /// <inheritdoc />
        public Type ResolvedType => typeof( T );

        /// <inheritdoc />
        public Delegate TypedReader => _reader;

        INullableDeserializationDriver IDeserializationDriver.ToNullable => _null;

        INonNullableDeserializationDriver IDeserializationDriver.ToNonNullable => this;

        object INonNullableDeserializationDriver.ReadAsObject( IBinaryDeserializer d, TypeReadInfo readInfo ) => ReadInstance( d, readInfo );

    }
}
