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

            public object? ReadAsObject( IBinaryDeserializer d, ITypeReadInfo readInfo ) => ReadInstance( d, readInfo );

            public T? ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
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
            public readonly ITypeReadInfo ReadInfo;

            /// <summary>
            /// Sets the unitialized instance and returns the deserializer to use
            /// to read the object's content.
            /// <para>
            /// This must be called once and only once.
            /// </para>
            /// </summary>
            /// <param name="o">The instantiated but not yet fully initialized object.</param>
            /// <returns>The deserializer to use.</returns>
            public IBinaryDeserializer SetInstance( T o )
            {
                if( o == null ) throw new ArgumentNullException( "o" );
                if( Instance != null ) throw new InvalidOperationException( "Result already set." );
                Instance = o;
                return Unsafe.As<BinaryDeserializerImpl>( _d ).Track( o );
            }

            /// <summary>
            /// Sets the instance by allowing to read a header for the object instantiation.
            /// <para></para>
            /// This must be used if instantiating the object requires some data: these data must appear first 
            /// (hence the term <paramref name="headerReader"/>) and should have no back reference to any object of 
            /// the deserialized graph.
            /// <para>
            /// This must be called once and only once before reading the actual object's content.
            /// </para>
            /// </summary>
            /// <param name="headerReader">
            /// Must return the instantiated but not yet fully initialized object. This function is 
            /// allowed to read some simple data from the deserializer.
            /// </param>
            /// <returns>The deserializer to use to read the object's content and the instantiated object.</returns>
            public (IBinaryDeserializer d, T o) SetInstance( Func<IBinaryDeserializer,T> headerReader )
            {
                if( headerReader == null ) throw new ArgumentNullException( "o" );
                if( Instance != null ) throw new InvalidOperationException( "Result already set." );
                Instance = headerReader( _d );
                return (Unsafe.As<BinaryDeserializerImpl>( _d ).Track( Instance ), Instance );
            }

            internal RefReader( IBinaryDeserializer d, ITypeReadInfo i )
            {
                _d = d;
                ReadInfo = i;
                Reader = d.Reader;
                Instance = null;
            }
            internal T? Instance;
            readonly IBinaryDeserializer _d;
        }

        T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
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

        object INonNullableDeserializationDriver.ReadAsObject( IBinaryDeserializer d, ITypeReadInfo readInfo ) => ReadInstance( d, readInfo );

    }
}
