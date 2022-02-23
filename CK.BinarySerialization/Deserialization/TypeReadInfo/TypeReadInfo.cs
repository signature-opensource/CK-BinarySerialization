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
    class TypeReadInfo : ITypeReadInfo
    {
        readonly BinaryDeserializerImpl _deserializer;
        IDeserializationDriver? _driver;
        Type? _localType;
        Mutable? _mutating;
        ITypeReadInfo[]? _typePath;

        bool _hooked;
        bool _driverLookupDone;

        internal TypeReadInfo( BinaryDeserializerImpl deserializer, TypeReadInfoKind k )
        {
            _deserializer = deserializer;
            Kind = k;
            SerializationVersion = -1;
            SubTypes = Array.Empty<ITypeReadInfo>();
            IsSealed = k == TypeReadInfoKind.ValueType || k == TypeReadInfoKind.SealedClass || k == TypeReadInfoKind.GenericSealedClass;
            IsValueType = k == TypeReadInfoKind.ValueType || k == TypeReadInfoKind.GenericValueType || k == TypeReadInfoKind.Enum;
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
            var t = new ITypeReadInfo[l];
            for( int i = 0; i < l; i++ )
            {
                t[i] = _deserializer.ReadTypeInfo();
            }
            SubTypes = t;
        }

        internal void ReadArray()
        {
            Debug.Assert( Kind == TypeReadInfoKind.Array || Kind == TypeReadInfoKind.OpenArray );
            Debug.Assert( typeof( int[] ).Namespace == "System"
                            && typeof( int[] ).Assembly.GetName().Name == "System.Private.CoreLib" );
            TypeNamespace = "System";
            AssemblyName = "System.Private.CoreLib";
            ArrayRank = _deserializer.Reader.ReadSmallInt32( 1 );
            string eName;
            if( Kind == TypeReadInfoKind.Array )
            {
                var item = _deserializer.ReadTypeInfo();
                SubTypes = new[] { item };
                DriverName = _deserializer.Reader.ReadSharedString();
                eName = item.TypeName.Split( '+' )[^1];
            }
            else
            {
                eName = "T";
                _localType = typeof( Array );
            }
            TypeName = eName + '[' + new string( ',', ArrayRank - 1 ) + ']';
        }

        internal void ReadEnum()
        {
            SubTypes = new[] { _deserializer.ReadTypeInfo() };
        }

        internal void ReadRefOrPointerInfo()
        {
            var item = _deserializer.ReadTypeInfo();
            SubTypes = new[] { item };
            TypeNamespace = item.TypeNamespace;
            AssemblyName = item.AssemblyName;
            TypeName = item.TypeName + (Kind == TypeReadInfoKind.Ref ? '&' : '*');
        }
        #endregion

        class Mutable : IMutableTypeReadInfo
        {
            readonly TypeReadInfo _info;
            bool _closed;

            public Mutable( TypeReadInfo info )
            {
                Debug.Assert( info._mutating == null );
                _info = info;
            }

            public ITypeReadInfo ReadInfo => _info;

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

            public void Close()
            {
                _closed = true;
                if( _info._driver != null ) _info._driverLookupDone = true;
                _info._mutating = null;
            }
        }

        public TypeReadInfoKind Kind { get; }

        public bool IsSealed { get; }

        public string? DriverName { get; private set; }

        public bool IsNullable => false;

        public bool IsValueType { get; }
        
        public ITypeReadInfo ToNonNullable => this;

        public string TypeNamespace { get; private set; }

        public string TypeName { get; private set; }

        public string AssemblyName { get; private set; }

        public int SerializationVersion { get; private set; }

        public int ArrayRank { get; private set; }

        public ITypeReadInfo? BaseTypeReadInfo { get; private set; }

        public IReadOnlyList<ITypeReadInfo> SubTypes { get; private set; }

        public IReadOnlyList<ITypeReadInfo> TypePath 
        { 
            get
            {
                if( _typePath == null )
                {
                    if( BaseTypeReadInfo == null )
                    {
                        _typePath = new ITypeReadInfo[] { this };
                    }
                    else
                    {
                        List<ITypeReadInfo> p = new() { this };
                        var a = BaseTypeReadInfo;
                        while( a != null )
                        {
                            p.Add( a );
                            a = a.BaseTypeReadInfo;
                        }
                        _typePath = new ITypeReadInfo[p.Count];
                        p.CopyTo( _typePath );
                        Array.Reverse( _typePath );
                    }
                }
                return _typePath;
            }
        }

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
                    // Use void to remember an error.
                    _localType = typeof( void );
                }
            }
            return _localType != typeof( void ) ? _localType : null;
        }

        public Type ResolveLocalType()
        {
            if( _localType == null || _localType == typeof( void ) )
            {
                // OpenArray has a local type by design (set by read).
                Debug.Assert( Kind != TypeReadInfoKind.OpenArray );
                // Apply hooks if this is the first time.
                if( !_hooked )
                {
                    _hooked = true;
                    _mutating = new Mutable( this );
                    _deserializer.Context.Shared.CallHooks( _mutating );
                    _mutating = null;
                    if( _driver != null ) _driverLookupDone = true;
                    if( _localType != null ) return _localType;
                }
                try
                {
                    Type t;
                    if( Kind == TypeReadInfoKind.GenericValueType 
                        || Kind == TypeReadInfoKind.GenericSealedClass 
                        || Kind == TypeReadInfoKind.GenericClass )
                    {
                        t = CreateTypeFromNames();
                        var p = new Type[SubTypes.Count];
                        for( int i = 0; i < p.Length; i++ )
                        {
                            p[i] = SubTypes[i].ResolveLocalType();
                        }
                        // Handling struct to class mutation here: if this was a Nullable<>,
                        // then if the subtype became a class, then its local type is the new class.
                        if( t == typeof( Nullable<> ) && !p[0].IsValueType )
                        {
                            _localType = p[0];
                        }
                        else
                        {
                            _localType = t.MakeGenericType( p );
                        }
                    }
                    else if( Kind == TypeReadInfoKind.Array )
                    {
                        Debug.Assert( SubTypes.Count == 1 );
                        var tE = SubTypes[0].ResolveLocalType();
                        _localType = ArrayRank == 1 ? tE.MakeArrayType() : tE.MakeArrayType( ArrayRank );
                    }
                    else if( Kind == TypeReadInfoKind.Ref )
                    {
                        Debug.Assert( SubTypes.Count == 1 );
                        var tE = SubTypes[0].ResolveLocalType();
                        _localType = tE.MakeByRefType();
                    }
                    else if( Kind == TypeReadInfoKind.Pointer )
                    {
                        Debug.Assert( SubTypes.Count == 1 );
                        var tE = SubTypes[0].ResolveLocalType();
                        _localType = tE.MakePointerType();
                    }
                    else
                    {
                        Debug.Assert( Kind == TypeReadInfoKind.ValueType
                                      || Kind == TypeReadInfoKind.Class
                                      || Kind == TypeReadInfoKind.SealedClass
                                      || Kind == TypeReadInfoKind.GenericValueType
                                      || Kind == TypeReadInfoKind.GenericClass
                                      || Kind == TypeReadInfoKind.GenericSealedClass
                                      || Kind == TypeReadInfoKind.Enum 
                                      || Kind == TypeReadInfoKind.OpenGeneric );
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

        public bool HasResolvedDeserializationDriver => _driver != null;

        public IDeserializationDriver GetDeserializationDriver()
        {
            if( !_driverLookupDone )
            {
                _driverLookupDone = true;
                var a = new DeserializerResolverArg( this, _deserializer.Context.Shared );
                _driver = _deserializer.Context.TryFindDriver( ref a );
            }
            if( _driver == null )
            {
                throw new InvalidOperationException( $"Unable to resolve deserialization driver for {ToString()}." );
            }
            return _driver;
        }

        public override string ToString()
        {
            var gen = SubTypes.Count > 0
                        ? '<' + SubTypes.Select( p => p.ToString()! ).Concatenate() + '>'
                        : "";
            return $"[{DriverName}]{TypeNamespace}.{TypeName}{gen}";
        }

    }
}