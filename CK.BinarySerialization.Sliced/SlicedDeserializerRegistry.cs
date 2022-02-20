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
    /// Since the synthesized drivers only depends on the local type and don't directly need any other resolvers, 
    /// a singleton cache is fine and it uses the <see cref="SharedBinaryDeserializerContext.PureLocalTypeDependentDrivers"/>.
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

        /// <inheritdoc />
        public IDeserializationDriver? TryFindDriver( ref DeserializerResolverArg info )
        {
            if( info.DriverName == "Sliced" )
            {
                if( info.LocalType.IsValueType )
                {
                    var ctor = GetDeserializationCtor( info.LocalType );
                    var tV = typeof( SlicedDeserializerDriverV<> ).MakeGenericType( info.LocalType );
                    return (IDeserializationDriver)Activator.CreateInstance( tV, ctor )!;
                }
                List<ConstructorInfo> ctors = new();
                GetConstructorsTopDown( info.LocalType, ctors );
                var tR = typeof( SlicedDeserializerDriverR<> ).MakeGenericType( info.LocalType );
                return ((IDeserializationDriver)Activator.CreateInstance( tR, ctors )!).ToNullable;
            }
            return null;
        }

        static readonly Type[] _ctorTypes = new Type[] { typeof( IBinaryDeserializer ), typeof( ITypeReadInfo ) };
        static readonly ParameterExpression[] _ctorExpressions = new ParameterExpression[] { Expression.Parameter( typeof( IBinaryDeserializer ) ), Expression.Parameter( typeof( ITypeReadInfo ) ) };

        static void GetConstructorsTopDown( Type t, List<ConstructorInfo> w )
        {
            var b = t.BaseType;
            Debug.Assert( b != null );
            if( b != typeof( object )
                && b != typeof( ValueType )
                && typeof( ICKSlicedSerializable ).IsAssignableFrom( b ) )
            {
                GetConstructorsTopDown( b, w );
            }
            w.Add( GetDeserializationCtor( t ) );
        }

        static ConstructorInfo GetDeserializationCtor( Type t )
        {
            var c = t.GetConstructor( _ctorTypes );
            if( c == null ) throw new InvalidOperationException( $"Type '{t}' requires a public constructor with (IBinaryDeserializer d, ITypeReadInfo info) parameters." );
            return c;
        }
    }
}
