using CK.BinarySerialization;
using CK.Testing;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CK.Core
{
    static class TestHelperExtensions
    {

        public static object? SaveAndLoadObject( this IBasicTestHelper @this, object? o, 
                                                                              IServiceProvider? serviceProvider = null, 
                                                                              ISerializerResolver? serializers = null, 
                                                                              IDeserializerResolver? deserializers = null )
        {
            return SaveAndLoadObject( @this, o, ( x, w ) => w.WriteNullableObject( x ), r => r.ReadObject(), serializers, deserializers );
        }

        public static T SaveAndLoadObject<T>( this IBasicTestHelper @this, T o, 
                                                                           Action<T, IBinarySerializer> w, 
                                                                           Func<IBinaryDeserializer, T> r, 
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
