using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Mutable cache of deserialization drivers and known objects/keys association that should be 
    /// reused for multiple, non concurrent, deserialization sessions of the same objects family.
    /// <para>
    /// This is absolutely not thread safe and can be used at any time by only one <see cref="IBinaryDeserializer"/>
    /// that must be disposed to free the context (otherwise a <see cref="InvalidOperationException"/> is raised).
    /// </para>
    /// </summary>
    public class BinaryDeserializerContext : IDeserializerResolver, IDeserializerKnownObject
    {
        readonly Dictionary<string,object> _knownObjects;
        readonly SharedBinaryDeserializerContext _shared;
        readonly SimpleServiceContainer _services;
        Dictionary<Type, IDeserializationDriver> _localTypeDrivers;
        BinaryDeserializerImpl? _deserializer;

        /// <summary>
        /// Initializes a new <see cref="BinaryDeserializerContext"/>.
        /// </summary>
        /// <param name="shared">The shared context to use.</param>
        public BinaryDeserializerContext( SharedBinaryDeserializerContext shared, IServiceProvider? services )
        {
            _knownObjects = new Dictionary<string, object>();
            _shared = shared;
            _services = new SimpleServiceContainer( services );
            _localTypeDrivers = new Dictionary<Type, IDeserializationDriver>();
        }

        /// <summary>
        /// Initializes a new <see cref="BinaryDeserializerContext"/> bound to the <see cref="BinaryDeserializer.DefaultSharedContext"/>
        /// and with empty services.
        /// </summary>
        public BinaryDeserializerContext()
            : this( BinaryDeserializer.DefaultSharedContext, null )
        {
        }

        internal void Acquire( BinaryDeserializerImpl d )
        {
            if( _deserializer != null )
            {
                throw new InvalidOperationException( "This BinaryDeserializerContext is already used by an existing BinaryDeserializer. The existing BinaryDeserializer must be disposed first." );
            }
            _deserializer = d;
        }

        internal void Release()
        {
            _deserializer = null;
        }

        public void EnsureLocalTypeDeserializer( IDeserializationDriver driver )
        {
            var n = driver.ToNullable.ResolvedType;
            _localTypeDrivers[n] = driver.ToNullable;
            var nn = driver.ToNonNullable.ResolvedType;
            if( nn != n ) _localTypeDrivers[n] = driver.ToNonNullable;
        }

        /// <summary>
        /// Raised for each <see cref="TypeReadInfo"/> read. See <see cref="IMutableTypeReadInfo"/>.
        /// <para>
        /// This event enables setting the local type to deserialize and/or the <see cref="IDeserializationDriver"/> 
        /// to use instead of calling <see cref="IDeserializerResolver.TryFindDriver(TypeReadInfo)"/>.
        /// </para>
        /// </summary>
        public event Action<IBinaryDeserializer, IMutableTypeReadInfo>? OnTypeReadInfo;

        /// <summary>
        /// Gets a mutable service container.
        /// </summary>
        public SimpleServiceContainer Services => _services;

        /// <inheritdoc />
        public IDeserializationDriver? TryFindDriver( TypeReadInfo info )
        {
            if( OnTypeReadInfo != null && _deserializer != null )
            {
                OnTypeReadInfo( _deserializer, info.CreateMutation() );
                var r = info.CloseMutation();
                if( r != null ) return r;
            }
            var localType = info.TryResolveLocalType();
            if( localType != null && _localTypeDrivers.TryGetValue( localType, out var localDriver ) )
            {
                return localDriver;
            }
            return _shared.TryFindDriver( info );
        }



        /// <inheritdoc />
        public object? GetKnownObject( string instanceKey )
        {
            if( !_knownObjects.TryGetValue( instanceKey, out var r ) )
            {
                r = _shared.KnownObjects.GetKnownObject( instanceKey );
                if( r != null ) _knownObjects.Add( instanceKey, r );
            }
            return r;
        }

        /// <inheritdoc />
        public void RegisterKnownKey( string key, object o )
        {
            if( _knownObjects.TryGetValue( key, out var oExist ) )
            {
                SharedDeserializerKnownObject.ThrowOnDuplicateKey( key, oExist );
            }
            _knownObjects.Add( key, o );
        }

        /// <inheritdoc />
        public void RegisterKnownKey( params (string key, object o)[] mapping )
        {
            foreach( var a in mapping )
            {
                RegisterKnownKey( a.key, a.o );
            }
        }
    }
}
