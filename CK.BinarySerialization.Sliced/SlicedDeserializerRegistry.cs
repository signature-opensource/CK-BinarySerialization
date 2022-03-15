using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Registry for "Sliced" deserializers. 
    /// <para>
    /// The synthesized drivers depends on the local type (and its inheritance list) and don't directly need any other resolvers.
    /// Only the "nominal drivers" are cached in the <see cref="SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers"/>:
    /// "nominal drivers" are:
    /// <list type="bullet">
    ///     <item>
    ///     When the local type is a class with only one existing deserialization constructor (its base class is Object or a non ICKSlicedSerializable 
    ///     object), we have no other options to trust its constructor that must then be able to handle 
    ///     any "previous" writes with potentially multiple TypeReadInfo from previous base classes and the standard <see cref="ReferenceTypeDeserializer{T}"/>
    ///     gracefully handles previously written struct: we can cache and reuse the driver.
    ///     </item>
    ///     <item>
    ///     When the local type is a class with more than one deserialization constructors on its base types, it's much more complex: 
    ///     some base classes may have disappeared or appeared. Knowing which "Slice" can or should handle which TypeReadInfo is (more than) tricky. 
    ///     <para>
    ///     The "mapping" heuristic is rather simple:
    ///     <list type="bullet">
    ///         <item>
    ///         First we associate each constructor to the TypeReadInfo that has the same local type regardless of its position
    ///         in the TyperReadInfo chain. This handles renaming (as long as type has been mapped) and suppression of base classes.
    ///         </item>
    ///         <item>
    ///         Constructors that are "unbound" are provided with a <see cref="MissingSlicedTypeReadInfo"/>... and let it be.
    ///         </item>
    ///     </list>
    ///     </para>
    ///     <para>
    ///     As soon as we have to use a "mapping" like this, the driver is not nominal. 
    ///     </para>
    ///     <para>
    ///     We cache (and reuse) the deserializer only if the TypeReadInfo.TypePath's local types match the local constructor's chain.
    ///     </para>
    ///     </item>
    ///     <item>
    ///     When the local type is a ValueType, we have no other options to trust its constructor that must then be able to handle 
    ///     any "previous" writes with potentially multiple TypeReadInfo from previous base classes.
    ///     <para>
    ///     This differs from the first case above on one point: if the written type was a reference type we must use the <see cref="ValueTypeDeserializerWithRef{T}"/>
    ///     adapter instead of the efficient <see cref="ValueTypedReaderDeserializer{T}"/> we can use.
    ///     <para>
    ///     Only if the written type was a struct can we cache and reuse the driver in this case.
    ///     </para>
    ///     </para>
    ///     </item>
    /// </list>
    /// </para>
    /// </summary>
    public class SlicedDeserializerRegistry : IDeserializerResolver
    {
        /// <summary>
        /// Gets the registry.
        /// </summary>
        public static readonly SlicedDeserializerRegistry Instance = new SlicedDeserializerRegistry();

        SlicedDeserializerRegistry() { }

        sealed class SlicedDeserializerDriverVWithRef<T> : ValueTypeDeserializerWithRef<T> where T : struct
        {
            readonly TypedReader<T> _factory;

            public SlicedDeserializerDriverVWithRef( ConstructorInfo ctor )
            {
                _factory = BinaryDeserializer.Helper.CreateTypedReaderNewDelegate<T>( ctor );
            }

            protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _factory( d, readInfo );
        }

        sealed class SlicedDeserializerDriverRMonoCtor<T> : ReferenceTypeDeserializer<T>, IDeserializationDeferredDriver where T : class
        {
            readonly ConstructorInfo _ctor;

            public SlicedDeserializerDriverRMonoCtor( ConstructorInfo ctor )
                :  base( true )
            {
                _ctor = ctor;
            }

            public void ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo, object o )
            {
                _ctor.Invoke( o, new object?[] { d, readInfo } );
            }

            protected override void ReadInstance( ref RefReader r )
            {
                var o = RuntimeHelpers.GetUninitializedObject( typeof( T ) );
                var d = r.SetInstance( (T)o );
                ReadInstance( d, r.ReadInfo, o );
            }
        }

        sealed class SlicedDeserializerDriverRNominal<T> : ReferenceTypeDeserializer<T>, IDeserializationDeferredDriver where T : class
        {
            readonly ConstructorInfo[] _ctors;

            public SlicedDeserializerDriverRNominal( List<ConstructorInfo> ctors )
                : base( true )
            {
                Debug.Assert( ctors.Count > 1 );
                _ctors = ctors.ToArray();
            }

            public void ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo, object o )
            {
                int idxInfo = readInfo.TypePath.Count - _ctors.Length;
                var parameters = new object?[] { d, readInfo.TypePath[idxInfo] };
                // Call the top one.
                _ctors[0].Invoke( o, parameters );
                // Challenge the IDestroyable and calls the remainders if the object is not destroyed.
                if( o is not IDestroyable destroyed || !destroyed.IsDestroyed )
                {
                    int idxCtor = 1;
                    do
                    {
                        parameters[1] = readInfo.TypePath[++idxInfo];
                        _ctors[idxCtor].Invoke( o, parameters );
                    }
                    while( ++idxCtor < _ctors.Length );
                }
            }

            protected override void ReadInstance( ref RefReader r )
            {
                var o = RuntimeHelpers.GetUninitializedObject( typeof( T ) );
                var d = r.SetInstance( (T)o );
                ReadInstance( d, r.ReadInfo, o );
            }
        }

        sealed class SlicedDeserializerDriverGoodLuck<T> : ReferenceTypeDeserializer<T>, IDeserializationDeferredDriver where T : class
        {
            readonly List<ConstructorInfo> _ctors;
            readonly ITypeReadInfo[] _readTypes;

            public SlicedDeserializerDriverGoodLuck( List<ConstructorInfo> ctors, ITypeReadInfo[] readTypes )
                : base( false )
            {
                Debug.Assert( ctors.Count == readTypes.Length );
                _ctors = ctors;
                _readTypes = readTypes;
            }

            public void ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo, object o )
            {
                // Call the top one.
                var parameters = new object?[] { d, _readTypes[0] };
                _ctors[0].Invoke( o, parameters );
                // Challenge the IDestroyable and calls the remainders if the object is not destroyed.
                if( o is not IDestroyable destroyed || !destroyed.IsDestroyed )
                {
                    int idxCtor = 1;
                    do
                    {
                        parameters[1] = readInfo.TypePath[idxCtor];
                        _ctors[idxCtor].Invoke( o, parameters );
                    }
                    while( ++idxCtor < _readTypes.Length );
                }
            }

            protected override void ReadInstance( ref RefReader r )
            {
                var o = RuntimeHelpers.GetUninitializedObject( typeof( T ) );
                var d = r.SetInstance( (T)o );
                ReadInstance( d, r.ReadInfo, o );
            }
        }

        /// <inheritdoc />
        public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
        {
            if( info.DriverName == "Sliced" )
            {
                // Is the TargetType a ICKSlicedSerializable?
                if( !typeof( ICKSlicedSerializable ).IsAssignableFrom( info.TargetType ) )
                {
                    // No: it has been written as a Sliced but this has changed.
                    //     Let's try the Versioned and Simple deserialization constructor.
                    return SimpleBinaryDeserializableRegistry.TryGetOrCreateVersionedDriver( ref info ) 
                            ?? SimpleBinaryDeserializableRegistry.TryGetOrCreateSimpleDriver( ref info );
                }
                // We are on a Sliced deserialization.
                if( info.TargetType.IsValueType )
                {
                    if( info.ReadInfo.IsValueType )
                    {
                        return SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers.GetOrAdd( info.TargetType, CreateCachedValueTypeDriver );
                    }
                    var tV = typeof( SlicedDeserializerDriverVWithRef<> ).MakeGenericType( info.TargetType );
                    return (IDeserializationDriver)Activator.CreateInstance( tV, GetDeserializationCtor( info.TargetType ) )!;
                }
                // Looking for mono constructor: the base type (that may be Object) is not a ICKSlicedSerializable.
                var b = info.TargetType.BaseType;
                Debug.Assert( b != null );  
                if( !typeof( ICKSlicedSerializable ).IsAssignableFrom( b ) )
                {
                    return SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers.GetOrAdd( info.TargetType, CreateCachedMonoConstructorDriver );
                }
                // Multiple constructors case: check the TypePath and only if it matches cache the driver.
                List<ConstructorInfo> ctors = new();
                if( GetConstructorsTopDownAndCheckNominality( info.TargetType, ctors, info.ReadInfo ) )
                {
                    return SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers.GetOrAdd( info.TargetType, CreateCachedNominalDriver, ctors );
                }
                // The TypeReadInfos don't match deserialization constructors.
                // We build a driver that tries its best to associate the TypeReadInfos to the constructors and don't cache it:
                // it will be dedicated to this deserialization session.
                // By resolving the mapping here (the best we can), we optimize the run: the specific driver will have less
                // work to do for each instance of that type.
                var readTypeInfo = info.ReadInfo;
                Lazy<MissingSlicedTypeReadInfo> missing = new( () => new MissingSlicedTypeReadInfo( readTypeInfo.TypePath ) );
                var types = ctors.Select( ctor => readTypeInfo.TypePath.FirstOrDefault( i => i.TryResolveLocalType() == ctor.DeclaringType ) ?? missing.Value ).ToArray();
                var tGoodLuck = typeof( SlicedDeserializerDriverGoodLuck<> ).MakeGenericType( info.TargetType );
                return (IDeserializationDriver)Activator.CreateInstance( tGoodLuck, ctors, types )!;

            }
            return null;
        }

        IDeserializationDriver CreateCachedValueTypeDriver( Type t )
        {
            var tD = typeof( ValueTypedReaderDeserializer<> ).MakeGenericType( t );
            return (IDeserializationDriver)Activator.CreateInstance( tD, GetDeserializationCtor( t ), true )!;
        }

        IDeserializationDriver CreateCachedNominalDriver( Type t, List<ConstructorInfo> ctors )
        {
            var tV = typeof( SlicedDeserializerDriverRNominal<> ).MakeGenericType( t );
            return (IDeserializationDriver)Activator.CreateInstance( tV, ctors )!;
        }

        IDeserializationDriver CreateCachedMonoConstructorDriver( Type t )
        {
            var tV = typeof( SlicedDeserializerDriverRMonoCtor<> ).MakeGenericType( t );
            return (IDeserializationDriver)Activator.CreateInstance( tV, GetDeserializationCtor( t ) )!;
        }

        static bool GetConstructorsTopDownAndCheckNominality( Type t, List<ConstructorInfo> w, ITypeReadInfo? info )
        {
            bool isNominal = info != null && info.DriverName == "Sliced" && info.TryResolveLocalType() == t;
            var b = t.BaseType;
            Debug.Assert( b != null );
            if( b != typeof( object ) && typeof( ICKSlicedSerializable ).IsAssignableFrom( b ) )
            {
                isNominal &= GetConstructorsTopDownAndCheckNominality( b, w, info?.BaseTypeReadInfo );
            }
            w.Add( GetDeserializationCtor( t ) );
            return isNominal;
        }

        static ConstructorInfo GetDeserializationCtor( Type t )
        {
            var c = BinaryDeserializer.Helper.GetTypedReaderConstructor( t );
            if( c == null ) throw new InvalidOperationException( $"Type '{t}' requires a constructor with (IBinaryDeserializer d, ITypeReadInfo info) parameters." );
            return c;
        }
    }
}
