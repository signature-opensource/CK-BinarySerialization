using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Serialization driver wrapper for nullable value type.
    /// The driver name is suffixed by '?'.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    public sealed class ReferenceTypeNullableDriver<T> : ISerializationDriver<T?> where T : class
    {
        readonly INonNullableSerializationDriver<T> _value;

        public ReferenceTypeNullableDriver( INonNullableSerializationDriver<T> value )
        {
            _value = value;
            DriverName = value.DriverName + '?';
        }

        /// <inheritdoc />
        public string DriverName { get; }

        /// <inheritdoc />
        public int SerializationVersion => _value.SerializationVersion;

        /// <summary>
        /// Writes the nullable value type.
        /// </summary>
        /// <param name="w">The serializer.</param>
        /// <param name="o">The nullable value type.</param>
        public void WriteData( IBinarySerializer w, in T? o )
        {
            if( o != null )
            {
                w.Writer.Write( true );
                _value.WriteData( w, o );
            }
            else
            {
                w.Writer.Write( false );
            }
        }
    }
}
