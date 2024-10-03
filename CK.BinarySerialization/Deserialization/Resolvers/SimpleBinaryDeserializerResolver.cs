using CK.Core;
using System;
using System.Reflection;

namespace CK.BinarySerialization;

/// <summary>
/// Static thread safe registry for <see cref="ICKSimpleBinarySerializable"/> and <see cref="ICKVersionedBinarySerializable"/>
/// deserializers.
/// <para>
/// Since this kind on serialization don't need any other resolvers (drivers only depends on the actual type), a singleton
/// cache is fine and it uses the <see cref="SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers"/>
/// cache since the synthesized drivers only depends on the local type.
/// </para>
/// </summary>
public sealed class SimpleBinaryDeserializerResolver : IDeserializerResolver
{
    /// <summary>
    /// Gets the default registry.
    /// </summary>
    public static readonly IDeserializerResolver Instance = new SimpleBinaryDeserializerResolver();

    SimpleBinaryDeserializerResolver() { }

    /// <summary>
    /// Synthesizes a deserialization driver if the <see cref="DeserializerResolverArg.DriverName"/>
    /// is "SimpleBinarySerializable" or "VersionedBinarySerializable" and the corresponding 
    /// constructor exists.
    /// </summary>
    /// <param name="info">The info to resolve.</param>
    /// <returns>The driver or null.</returns>
    public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
    {
        // Only the "SimpleBinarySerializable" or "VersionedBinarySerializable" drivers are handled here.
        bool isSimple = info.DriverName == "SimpleBinarySerializable";
        bool isSealed = !isSimple && info.DriverName == "VersionedBinarySerializable";
        if( !isSimple && !isSealed ) return null;

        // We allow Simple to be read back as Sealed and the opposite.
        // We lookup the TargetType constructors here. Should we handle the case where the TargetType has no 
        // such deserializer constructors but the LocalType (if it can be resolved) has?
        // 
        // We may lookup the LocalType simple deserializer constructors as well and handle the conversion
        // from Local to TargetType via a constructor TargetType( LocalType ) (and/or Convert.ChangeType and
        // standard .Net TypeConverters).
        // For the moment, there's no such conversions.
        // 
        return TryGetOrCreateVersionedDriver( ref info, true ) ?? TryGetOrCreateSimpleDriver( ref info, true );
    }

    /// <summary>
    /// Publicly exposed to allow other deserializers to be able to fallback to Simple deserialization
    /// if they want.
    /// Note that only the existence of the simple deserialization constructor matters: the <see cref="ICKSimpleBinarySerializable"/> interface
    /// doesn't need to be defined on the <see cref="DeserializerResolverArg.ExpectedType"/>.
    /// </summary>
    /// <param name="info">The info to resolve.</param>
    /// <returns>The driver or null.</returns>
    public static IDeserializationDriver? TryGetOrCreateSimpleDriver( ref DeserializerResolverArg info )
    {
        return TryGetOrCreateSimpleDriver( ref info, false );
    }

    static IDeserializationDriver? TryGetOrCreateSimpleDriver( ref DeserializerResolverArg info, bool internalCall )
    {
        ConstructorInfo? ctor = BinaryDeserializer.Helper.GetSimpleConstructor( info.ExpectedType );
        if( ctor != null )
        {
            // Simple basic strategy here: as soon as the written type is not the target one, we skip caching.
            if( internalCall && info.IsPossibleNominalDeserialization )
            {
                // We cache the driver only if the TargetType is the LocalType ("nominal" deserializers) since if they differ,
                // an adaptation via SimpleBinaryDeserializableDriverVFromRef may be required (for the moment, struct from/to class is
                // the only existing adaptation).
                // We may have duplicate calls to Create here (that should barely happen but who knows), but GetOrAdd
                // will return the winner.
                return SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers.GetOrAdd( info.ExpectedType, CreateCachedSimple, ctor );
            }
            if( info.ExpectedType.IsValueType )
            {
                if( info.ReadInfo.IsValueType )
                {
                    // Creates a non cached value type deserializer.
                    return (IDeserializationDriver)Activator.CreateInstance(
                                    typeof( SimpleBinaryDeserializableDriverV<> ).MakeGenericType( info.ExpectedType ),
                                    ctor,
                                    false )!;

                }
                // FromRef is by design not cached.
                return (IDeserializationDriver)Activator.CreateInstance(
                                typeof( SimpleBinaryDeserializableDriverVFromRef<> ).MakeGenericType( info.ExpectedType ),
                                ctor )!;
            }
            var tR = typeof( SimpleBinaryDeserializableDriverR<> ).MakeGenericType( info.ExpectedType );
            return (IDeserializationDriver)Activator.CreateInstance( tR, ctor, false )!;
        }
        return null;
    }

