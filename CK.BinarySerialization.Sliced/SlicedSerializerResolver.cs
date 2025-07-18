using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace CK.BinarySerialization;

/// <summary>
/// Factory for "Sliced" serialization drivers.
/// </summary>
public sealed class SlicedSerializerResolver : ISerializerResolver
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static readonly SlicedSerializerResolver Instance = new SlicedSerializerResolver();

    SlicedSerializerResolver()
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// The <paramref name="context"/> is not used by this resolver.
    /// </remarks>
    public ISerializationDriver? TryFindDriver( BinarySerializerContext context, Type t )
    {
        if( t.ContainsGenericParameters || !typeof( ICKSlicedSerializable ).IsAssignableFrom( t ) ) return null;
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

        protected override void Write( IBinarySerializer s, in T o )
        {
            try
            {
                var p = new object[] { s, o };
                _writers[0].Invoke( null, p );
                if( _writers.Count > 1 && (!_isDestroyable || !((IDestroyable)o).IsDestroyed) )
                {
                    for( int i = 1; i < _writers.Count; i++ )
                    {
                        _writers[i].Invoke( null, p );
                    }
                }
            }
            catch( TargetInvocationException ex )
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture( ex.InnerException! ).Throw();
            }
        }
    }

    static ISerializationDriver Create( Type t )
    {
        var version = SerializationVersionAttribute.GetRequiredVersion( t );
        if( t.IsValueType )
        {
            var tV = typeof( SlicedValueTypeSerializableDriver<> ).MakeGenericType( t );
            return (ISerializationDriver)Activator.CreateInstance( tV, version )!;
        }
        List<MethodInfo> writers = new();
        GetWritersTopDown( t, writers, typeof( IDestroyable ).IsAssignableFrom( t ) );
        var tR = typeof( SlicedReferenceTypeSerializableDriver<> ).MakeGenericType( t );
        return ((ISerializationDriver)Activator.CreateInstance( tR, version, writers, typeof( IDestroyable ).IsAssignableFrom( t ) )!).Nullable;
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
                Throw.InvalidOperationException( $"Type '{b}' must be a IDestroyableObject: IDestroyableObject must be implemented by the root of the ICKSlicedSerializable type hierarchy." );
            }
            GetWritersTopDown( b, w, mustBeDestroyable | isDestroyable );
        }
        w.Add( SharedBinarySerializerContext.GetStaticWriter( t, t ) );
    }

}
