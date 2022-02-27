using CK.Core;
using CK.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Static thread safe registry for <see cref="ICKSimpleBinarySerializable"/> and <see cref="ICKVersionedBinarySerializable"/>
    /// deserializers.
    /// <para>
    /// Since this kind on serialization don't need any other resolvers (drivers only depends on the actual type), a singleton
    /// cache is fine and it uses the <see cref="SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers"/>
    /// cache since the synthesized drivers only depends on the local type.
    /// </para>
    /// </summary>
    public sealed class SimpleBinaryDeserializableRegistry : IDeserializerResolver
    {
        /// <summary>
        /// Gets the default registry.
        /// </summary>
        public static readonly IDeserializerResolver Instance = new SimpleBinaryDeserializableRegistry();

        SimpleBinaryDeserializableRegistry() { }

        public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
        {
            // Only the "SimpleBinarySerializable" or "SealedVersionBinarySerializable" drivers are handled here.
            bool isSimple = info.DriverName == "SimpleBinarySerializable";
            bool isSealed = !isSimple && info.DriverName == "SealedVersionBinarySerializable";
            if( !isSimple && !isSealed ) return null;

            // We allow Simple to be read back as Sealed and the opposite.
            // We lookup the TargetType constructors here. Should we handle the case where the TargetType has no 
            // simple deserializer constructors? 
            // We may lookup the LocalType simple deserializer constructors as well and handle the conversion
            // from Local to TargetType via a constructor TargetType( LocalType ) (and/or Convert.ChangeType and
            // standard .Net TypeConverters).
            // For the moment, there's no such conversions.

            // We cache the driver only if the TargetType is the LocalType ("nominal" deserializers) since if they differ,
            // an adaptation via VersionedBinaryDeserializableDriverVFromRef may be required (for the moment, this is
            // the only existing adaptation).
            // We may have duplicate calls to Create here (that should barely happen but who knows), but GetOrAdd
            // will return the winner.
            // 
            var ctor = BinaryDeserializer.Helper.GetVersionedConstructor( info.TargetType );
            if( ctor != null )
            {
                if( info.IsTargetSameAsLocalType )
                {
                    return SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers.GetOrAdd( info.TargetType, CreateCachedSealed, ctor );
                }
                if( info.TargetType.IsValueType )
                {
                    var tGen = info.ReadInfo.IsValueType
                                ? typeof( VersionedBinaryDeserializableDriverV<> )
                                : typeof( VersionedBinaryDeserializableDriverVFromRef<> );
                    return (IDeserializationDriver)Activator.CreateInstance( tGen.MakeGenericType( info.TargetType ), ctor )!;
                }
                var tR = typeof( VersionedBinaryDeserializableDriverR<> ).MakeGenericType( info.TargetType );
                return (IDeserializationDriver)Activator.CreateInstance( tR, ctor )!;
            }
            ctor = BinaryDeserializer.Helper.GetSimpleConstructor( info.TargetType );
            if( ctor != null )
            {
                if( info.IsTargetSameAsLocalType )
                {
                    return SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers.GetOrAdd( info.TargetType, CreateCachedSimple, ctor );
                }
                if( info.TargetType.IsValueType )
                {
                    var tGen = info.ReadInfo.IsValueType
                                ? typeof( SimpleBinaryDeserializableDriverV<> )
                                : typeof( SimpleBinaryDeserializableDriverVFromRef<> );
                    return (IDeserializationDriver)Activator.CreateInstance( tGen.MakeGenericType( info.TargetType ), ctor )!;
                }
                var tR = typeof( SimpleBinaryDeserializableDriverR<> ).MakeGenericType( info.TargetType );
                return (IDeserializationDriver)Activator.CreateInstance( tR, ctor )!;
            }
            return null;
        }

        sealed class SimpleBinaryDeserializableDriverVFromRef<T> : ValueTypeDeserializerWithRef<T> where T : struct
        {
            readonly Func<ICKBinaryReader, T> _factory;

            public SimpleBinaryDeserializableDriverVFromRef( ConstructorInfo ctor )
            {
                _factory = BinaryDeserializer.Helper.CreateSimpleNewDelegate<T>( ctor );
            }

            protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _factory( d.Reader );
        }

        sealed class SimpleBinaryDeserializableDriverV<T> : ValueTypeDeserializer<T> where T : struct
        {
            readonly Func<ICKBinaryReader, T> _factory;

            public SimpleBinaryDeserializableDriverV( ConstructorInfo ctor )
            {
                _factory = BinaryDeserializer.Helper.CreateSimpleNewDelegate<T>( ctor );
            }

            protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _factory( d.Reader );
        }

        sealed class SimpleBinaryDeserializableDriverR<T> : SimpleReferenceTypeDeserializer<T> where T : class
        {
            readonly Func<ICKBinaryReader, T> _factory;

            public SimpleBinaryDeserializableDriverR( ConstructorInfo ctor )
            {
                _factory = BinaryDeserializer.Helper.CreateSimpleNewDelegate<T>( ctor );
            }

            protected override T ReadInstance( ICKBinaryReader r, ITypeReadInfo readInfo ) => _factory( r );
        }

        static IDeserializationDriver CreateCachedSimple( Type t, ConstructorInfo ctor )
        {
            if( t.IsValueType )
            {
                var tV = typeof( SimpleBinaryDeserializableDriverV<> ).MakeGenericType( t );
                return (IDeserializationDriver)Activator.CreateInstance( tV, ctor )!;
            }
            var tR = typeof( SimpleBinaryDeserializableDriverR<> ).MakeGenericType( t );
            return (IDeserializationDriver)Activator.CreateInstance( tR, ctor )!;
        }

        sealed class VersionedBinaryDeserializableDriverVFromRef<T> : ValueTypeDeserializerWithRef<T> where T : struct
        {
            readonly Func<ICKBinaryReader, int, T> _factory;

            public VersionedBinaryDeserializableDriverVFromRef( ConstructorInfo ctor )
            {
                _factory = BinaryDeserializer.Helper.CreateVersionedNewDelegate<T>( ctor );
            }

            protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _factory( d.Reader, d.SerializerVersion );
        }

        sealed class VersionedBinaryDeserializableDriverV<T> : ValueTypeDeserializer<T> where T : struct
        {
            readonly Func<ICKBinaryReader, int, T> _factory;

            public VersionedBinaryDeserializableDriverV( ConstructorInfo ctor )
            {
                _factory = BinaryDeserializer.Helper.CreateVersionedNewDelegate<T>( ctor );
            }

            protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _factory( d.Reader, d.SerializerVersion );
        }

        sealed class VersionedBinaryDeserializableDriverR<T> : SimpleReferenceTypeDeserializer<T> where T : class
        {
            readonly Func<ICKBinaryReader, int, T> _factory;

            public VersionedBinaryDeserializableDriverR( ConstructorInfo ctor )
            {
                _factory = BinaryDeserializer.Helper.CreateVersionedNewDelegate<T>( ctor );
            }

            protected override T ReadInstance( ICKBinaryReader r, ITypeReadInfo readInfo ) => _factory( r, readInfo.SerializationVersion );
        }

        static IDeserializationDriver CreateCachedSealed( Type t, ConstructorInfo ctor )
        {
            if( t.IsValueType )
            {
                var tV = typeof( VersionedBinaryDeserializableDriverV<> ).MakeGenericType( t );
                return (IDeserializationDriver)Activator.CreateInstance( tV, ctor )!;
            }
            var tR = typeof( VersionedBinaryDeserializableDriverR<> ).MakeGenericType( t );
            return (IDeserializationDriver)Activator.CreateInstance( tR , ctor )!;
        }

    }
}
