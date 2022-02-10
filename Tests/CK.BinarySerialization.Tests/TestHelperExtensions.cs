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
        public static object? SaveAndLoadObject( this IBasicTestHelper @this, object? o,
                                                                              IServiceProvider? serviceProvider = null,
                                                                              ISerializerResolver? serializers = null,
                                                                              IDeserializerResolver? deserializers = null )
        {
            return SaveAndLoad( @this, o, ( x, w ) => w.WriteAnyNullable( x ), r => r.ReadAnyNullable(), serviceProvider, serializers, deserializers );
        }

        public static T SaveAndLoadValue<T>( this IBasicTestHelper @this, in T v,
                                                                          IServiceProvider? serviceProvider = null,
                                                                          ISerializerResolver? serializers = null,
                                                                          IDeserializerResolver? deserializers = null ) where T : struct
        {
            return SaveAndLoad<T>( @this, v, ( x, w ) => w.WriteValue( x ), r => r.ReadValue<T>(), serviceProvider, serializers, deserializers );
        }

        public static T? SaveAndLoadNullableValue<T>( this IBasicTestHelper @this, in T? v,
                                                                                   IServiceProvider? serviceProvider = null,
                                                                                   ISerializerResolver? serializers = null,
                                                                                   IDeserializerResolver? deserializers = null ) where T : struct
        {
            return SaveAndLoad<T?>( @this, v, ( x, w ) => w.WriteNullableValue( x ), r => r.ReadNullableValue<T>(), serviceProvider, serializers, deserializers );
        }

        public static T SaveAndLoad<T>( this IBasicTestHelper @this, in T o, 
                                                                     Action<T, IBinarySerializer> w, 
                                                                     Func<IBinaryDeserializer, T> r,
                                                                     IServiceProvider? serviceProvider = null,
                                                                     ISerializerResolver? serializers = null, 
                                                                     IDeserializerResolver? deserializers = null )
        {
            using( var s = new MemoryStream() )
            using( var writer = BinarySerializer.Create( s, true, serializers ) )
            {
                writer.DebugWriteSentinel();
                w( o, writer );
                writer.DebugWriteSentinel();
                s.Position = 0;
                using( var reader = BinaryDeserializer.Create( s, true, deserializers ) )
                {
                    reader.DebugCheckSentinel();
                    T result = r( reader );
                    reader.DebugCheckSentinel();
                    return result;
                }
            }
        }

    }
}
