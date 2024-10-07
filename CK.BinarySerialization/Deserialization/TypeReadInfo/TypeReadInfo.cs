using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace CK.BinarySerialization;

/// <summary>
/// Immutable neutral description of a Type that has been written.
/// </summary>
sealed class TypeReadInfo : ITypeReadInfo
{
    readonly BinaryDeserializerImpl _deserializer;
    IDeserializationDriver? _driver;
    Type? _targetType;
    Type? _localType;
    Mutable? _mutating;
    ITypeReadInfo[]? _typePath;
    IDeserializationDriver? _abstractDriver;
    string? _overriddenDriverName;
    bool? _isDirtyInfo;

    bool _hooked;
    bool _driverLookupDone;
    bool _dirtyComputing;

    internal TypeReadInfo( BinaryDeserializerImpl deserializer, TypeReadInfoKind k )
    {
        _deserializer = deserializer;
        Kind = k;
        Version = -1;
        SubTypes = Array.Empty<ITypeReadInfo>();
        IsValueType = k == TypeReadInfoKind.ValueType || k == TypeReadInfoKind.GenericValueType || k == TypeReadInfoKind.Enum;
        IsSealed = k != TypeReadInfoKind.Class && k != TypeReadInfoKind.GenericClass && k != TypeReadInfoKind.Interface;
    }

