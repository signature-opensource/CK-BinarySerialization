using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Factory for "Sliced" serialization drivers.
    /// </summary>
    public class SlicedSerializerFactory : ISerializerResolver
    {
        readonly SharedBinarySerializerContext _resolver;

#if NET6_0_OR_GREATER
        [ModuleInitializer]
        internal static void AutoSharedRegister()
        {
            BinarySerializer.DefaultSharedContext.Register( Default, false );
        }
#endif

        /// <summary>
        /// Initializes a new factory.
        /// </summary>
        /// <param name="resolver">The root resolver to use.</param>
        public SlicedSerializerFactory( SharedBinarySerializerContext resolver )
        {
            _resolver = resolver;
        }

        /// <inheritdoc />
        public ISerializationDriver? TryFindDriver( Type t )
        {
            if( t.ContainsGenericParameters || !typeof(ICKSlicedSerializable).IsAssignableFrom( t ) ) return null;
            return Create( t );
        }

        sealed class SlicedValueTypeSerializableDriver<T> : StaticValueTypeSerializer<T> where T : struct, ICKSlicedSerializable
        {
            public override string DriverName => "Sliced";

            public SlicedValueTypeSerializableDriver( int version )
                : base( typeof( T ) )
            {
                SerializationVersion = version;
            }

            public override int SerializationVersion { get; }
        }

        /// <summary>
        /// Serializer don't rely on base type serializers for 2 reasons: there's no gain
        /// to reuse the base serializers to call their static write method (especially with generated 
        /// delegates if we implement them) and base types may be abstract: there will be no serializer for them.
        /// If a base type must be serialized, it will have its own dedicated serializer.
        /// </summary>
        sealed class SlicedReferenceTypeSerializableDriver<T> : ReferenceTypeSerializer<T>, ISerializationDriverAllowDeferredRead where T : class
        {
            public override string DriverName => "Sliced";

            readonly List<MethodInfo> _writers;
            readonly bool _isDestroyable;

            public SlicedReferenceTypeSerializableDriver( int version, List<MethodInfo> writers, bool isDestroyable )
            {
                SerializationVersion = version;
                _writers = writers;
                _isDestroyable = isDestroyable;
            }

            public override int SerializationVersion { get; }

            protected override void Write( IBinarySerializer w, in T o )
            {
                var p = new object[] { w, o };  
                _writers[0].Invoke( null, p );
                if( _writers.Count > 1 && (!_isDestroyable || !((IDestroyable)o).IsDestroyed) )
                {
                    for( int i = 1; i < _writers.Count; i++ )
                    {
                        _writers[i].Invoke( null, p );
                    }
                }
            }
        }

        ISerializationDriver Create( Type t )
        {
            var version = SerializationVersionAttribute.GetRequiredVersion( t );
            if( t.IsValueType )
            {
                var tV = typeof( SlicedValueTypeSerializableDriver<> ).MakeGenericType( t );
                return (ISerializationDriver)Activator.CreateInstance( tV, version )!;
            }
            List<MethodInfo> writers = new();
            GetWritersTopDown( t, writers, typeof(IDestroyable).IsAssignableFrom( t ) );
            var tR = typeof( SlicedReferenceTypeSerializableDriver<> ).MakeGenericType( t );
            return ((ISerializationDriver)Activator.CreateInstance( tR, version, writers, typeof( IDestroyable ).IsAssignableFrom( t ) )!).ToNullable;
        }

        static void GetWritersTopDown( Type t, List<MethodInfo> w, bool mustBeDestroyable )
        {
            var b = t.BaseType;
            Debug.Assert( b != null );
            if( b != typeof( object )
                && typeof( ICKSlicedSerializable ).IsAssignableFrom( b ) )
            {
                bool isDestroyable = typeof( IDestroyable ).IsAssignableFrom( b );
                if( mustBeDestroyable && !isDestroyable )
                {
                    throw new InvalidOperationException( $"Type '{b}' must be a IDestroyableObject: IDestroyableObject must be implemented by the root of the ICKSlicedSerializable type hierarchy." );
                }
                GetWritersTopDown( b, w, mustBeDestroyable | isDestroyable );
            }
            w.Add( SharedBinarySerializerContext.GetStaticWriter( t, t ) );
        }

    }
}
