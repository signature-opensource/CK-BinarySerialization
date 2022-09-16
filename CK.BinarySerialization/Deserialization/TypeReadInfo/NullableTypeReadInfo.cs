using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace CK.BinarySerialization
{
    sealed class NullableTypeReadInfo : ITypeReadInfo
    {
        ITypeReadInfo _nonNull;
        Type? _localType;
        ITypeReadInfo[]? _typePath;

        internal void Init( ITypeReadInfo nonNull ) => _nonNull = nonNull;

        public bool IsNullable => true;

        public bool IsValueType => _nonNull.IsValueType;

        public bool IsSealed => _nonNull.IsSealed;

        public ITypeReadInfo ToNonNullable => _nonNull;

        public TypeReadInfoKind Kind => _nonNull.Kind;

        public int ArrayRank => _nonNull.ArrayRank;

        public ITypeReadInfo? BaseTypeReadInfo => _nonNull.BaseTypeReadInfo;

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
                        _typePath = (ITypeReadInfo[])((ITypeReadInfo[])_nonNull.TypePath).Clone();
                        _typePath[_typePath.Length-1] = this;
                    }
                }
                return _typePath;
            }
        }

        public string? DriverName => _nonNull.DriverName;

        public int Version => _nonNull.Version;

        public IReadOnlyList<ITypeReadInfo> SubTypes => _nonNull.SubTypes;

        public string AssemblyName => _nonNull.AssemblyName;

        public string TypeName => _nonNull.TypeName;

        public string TypeNamespace => _nonNull.TypeNamespace;

        public bool HasResolvedConcreteDriver => _nonNull.HasResolvedConcreteDriver;

        public Type? TargetType => _nonNull.TargetType;

        public bool IsDirtyInfo => _nonNull.IsDirtyInfo;

        public Type? TryResolveLocalType()
        {
            if( _localType == null )
            {
                var inner = _nonNull.TryResolveLocalType();
                if( inner != null )
                {
                    if( inner.IsValueType )
                    {
                        _localType = typeof( Nullable<> ).MakeGenericType( inner );
                    }
                    else
                    {
                        _localType = inner;
                    }
                }
            }
            return _localType;
        }

        public Type ResolveLocalType()
        {
            if( _localType == null )
            {
                _localType = _nonNull.ResolveLocalType();
                if( _localType.IsValueType )
                {
                    _localType = typeof( Nullable<> ).MakeGenericType( _localType );
                }
            }
            return _localType;
        }

        public IDeserializationDriver GetConcreteDriver( Type? expected ) => _nonNull.GetConcreteDriver( expected ).ToNullable;

        public override string ToString() => '?' + _nonNull.ToString();

        public IDeserializationDriver GetPotentiallyAbstractDriver( Type? expected ) => _nonNull.GetPotentiallyAbstractDriver( expected ).ToNullable;

    }
}
