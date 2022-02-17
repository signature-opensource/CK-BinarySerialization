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
    /// <summary>
    /// Immutable neutral description of a Type that has been written.
    /// </summary>
    public class TypeReadInfo
    {
        readonly BinaryDeserializerImpl _deserializer;
        IDeserializationDriver? _driver;
        Type? _localType;
        Mutable? _mutating;
        string _driverName;

        bool _driverLookupDone;
        private TypeReadInfo? _elementTypeReadInfo;

        internal TypeReadInfo( BinaryDeserializerImpl deserializer, TypeKind k )
        {
            _deserializer = deserializer;
            Kind = k;
            SerializationVersion = -1;
            SubTypes = Array.Empty<TypeReadInfo>();
        }

        #region Read methods called after instantiation by BinaryDeserializerImpl.ReadTypeInfo().
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

        internal void ReadBaseType()
        {
            if( _deserializer.Reader.ReadBoolean() ) BaseTypeReadInfo = _deserializer.ReadTypeInfo();
        }

        internal void ReadGenericParameters( int l )
        {
            var t = new TypeReadInfo[l];
            for( int i = 0; i < l; i++ )
            {
                t[i] = _deserializer.ReadTypeInfo();
            }
            SubTypes = t;
        }

        internal void ReadArray()
        {
            Debug.Assert( typeof( int[] ).Namespace == "System"
                            && typeof( int[] ).Assembly.GetName().Name == "System.Private.CoreLib" );
            TypeNamespace = "System";
            AssemblyName = "System.Private.CoreLib";
            ArrayRank = _deserializer.Reader.ReadSmallInt32( 1 );
            string eName;
            if( _deserializer.Reader.ReadBoolean() )
            {
                ElementTypeReadInfo = _deserializer.ReadTypeInfo();
                DriverName = _deserializer.Reader.ReadSharedString();
                eName = ElementTypeReadInfo.TypeName.Split( '+' )[^1];
            }
            else
            {
                Kind = TypeKind.OpenArray;
                eName = "T";
                _localType = typeof( Array );
            }
            TypeName = eName + '[' + new string( ',', ArrayRank - 1 ) + ']';
        }

        internal void ReadEnum()
        {
            ElementTypeReadInfo = _deserializer.ReadTypeInfo();
        }

        internal void ReadRefOrPointerInfo()
        {
            ElementTypeReadInfo = _deserializer.ReadTypeInfo();
            TypeNamespace = ElementTypeReadInfo.TypeNamespace;
            AssemblyName = ElementTypeReadInfo.AssemblyName;
            TypeName = ElementTypeReadInfo.TypeName + (Kind == TypeKind.Ref ? '&' : '*');
        }
        #endregion

        class Mutable : IMutableTypeReadInfo
        {
            readonly TypeReadInfo _info;
            bool _closed;

            public Mutable( TypeReadInfo info )
            {
                Debug.Assert( info._mutating == null );
                info._mutating = this;
                _info = info;
            }

            public TypeReadInfo ReadInfo => _info;

            public void SetDriver( IDeserializationDriver driver )
            {
                if( driver == null ) throw new ArgumentNullException( nameof( driver ) );
                if( _closed ) throw new InvalidOperationException();
                _info._driver = driver;
            }

            public void SetDriverName( string driverName )
            {
                if( driverName == null ) throw new ArgumentNullException( nameof( driverName ) );
                if( _closed ) throw new InvalidOperationException();
                _info.DriverName = driverName;
            }

            public void SetLocalType( Type t )
            {
                if( t == null ) throw new ArgumentNullException( nameof( t ) );
                if( _closed ) throw new InvalidOperationException();
                _info._localType = t;
            }

            public IDeserializationDriver? Close()
            {
                _closed = true;
                if( _info._driver != null ) _info._driverLookupDone = true;
                _info._mutating = null;
                return _info._driver;
            }
        }

        internal IMutableTypeReadInfo CreateMutation() => new Mutable( this );
        internal IDeserializationDriver? CloseMutation()
        {
            Debug.Assert( _mutating != null );
            return _mutating.Close();
        }

        /// <summary>
        /// Categories the <see cref="TypeReadInfo"/>.
        /// </summary>
        public enum TypeKind
        {
            /// <summary>
            /// Regular reference or value type. Instances may be deserialized.
            /// </summary>
            Regular,

            /// <summary>
            /// Enumeration. Instances may be deserialized.
            /// </summary>
            Enum,

            /// <summary>
            /// Array, potentially with multiple dimensions. Instances may be deserialized.
            /// </summary>
            Array,

            /// <summary>
            /// "Generic" array (see <see cref="Type.ContainsGenericParameters"/> is true).
            /// Instances cannot be deserialized.
            /// <see cref="ResolveLocalType()"/> returns the system type <see cref="System.Array"/>.
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
        /// Gets the serialization's driver name that has been resolved and potentially 
        /// used to write instance of this type.
        /// <para>
        /// Null if no serialization's driver was resolved for the type.
        /// This is totally possible since a type written by <see cref="IBinarySerializer.WriteTypeInfo(Type)"/> is not 
        /// necessarily serializable and this is often the case for base types of a type that is itself serializable
        /// (like <see cref="TypeKind.OpenGeneric"/> for instance).
        /// </para>
        /// </summary>
        public string? DriverName
        {
            get => _driverName;
            private set
            {
                if( value != null && value != _driverName )
                {
                    _driverName = value;
                    if( IsNullable = (value[value.Length - 1] == '?') )
                    {
                        NonNullableDriverName = value.Substring( 0, value.Length - 1 );
                    }
                    else
                    {
                        NonNullableDriverName = value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the non nullable driver name (without the trailing '?').
        /// </summary>
        public string? NonNullableDriverName { get; private set; }

        /// <summary>
        /// Gets whether this type information describes a nullable type.
        /// </summary>
        public bool IsNullable { get; private set; }

        /// <summary>
        /// Gets the namespace of the type.
        /// </summary>
        public string TypeNamespace { get; private set; }

        /// <summary>
        /// Gets the simple name or nested name of the type (parent nested simple type name are separated with a '+').
        /// For generic type, it is suffixed with a backtick and the number of generic parameters.
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
        /// Gets the element type information if this is an array, pointer or reference
        /// or the underlying type for an Enum.
        /// </summary>
        public TypeReadInfo? ElementTypeReadInfo 
        { 
            get => _elementTypeReadInfo; 
            private set
            {
                Debug.Assert( value != null );
                _elementTypeReadInfo = value;
                SubTypes = new[] { _elementTypeReadInfo };
            }
        }

        /// <summary>
        /// Gets the base type information if any (object and ValueType are skipped).
        /// </summary>
        public TypeReadInfo? BaseTypeReadInfo { get; private set; }

        /// <summary>
        /// Gets the type informations for the generic parameters if any or
        /// the element type information if this is an array, pointer or reference
        /// or the underlying type for an Enum.
        /// </summary>
        public IReadOnlyList<TypeReadInfo> SubTypes { get; private set; }

        /// <summary>
        /// Tries to resolve the local type.
        /// <para>
        /// Note that <see cref="TypeKind.OpenArray"/> is bound to the system typeof( <see cref="Array"/> ).
        /// </para>
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
            return _localType != typeof( void ) ? _localType : null;
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
                        var p = new Type[SubTypes.Count];
                        for( int i = 0; i < p.Length; i++ )
                        {
                            p[i] = SubTypes[i].ResolveLocalType();
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
                        Debug.Assert( Kind == TypeKind.Regular || Kind == TypeKind.Enum || Kind == TypeKind.OpenGeneric );
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
        /// Tries to resolve the deserialization driver.
        /// </summary>
        internal IDeserializationDriver? TryResolveDeserializationDriver()
        {
            if( !_driverLookupDone && _mutating == null )
            {
                _driverLookupDone = true;
                _driver = _deserializer.Context.TryFindDriver( this );
            }
            return _driver;
        }

        /// <summary>
        /// Gets the deserialization driver. Throws an <see cref="InvalidOperationException"/> if it cannot be resolved.
        /// </summary>
        internal IDeserializationDriver GetDeserializationDriver()
        {
            if( TryResolveDeserializationDriver() == null )
            {
                throw new InvalidOperationException( $"Unable to resolve deserialization driver for {ToString()}." );
            }
            Debug.Assert( _driver != null );
            return _driver;
        }

        public override string ToString()
        {
            var gen = SubTypes.Count > 0
                        ? '[' + SubTypes.Select( p => p.ToString() ).Concatenate() + ']'
                        : "";
            return $"DriverName = {DriverName}, Type '{TypeNamespace}.{TypeName}{gen},{AssemblyName}'";
        }

    }
}