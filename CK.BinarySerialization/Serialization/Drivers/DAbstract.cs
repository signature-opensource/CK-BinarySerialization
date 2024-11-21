using CK.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization.Serialization;

sealed class DAbstract : ISerializationDriver
{
    TypedWriter<object?> _reader;

    public static DAbstract Instance = new DAbstract( false );
    static DAbstract NullableInstance = new DAbstract( true );

    DAbstract( bool isNullable )
    {
        _reader = isNullable ? WriteNullable : WriteNonNullable;
    }

    public string DriverName => "object";

    public int SerializationVersion => -1;

    public Delegate UntypedWriter => _reader;

    public Delegate TypedWriter => _reader;

    static void WriteNullable( IBinarySerializer s, in object? o )
    {
        if( o == null )
        {
            s.Writer.Write( (byte)SerializationMarker.Null );
        }
        else
        {
            Unsafe.As<BinarySerializerImpl>( s ).DoWriteObject( o );
        }
    }

    static void WriteNonNullable( IBinarySerializer s, in object? o )
    {
        Throw.CheckNotNullArgument( o );
        Unsafe.As<BinarySerializerImpl>( s ).DoWriteObject( o );
    }

    public ISerializationDriver ToNullable => NullableInstance;

    public ISerializationDriver ToNonNullable => Instance;

    /// <summary>
    /// Somehow irrelevant since this driver (and its nullable) are true singletons. 
    /// </summary>
    SerializationDriverCacheLevel ISerializationDriver.CacheLevel => SerializationDriverCacheLevel.SharedContext;
}
