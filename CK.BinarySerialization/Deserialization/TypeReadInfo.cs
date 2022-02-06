using CK.Core;
using System;
using System.Collections.Generic;
using System.Reflection;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace CK.BinarySerialization
{
    public class TypeReadInfo
    {
        IDeserializationDriver<object>? _driver;
        Type? _localType;

        internal TypeReadInfo( ICKBinaryReader r )
        {
            DriverName = r.ReadSharedString();
            SerializationVersion = r.ReadSmallInt32();
            TypeNamespace = r.ReadSharedString()!;
            TypeName = r.ReadSharedString()!;
            AssemblyFullName = r.ReadSharedString()!;
        }

        internal void ConcludeRead( IBinaryDeserializer d )
        {
            var r = d.Reader;
            if( r.ReadBoolean() ) BaseTypeReadInfo = d.ReadTypeInfo();
            int l = r.ReadNonNegativeSmallInt32();
            if( l == 0 ) GenericParameters = Array.Empty<TypeReadInfo>();
            else
            {
                var t = new TypeReadInfo[l];
                for( int i = 0; i < l; i++ )
                {
                    t[i] = d.ReadTypeInfo();
                }
            }
        }

        /// <summary>
        /// Gets the name of the driver that has been resolved or null is no 
        /// driver was resolved for the type.
        /// <para>
        /// A type written by <see cref="IBinarySerializer.WriteTypeInfo(System.Type)"/> is not 
        /// necessarily serializable. This is often the case for base types of a type that is serializable.
        /// </para>
        /// </summary>
        public string? DriverName { get; }

        /// <summary>
        /// Gets the namespace of the type.
        /// </summary>
        public string TypeNamespace { get; }

        /// <summary>
        /// Gets the simple name of the type.
        /// For generic type, suffixed with a backtick and the number of generic parameters.
        /// </summary>
        public string TypeName { get; }
        
        /// <summary>
        /// Gets the full assembly name of the type (with version, culture and token).
        /// </summary>
        public string AssemblyFullName { get; }

        /// <summary>
        /// Gets the serialization version. -1 when no version is defined.
        /// </summary>
        public int SerializationVersion { get; }

        /// <summary>
        /// Gets the base type information if any.
        /// </summary>
        public TypeReadInfo? BaseTypeReadInfo { get; private set; }

        /// <summary>
        /// Gets the type informations for the generic parameters if any.
        /// </summary>
        public IReadOnlyList<TypeReadInfo> GenericParameters { get; private set; }

        /// <summary>
        /// Tries to resolve the local type.
        /// </summary>
        /// <returns>The local type if it can be resolved, null otherwise.</returns>
        public Type? TryResolveLocalType()
        {
            if( _localType == null )
            {
                try
                {
                    ResolveLocalType();
                }
                catch( Exception )
                {
                    _localType = typeof( void );
                }
            }
            return _localType != typeof(void) ? _localType : null;
        }

        /// <summary>
        /// Resolves the local type or throws a <see cref="TypeLoadException"/>.
        /// </summary>
        /// <returns>The local type.</returns>
        public Type ResolveLocalType()
        {
            if( _localType == null || _localType == typeof( void ) )
            {
                try
                {
                    var a = Assembly.Load( AssemblyFullName );
                    var s = TypeNamespace + '.' + TypeName;
                    _localType = a.GetType( s, throwOnError: true )!;
                }
                catch( Exception ex )
                {
                    throw new TypeLoadException( $"Unable to load Type for {ToString()}.", ex );
                }
            }
            return _localType;
        }

        /// <summary>
        /// Gets the deserialization driver. Throws an <see cref="InvalidOperationException"/> if it cannot be resolved.
        /// </summary>
        /// <param name="r">The deserializer.</param>
        public IDeserializationDriver<object> GetDeserializationDriver( IDeserializerResolver r )
        {
            if( _driver == null )
            {
                _driver = (IDeserializationDriver<object>?)r.TryFindDriver( this );
                if( _driver == null )
                {
                    throw new InvalidOperationException( $"Unable to resolve deserialization driver for {ToString()}." );
                }
            }
            return _driver;
        }

        public override string ToString()
        {
            return $"Type {TypeNamespace}.{TypeName} from assembly '{AssemblyFullName}', DriverName = {DriverName}";
        }

    }
}