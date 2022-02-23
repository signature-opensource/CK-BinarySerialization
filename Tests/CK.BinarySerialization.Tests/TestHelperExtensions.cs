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
        public static bool CheckObjectReferences = true;

        /// <summary>
        /// Gets or sets whether SaveAndLoad methods check the reference management
        /// by writing a first (empty) object before and after the actual write, and yet another
        /// (empty) instance after twice (to use the reference tracking). 
        /// When reading, we first read this empty instance before the actual read, read it again
        /// and reads twice the second instance.
        /// <para>
        /// Defaults to true.
        /// </para>
        /// </summary>
        /// <param name="this">This tester.</param>
        /// <param name="check">Check the references.</param>
        public static void SetCheckObjectReferences( this IBasicTestHelper @this, bool check )
        {
            CheckObjectReferences = check;
        }


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
                writer.DebugWriteMode( true );

                var o1 = new object();
                if( CheckObjectReferences )
                {
                    writer.WriteAny( o1 );
                }
                
                writer.DebugWriteSentinel();
                w( o, writer );
                writer.DebugWriteSentinel();

                if( CheckObjectReferences )
                {
                    writer.WriteAny( o1 );
                    var o2 = new object();
                    writer.WriteAny( o2 );
                    writer.WriteAny( o2 );
                }
                s.Position = 0;
                using( var reader = BinaryDeserializer.Create( s, true, deserializerContext ?? new BinaryDeserializerContext() ) )
                {
                    reader.DebugReadMode();
                    
                    object? r1 = null;
                    if( CheckObjectReferences )
                    {
                        r1 = reader.ReadAny();
                    }
                    
                    reader.DebugCheckSentinel();
                    T result = r( reader );
                    reader.DebugCheckSentinel();

                    if( CheckObjectReferences )
                    {
                        reader.ReadAny().Should().BeSameAs( r1 );
                        var r2 = reader.ReadAny();
                        r2.Should().BeOfType<object>();
                        reader.ReadAny().Should().BeSameAs( r2 );
                    }
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