    /// <summary>
    /// Publicly exposed to allow other deserializers to be able to fallback to Versioned serialization
    /// if they want.
    /// Note that only the existence of the versioned deserialization constructor matters: the <see cref="ICKVersionedBinarySerializable"/> interface
    /// doesn't need to be defined on the <see cref="DeserializerResolverArg.ExpectedType"/>.
    /// </summary>
    /// <param name="info">The info to resolve.</param>
    /// <returns>The driver or null.</returns>
    public static IDeserializationDriver? TryGetOrCreateVersionedDriver( ref DeserializerResolverArg info )
    {
        return TryGetOrCreateVersionedDriver( ref info, false );
    }

    static IDeserializationDriver? TryGetOrCreateVersionedDriver( ref DeserializerResolverArg info, bool internalCall )
    {
        ConstructorInfo? ctor = BinaryDeserializer.Helper.GetVersionedConstructor( info.ExpectedType );
        if( ctor != null )
        {
            // Simple basic strategy here: as soon as the written type is not the target one, we skip caching.
            if( internalCall && info.IsPossibleNominalDeserialization )
            {
                return SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers.GetOrAdd( info.ExpectedType, CreateCachedVersioned, ctor );
            }
            if( info.ExpectedType.IsValueType )
            {
                if( info.ReadInfo.IsValueType )
                {
                    // Creates a non cached value type deserializer.
                    return (IDeserializationDriver)Activator.CreateInstance(
                                    typeof( VersionedBinaryDeserializableDriverV<> ).MakeGenericType( info.ExpectedType ),
                                    ctor,
                                    false )!;

                }
                // FromRef is by design not cached.
                return (IDeserializationDriver)Activator.CreateInstance(
                                typeof( VersionedBinaryDeserializableDriverVFromRef<> ).MakeGenericType( info.ExpectedType ),
                                ctor )!;
            }
            var tR = typeof( VersionedBinaryDeserializableDriverR<> ).MakeGenericType( info.ExpectedType );
            return (IDeserializationDriver)Activator.CreateInstance( tR, ctor, false )!;
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

        public SimpleBinaryDeserializableDriverV( ConstructorInfo ctor, bool isCached )
            : base( isCached )
        {
            _factory = BinaryDeserializer.Helper.CreateSimpleNewDelegate<T>( ctor );
        }

        protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _factory( d.Reader );
    }

    sealed class SimpleBinaryDeserializableDriverR<T> : SimpleReferenceTypeDeserializer<T> where T : class
    {
        readonly Func<ICKBinaryReader, T> _factory;

        public SimpleBinaryDeserializableDriverR( ConstructorInfo ctor, bool isCached )
            : base( isCached )
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
            return (IDeserializationDriver)Activator.CreateInstance( tV, ctor, true )!;
        }
        var tR = typeof( SimpleBinaryDeserializableDriverR<> ).MakeGenericType( t );
        return (IDeserializationDriver)Activator.CreateInstance( tR, ctor, true )!;
    }

    sealed class VersionedBinaryDeserializableDriverVFromRef<T> : ValueTypeDeserializerWithRef<T> where T : struct
    {
        readonly Func<ICKBinaryReader, int, T> _factory;

        public VersionedBinaryDeserializableDriverVFromRef( ConstructorInfo ctor )
        {
            _factory = BinaryDeserializer.Helper.CreateVersionedNewDelegate<T>( ctor );
        }

        protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _factory( d.Reader, readInfo.Version );
    }

    sealed class VersionedBinaryDeserializableDriverV<T> : ValueTypeDeserializer<T> where T : struct
    {
        readonly Func<ICKBinaryReader, int, T> _factory;

        public VersionedBinaryDeserializableDriverV( ConstructorInfo ctor, bool isCached )
            : base( isCached )
        {
            _factory = BinaryDeserializer.Helper.CreateVersionedNewDelegate<T>( ctor );
        }

        protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _factory( d.Reader, readInfo.Version );
    }

    sealed class VersionedBinaryDeserializableDriverR<T> : SimpleReferenceTypeDeserializer<T> where T : class
    {
        readonly Func<ICKBinaryReader, int, T> _factory;

        public VersionedBinaryDeserializableDriverR( ConstructorInfo ctor, bool isCached )
            : base( isCached )
        {
            _factory = BinaryDeserializer.Helper.CreateVersionedNewDelegate<T>( ctor );
        }

        protected override T ReadInstance( ICKBinaryReader r, ITypeReadInfo readInfo ) => _factory( r, readInfo.Version );
    }

    static IDeserializationDriver CreateCachedVersioned( Type t, ConstructorInfo ctor )
    {
        if( t.IsValueType )
        {
            var tV = typeof( VersionedBinaryDeserializableDriverV<> ).MakeGenericType( t );
            return (IDeserializationDriver)Activator.CreateInstance( tV, ctor, true )!;
        }
        var tR = typeof( VersionedBinaryDeserializableDriverR<> ).MakeGenericType( t );
        return (IDeserializationDriver)Activator.CreateInstance( tR , ctor, true )!;
    }

}