    #region Read methods called after instantiation by BinaryDeserializerImpl.ReadTypeInfo().
    internal void ReadNames( ICKBinaryReader r )
    {
        if( (OriginalDriverName = r.ReadSharedString()) != null )
        {
            Version = r.ReadSmallInt32();
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
            OriginalDriverName = _deserializer.Reader.ReadSharedString();
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

    sealed class Mutable : IMutableTypeReadInfo, IMutableTypeReadInfo.IWrittenInfo
    {
        readonly TypeReadInfo _info;
        readonly string _assemblyName;
        readonly string _typeName;
        readonly string _typeNamespace;
        bool _closed;

        public Mutable( TypeReadInfo info )
        {
            Debug.Assert( info._mutating == null );
            _info = info;
            _assemblyName = _info.AssemblyName;
            _typeName = _info.TypeName;
            _typeNamespace = _info.TypeNamespace;
        }

        public IMutableTypeReadInfo.IWrittenInfo WrittenInfo => this;

        TypeReadInfoKind IMutableTypeReadInfo.IWrittenInfo.Kind => _info.Kind;

        bool IMutableTypeReadInfo.IWrittenInfo.IsValueType => _info.IsValueType;

        bool IMutableTypeReadInfo.IWrittenInfo.IsSealed => _info.IsSealed;

        int IMutableTypeReadInfo.IWrittenInfo.ArrayRank => _info.ArrayRank;

        ITypeReadInfo? IMutableTypeReadInfo.IWrittenInfo.BaseTypeReadInfo => _info.BaseTypeReadInfo;

        string? IMutableTypeReadInfo.IWrittenInfo.DriverName => _info.OriginalDriverName;

        int IMutableTypeReadInfo.IWrittenInfo.Version => _info.Version;

        IReadOnlyList<ITypeReadInfo> IMutableTypeReadInfo.IWrittenInfo.SubTypes => _info.SubTypes;

        string IMutableTypeReadInfo.IWrittenInfo.AssemblyName => _assemblyName;

        string IMutableTypeReadInfo.IWrittenInfo.TypeName => _typeName;

        string IMutableTypeReadInfo.IWrittenInfo.TypeNamespace => _typeNamespace;

        public IDeserializationDriver? CurentDriver => _info._driver;

        public void SetDriver( IDeserializationDriver driver )
        {
            Throw.CheckNotNullArgument( driver );
            Throw.CheckState( !_closed );
            _info._driver = driver;
        }

        public string? CurentDriverName => _info._overriddenDriverName ?? _info.OriginalDriverName;

        public void SetDriverName( string driverName )
        {
            Throw.CheckNotNullArgument( driverName );
            Throw.CheckState( !_closed );
            _info._overriddenDriverName = driverName != _info.OriginalDriverName ? driverName : null;
        }

        public Type? CurentTargetType => _info._targetType;

        public void SetTargetType( Type t )
        {
            Throw.CheckNotNullArgument( t );
            Throw.CheckState( !_closed );
            _info._targetType = t;
        }

        public string CurentTypeNamespace => _info.TypeNamespace;

        public void SetLocalTypeNamespace( string typeNamespace )
        {
            Throw.CheckNotNullArgument( typeNamespace );
            Throw.CheckState( !_closed );
            _info.TypeNamespace = typeNamespace;
        }

        public string CurentAssemblyName => _info.AssemblyName;

        public void SetLocalTypeAssemblyName( string assemblyName )
        {
            Throw.CheckNotNullArgument( assemblyName );
            Throw.CheckState( !_closed );
            _info.AssemblyName = assemblyName;
        }

        public string CurentTypeName => _info.TypeName;

        public void SetLocalTypeName( string typeName )
        {
            Throw.CheckNotNullArgument( typeName );
            Throw.CheckState( !_closed );
            _info.TypeName = typeName;
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

    public string? OriginalDriverName { get; private set; }

    public string? DriverName => _overriddenDriverName ?? OriginalDriverName;

    public bool IsNullable => false;

    public bool IsValueType { get; }

    public ITypeReadInfo ToNonNullable => this;

    public string TypeNamespace { get; private set; }

    public string TypeName { get; private set; }

    public string AssemblyName { get; private set; }

    public int Version { get; private set; }

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

    public bool IsDirtyInfo
    {
        get
        {
            if( !_isDirtyInfo.HasValue )
            {
                // Prevents reentrancy when resolving BaseTypeReadInfo.IsDirtyInfo.
                if( _dirtyComputing ) return false;
                _dirtyComputing = true;
                if( _overriddenDriverName == null )
                {
                    // TryResolveLocalType may have updated _isDirtyInfo.
                    var local = TryResolveLocalType();
                    if( !_isDirtyInfo.HasValue
                        && local != null
                        && local.IsValueType == IsValueType
                        && local.IsSealed == IsSealed
                        && (BaseTypeReadInfo == null || !BaseTypeReadInfo.IsDirtyInfo) )
                    {
                        _isDirtyInfo = false;
                    }
                    else
                    {
                        _isDirtyInfo = true;
                    }
                }
                else
                {
                    _isDirtyInfo = true;
                }
                _dirtyComputing = false;
            }
            return _isDirtyInfo.Value;
        }
    }

    public Type? TargetType
    {
        get
        {
            if( !_hooked ) ApplyHook();
            return _targetType ?? TryResolveLocalType();
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
            if( !_hooked ) ApplyHook();
            try
            {
                Type t;
                if( Kind == TypeReadInfoKind.GenericValueType
                    || Kind == TypeReadInfoKind.GenericSealedClass
                    || Kind == TypeReadInfoKind.GenericClass
                    || Kind == TypeReadInfoKind.GenericInterface )
                {
                    t = CreateTypeFromNames();
                    var p = new Type[SubTypes.Count];
                    for( int i = 0; i < p.Length; i++ )
                    {
                        var subType = SubTypes[i];
                        p[i] = subType.ResolveLocalType();
                        if( subType.IsDirtyInfo ) _isDirtyInfo = true;
                    }
                    // Handling struct to class mutation here: if this was a Nullable<>,
                    // then if the subtype became a class, then its local type is the new class.
                    if( t == typeof( Nullable<> ) && !p[0].IsValueType )
                    {
                        _isDirtyInfo = true;
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
                    var subType = SubTypes[0];
                    if( subType.IsDirtyInfo ) _isDirtyInfo = true;
                    var tE = subType.ResolveLocalType();
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
                                  || Kind == TypeReadInfoKind.Interface
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

    void ApplyHook()
    {
        Debug.Assert( !_hooked );
        _hooked = true;
        _mutating = new Mutable( this );
        try
        {
            _deserializer.Context.Shared.CallHooks( _mutating );
        }
        finally
        {
            _mutating.Close();
        }
    }

    public bool HasResolvedConcreteDriver => _driver != null;

    public IDeserializationDriver GetConcreteDriver( Type? expected )
    {
        // If we have an expected type, then we must find a driver for it.
        // But before resolving it, we make sure that our local TargetType (if it exists)
        // is not the best choice: when expected is compatible with the TargetType, we
        // simply forget it.
        // It's useless to take any base type into account (we don't want to handle and
        // may be cache a IReadOnlyList<int> deserializer: the List<int> is fine).
        // Another case where we don't want to honor the expectedType is when this expected type
        // is the locally resolved type: this type is either ourTarget or is being globally
        // mutated.
        var ourTarget = TargetType;
        if( expected != null
            && ourTarget != null
            && expected != TryResolveLocalType()
            && !expected.IsAssignableFrom( ourTarget ) )
        {
            var a = new DeserializerResolverArg( this, _deserializer.Context, expected );
            var driver = _deserializer.Context.TryFindDriver( ref a );
            if( driver == null )
            {
                Throw.InvalidOperationException( $"Unable to resolve a deserialization driver for '{expected.FullName}' from read info: {ToString()}." );
            }
            // We found a driver for the expected type.
            // We don't cache it: it is an adapter, not the "nominal" one for our target.
            return driver;
        }
        // We have no expected type here. We have no other choice than to find a driver
        // for our target (and cache it). 
        if( !_driverLookupDone )
        {
            _driverLookupDone = true;
            // This will throw if the local type cannot be resolved.
            if( ourTarget == null ) ourTarget = ResolveLocalType();
            var a = new DeserializerResolverArg( this, _deserializer.Context, ourTarget );
            _driver = _deserializer.Context.TryFindDriver( ref a );
        }
        if( _driver == null )
        {
            Throw.InvalidOperationException( $"Unable to resolve deserialization driver for {ToString()}." );
        }
        return _driver;
    }

    public IDeserializationDriver GetPotentiallyAbstractDriver( Type? expected )
    {
        if( !IsSealed )
        {
            return _abstractDriver ??= _deserializer.Context.GetAbstractDriver( TargetType ?? ResolveLocalType() );
        }
        return GetConcreteDriver( expected );
    }


    public override string ToString()
    {
        var gen = Kind != TypeReadInfoKind.Array
                  && Kind != TypeReadInfoKind.Enum
                  && Kind != TypeReadInfoKind.Ref
                  && Kind != TypeReadInfoKind.Pointer
                  && SubTypes.Count > 0
                    ? '<' + SubTypes.Select( p => p.ToString()! ).Concatenate() + '>'
                    : "";
        return $"[{DriverName}]{TypeNamespace}.{TypeName}{gen}";
    }

}
