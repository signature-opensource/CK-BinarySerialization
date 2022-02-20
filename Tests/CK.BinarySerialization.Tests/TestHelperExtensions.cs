using CK.BinarySerialization;
using CK.Testing;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace CK.Core
{
    static class TestHelperExtensions
    {

        [return: NotNullIfNotNull("o")]
        public static T SaveAndLoadObject<T>( this IBasicTestHelper @this, T o,
                                                                            BinarySerializerContext? serializerContext = null,
                                                                            BinaryDeserializerContext? deserializerContext = null ) where T : class
        {
            return SaveAndLoad( @this, o, ( x, w ) => w.WriteObject( x ), r => r.ReadObject<T>(), serializerContext, deserializerContext );
        }

        [return: NotNullIfNotNull("o")]
        public static object? SaveAndLoadAny( this IBasicTestHelper @this, object? o,
                                                                           BinarySerializerContext? serializerContext = null,
                                                                           BinaryDeserializerContext? deserializerContext = null )
        {
            return SaveAndLoad( @this, o, ( x, w ) => w.WriteAnyNullable( x ), r => r.ReadAnyNullable(), serializerContext, deserializerContext );
        }

        public static T SaveAndLoadValue<T>( this IBasicTestHelper @this, in T v,
                                                                          BinarySerializerContext? serializerContext = null,
                                                                          BinaryDeserializerContext? deserializerContext = null ) where T : struct
        {
            return SaveAndLoad<T>( @this, v, ( x, w ) => w.WriteValue( x ), r => r.ReadValue<T>(), serializerContext, deserializerContext );
        }

        public static T? SaveAndLoadNullableValue<T>( this IBasicTestHelper @this, in T? v,
                                                                                   BinarySerializerContext? serializerContext = null,
                                                                                   BinaryDeserializerContext? deserializerContext = null ) where T : struct
        {
            return SaveAndLoad<T?>( @this, v, ( x, w ) => w.WriteNullableValue( x ), r => r.ReadNullableValue<T>(), serializerContext, deserializerContext );
        }

        public static T SaveAndLoad<T>( this IBasicTestHelper @this, in T o,
                                                                     Action<T, IBinarySerializer> w,
                                                                     Func<IBinaryDeserializer, T> r,
                                                                     BinarySerializerContext? serializerContext = null,
                                                                     BinaryDeserializerContext? deserializerContext = null )
        {
            using( var s = new MemoryStream() )
            using( var writer = BinarySerializer.Create( s, true, serializerContext ?? new BinarySerializerContext() ) )
            {
                writer.DebugWriteSentinel();
                w( o, writer );
                writer.DebugWriteSentinel();
                s.Position = 0;
                using( var reader = BinaryDeserializer.Create( s, true, deserializerContext ?? new BinaryDeserializerContext() ) )
                {
                    reader.DebugCheckSentinel();
                    T result = r( reader );
                    reader.DebugCheckSentinel();
                    return result;
                }
            }
        }

        public static void SaveAndLoad( this IBasicTestHelper @this, Action<IBinarySerializer> w,
                                                                     Action<IBinaryDeserializer> r,
                                                                     BinarySerializerContext? serializerContext = null,
                                                                     BinaryDeserializerContext? deserializerContext = null )
        {
            using( var s = new MemoryStream() )
            using( var writer = BinarySerializer.Create( s, true, serializerContext ?? new BinarySerializerContext() ) )
            {
                writer.DebugWriteSentinel();
                w( writer );
                writer.DebugWriteSentinel();
                s.Position = 0;
                using( var reader = BinaryDeserializer.Create( s, true, deserializerContext ?? new BinaryDeserializerContext() ) )
                {
                    reader.DebugCheckSentinel();
                    r( reader );
                    reader.DebugCheckSentinel();
                }
            }
        }

    }
}
