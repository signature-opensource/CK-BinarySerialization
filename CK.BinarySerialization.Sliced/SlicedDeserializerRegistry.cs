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
    ///     When the local type is a ValueType, we have no other options to trust its constructor that must then be able to handle 
    ///     any "previous" writes with potentially multiple TypeReadInfo from previous base classes.
    ///     </item>
    ///     <item>
    ///     When the local type is a class with only one existing deserialization constructor, we are in the same case as for ValueTypes: it's up
    ///     to the deserialization constructor to handle the TypeReadInfo and its potential base types.
    ///     </item>
    ///     <item>
    ///     When the local type is a class with more than one deserialization constructors on its base types, it's much more complex: 
    ///     some base classes may have disappeared or appeared. Knowing which "Slice" can or should handle which TypeReadInfo is (more than) tricky. 
    ///     Whatever heuristic we use for this "mapping", the point is that during one application run as well as across multiple runs, the "nominal" workload is to deserialize the same types that has been 
    ///     serialized. 
    ///     <para>
    ///     We cache (and reuse) the deserializer only if the TypeReadInfo.TypePath's local types match the local constructor's chain.
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

#if NET6_0_OR_GREATER
        [ModuleInitializer]
        internal static void AutoSharedRegister()
        {
            BinaryDeserializer.DefaultSharedContext.Register( Instance, false );
        }
#endif

        /// <summary>
        /// Deserializer for value types. We reuse the standard ValueTypeDeserializer even if
        /// a useless intermediate call is at stake here instead of writing another deserializer 
        /// that would use the generated delegate directly.
        /// </summary>
        sealed class SlicedDeserializerDriverV<T> : ValueTypeDeserializer<T> where T : struct
        {
            readonly Func<IBinaryDeserializer, ITypeReadInfo, T> _factory;

            public SlicedDeserializerDriverV( ConstructorInfo ctor )
            {
                _factory = (Func<IBinaryDeserializer, ITypeReadInfo, T>)SimpleBinaryDeserializableRegistry.CreateNewDelegate<T>( typeof( Func<IBinaryDeserializer, ITypeReadInfo, T> ), _ctorExpressions, ctor );
            }

            protected override T ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo ) => _factory( d, readInfo );
        }

        sealed class SlicedDeserializerDriverR<T> : ReferenceTypeDeserializer<T>, IDeserializationDeferredDriver where T : class
        {
            readonly List<ConstructorInfo> _ctors;

            public SlicedDeserializerDriverR( List<ConstructorInfo> ctors )
            {
                _ctors = ctors;
            }

            public void ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo, object o )
            {
                throw new NotImplementedException();
            }

            protected override void ReadInstance( ref RefReader r )
            {
                var o = RuntimeHelpers.GetUninitializedObject( typeof( T ) );
                var d = r.SetInstance( (T)o );
                ReadInstance( d, r.ReadInfo, o );
            }
        }

        sealed class SlicedDeserializerDriverRMonoCtor<T> : ReferenceTypeDeserializer<T>, IDeserializationDeferredDriver where T : class
        {
            readonly ConstructorInfo _ctor;

            public SlicedDeserializerDriverRMonoCtor( ConstructorInfo ctor )
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
                // Challenge the IDestroyable.
                if( o is not IDestroyable destroyed || !destroyed.IsDestroyed )
                {
                    int idxCtor = 1;
                    do
                    {
                        parameters[1] = ++idxInfo;
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

        /// <inheritdoc />
        public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
        {
            if( info.DriverName == "Sliced" && typeof( ICKSlicedSerializable ).IsAssignableFrom( info.LocalType ) )
            {
                if( info.LocalType.IsValueType )
                {
                    return SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers.GetOrAdd( info.LocalType, 
                                                                                                   CreateMonoConstructorDriver, 
                                                                                                   typeof( SlicedDeserializerDriverV<> ) );
                }
                // Looking for mono constructor: the base type (that may be Object) is not a ICKSlicedSerializable.
                var b = info.LocalType.BaseType;
                Debug.Assert( b != null );  
                if( !typeof( ICKSlicedSerializable ).IsAssignableFrom( b ) )
                {
                    return SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers.GetOrAdd( info.LocalType, 
                                                                                                   CreateMonoConstructorDriver, 
                                                                                                   typeof( SlicedDeserializerDriverRMonoCtor<> ) );
                }
                // Multiple constructors case.
                List<ConstructorInfo> ctors = new();
                if( GetConstructorsTopDownAndCheckNominality( info.LocalType, ctors, info.Info ) )
                {
                    return SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers.GetOrAdd( info.LocalType, CreateNominalDriver, ctors );
                }
                // The TypeReadInfos don't match deserialization constructors.
                // We build a driver that tries its best to associate the TypeReadInfos to the constructors and don't cache it:
                // it will be dedicated to this deserialization session.
                // By resolving the mapping here (the best we can), we optimize the run: the specific driver will have less
                // work to do for each instance of that type.
            }
            return null;
        }

        IDeserializationDriver CreateNominalDriver( Type t, List<ConstructorInfo> ctors )
        {
            var tV = typeof( SlicedDeserializerDriverRNominal<> ).MakeGenericType( t );
            return (IDeserializationDriver)Activator.CreateInstance( tV, ctors )!;
        }

        IDeserializationDriver CreateMonoConstructorDriver( Type t, Type tGenD )
        {
            var ctor = GetDeserializationCtor( t );
            var tV = tGenD.MakeGenericType( t );
            return (IDeserializationDriver)Activator.CreateInstance( tV, ctor )!;
        }

        static readonly Type[] _ctorTypes = new Type[] { typeof( IBinaryDeserializer ), typeof( ITypeReadInfo ) };
        static readonly ParameterExpression[] _ctorExpressions = new ParameterExpression[] { Expression.Parameter( typeof( IBinaryDeserializer ) ), Expression.Parameter( typeof( ITypeReadInfo ) ) };

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
            var c = t.GetConstructor( _ctorTypes );
            if( c == null ) throw new InvalidOperationException( $"Type '{t}' requires a public constructor with (IBinaryDeserializer d, ITypeReadInfo info) parameters." );
            return c;
        }
    }
}
