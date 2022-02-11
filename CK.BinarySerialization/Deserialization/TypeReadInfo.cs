using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace CK.BinarySerialization
{
    public class TypeReadInfo
    {
        IDeserializationDriver? _driver;
        Type? _localType;
        bool _driverLookupDone;

        internal TypeReadInfo( TypeKind k )
        {
            Kind = k;
            SerializationVersion = -1;
            GenericParameters = Array.Empty<TypeReadInfo>();
        }

        internal void ReadNames( ICKBinaryReader r )
        {
            if( (DriverName = r.ReadSharedString()) != null )
            {
                SerializationVersion = r.ReadSmallInt32();
            }
            TypeNamespace = r.ReadSharedString()!;
            TypeName = r.ReadString()!;
            AssemblyName = r.ReadSharedString()!;
        }

        internal void ReadBaseType( IBinaryDeserializer d )
        {
            if( d.Reader.ReadBoolean() ) BaseTypeReadInfo = d.ReadTypeInfo();
        }

        internal void ReadGenericParameters( IBinaryDeserializer d, int l )
        {
            var t = new TypeReadInfo[l];
            for( int i = 0; i < l; i++ )
            {
                t[i] = d.ReadTypeInfo();
            }
            GenericParameters = t;
        }

        internal void ReadArray( IBinaryDeserializer d )
        {
            Debug.Assert( typeof( int[] ).Namespace == "System"
                            && typeof( int[] ).Assembly.GetName().Name == "System.Private.CoreLib" );
            TypeNamespace = "System";
            AssemblyName = "System.Private.CoreLib";
            ArrayRank = d.Reader.ReadSmallInt32( 1 );
            string eName;
            if( d.Reader.ReadBoolean() )
            {
                ElementTypeReadInfo = d.ReadTypeInfo();
                DriverName = d.Reader.ReadSharedString();
                eName = ElementTypeReadInfo.TypeName.Split( '+' )[^1];
            }
            else
            {
                Kind = TypeKind.OpenArray;
                eName = "T";
                _localType = typeof(Array);
            }
            TypeName = eName + '[' + new string( ',', ArrayRank - 1 ) + ']';
        }

        internal void ReadRefOrPointerInfo( IBinaryDeserializer d )
        {
            ElementTypeReadInfo = d.ReadTypeInfo();
            TypeNamespace = ElementTypeReadInfo.TypeNamespace;
            AssemblyName = ElementTypeReadInfo.AssemblyName;
            TypeName = ElementTypeReadInfo.TypeName + (Kind == TypeKind.Ref ? '&' : '*');
        }

        /// <summary>
        /// Categories the <see cref="TypeReadInfo"/>.
        /// </summary>
        public enum TypeKind
        {
            /// <summary>
            /// Regular object. Instances may be deserialized.
            /// </summary>
            Regular,

            /// <summary>
            /// Array, potentially with multiple dimensions. Instances may be deserialized.
            /// </summary>
            Array,

            /// <summary>
            /// "Generic" array (see <see cref="Type.ContainsGenericParameters"/> is true).
            /// Instances cannot be deserialized.
            /// </summary>
            OpenArray,

            /// <summary>
            /// <see cref="Type.IsPointer"/> type.
            /// Instances cannot be deserialized.
            /// </summary>
            Pointer,

            /// <summary>
            /// <see cref="Type.IsByRef"/> type.
            /// Instances cannot be deserialized.
            /// </summary>
            Ref,

            /// <summary>
            /// Generic closed type. Instances may be deserialized.
            /// </summary>
            Generic,

            /// <summary>
            /// Open generic type. Instances cannot be deserialized.
            /// </summary>
            OpenGeneric
        }

        /// <summary>
        /// Gets the kind of this type.
        /// </summary>
        public TypeKind Kind { get; private set; }

        /// <summary>
        /// Gets the name of the driver that has been resolved or null if no 
        /// driver was resolved for the type.
        /// <para>
        /// A type written by <see cref="IBinarySerializer.WriteTypeInfo(Type)"/> is not 
        /// necessarily serializable. This is often the case for base types of a type that is serializable.
        /// </para>
        /// </summary>
        public string? DriverName { get; private set; }

        /// <summary>
        /// Gets the namespace of the type.
        /// </summary>
        public string TypeNamespace { get; private set; }

        /// <summary>
        /// Gets the simple name or nested name of the type (parent nested simple type name are separated with a '+').
        /// For generic type, suffixed with a backtick and the number of generic parameters.
        /// </summary>
        public string TypeName { get; private set; }
        
        /// <summary>
        /// Gets the simple assembly name of the type (without version, culture, etc.).
        /// </summary>
        public string AssemblyName { get; private set; }

        /// <summary>
        /// Gets the serialization version. -1 when no version is defined.
        /// </summary>
        public int SerializationVersion { get; private set; }

        /// <summary>
        /// Gets the rank of the array if this is an array.
        /// </summary>
        public int ArrayRank { get; private set; }

        /// <summary>
        /// Gets the element type information if this is an array, pointer or reference.
        /// </summary>
        public TypeReadInfo? ElementTypeReadInfo { get; private set; }

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
        /// <see cref="TypeKind.OpenArray"/> is bound to the system typeof( <see cref="Array"/> ).
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
                // OpenArray has a local type by design (set by read).
                Debug.Assert( Kind != TypeKind.OpenArray );
                try
                {
                    Type t;
                    if( Kind == TypeKind.Generic )
                    {
                        t = CreateTypeFromNames();
                        var p = new Type[GenericParameters.Count];
                        for( int i = 0; i < p.Length; i++ )
                        {
                            p[i] = GenericParameters[i].ResolveLocalType();
                        }
                        _localType = t.MakeGenericType( p );
                    }
                    else if( Kind == TypeKind.Array )
                    {
                        Debug.Assert( ElementTypeReadInfo != null );
                        var tE = ElementTypeReadInfo.ResolveLocalType();
                        _localType = ArrayRank == 1 ? tE.MakeArrayType() : tE.MakeArrayType( ArrayRank );
                    }
                    else if( Kind == TypeKind.Ref )
                    {
                        Debug.Assert( ElementTypeReadInfo != null );
                        var tE = ElementTypeReadInfo.ResolveLocalType();
                        _localType = tE.MakeByRefType();
                    }
                    else if( Kind == TypeKind.Pointer )
                    {
                        Debug.Assert( ElementTypeReadInfo != null );
                        var tE = ElementTypeReadInfo.ResolveLocalType();
                        _localType = tE.MakePointerType();
                    }
                    else
                    {
                        Debug.Assert( Kind == TypeKind.Regular || Kind == TypeKind.OpenGeneric );
                        _localType = CreateTypeFromNames();
                    }
                }
                catch( Exception ex )
                {
                    _localType = typeof( void );
                    throw new TypeLoadException( $"Unable to load Type for {ToString()}.", ex );
                }
            }
            return _localType;

            Type CreateTypeFromNames()
            {
                Type t;
                var a = Assembly.Load( AssemblyName );
                var s = TypeNamespace + '.' + TypeName;
                t = a.GetType( s, throwOnError: true )!;
                return t;
            }
        }

        /// <summary>
        /// Gets the deserialization driver. Throws an <see cref="InvalidOperationException"/> if it cannot be resolved.
        /// </summary>
        /// <param name="r">The deserializer.</param>
        internal IDeserializationDriver GetDeserializationDriver( IDeserializerResolver r )
        {
            if( TryGetDeserializationDriver( r ) == null )
            {
                throw new InvalidOperationException( $"Unable to resolve deserialization driver for {ToString()}." );
            }
            Debug.Assert( _driver != null );
            return _driver;
        }

        /// <summary>
        /// Tries to get the deserialization driver.
        /// </summary>
        /// <param name="r">The deserializer.</param>
        internal IDeserializationDriver? TryGetDeserializationDriver( IDeserializerResolver r )
        {
            if( !_driverLookupDone )
            {
                _driverLookupDone = true;
                _driver = r.TryFindDriver( this );
            }
            return _driver;
        }

        public override string ToString()
        {
            var gen = GenericParameters.Count > 0
                        ? '[' + GenericParameters.Select( p => p.ToString() ).Concatenate() + ']'
                        : "";
            return $"DriverName = {DriverName}, Type '{TypeNamespace}.{TypeName}{gen},{AssemblyName}'";
        }

    }
}