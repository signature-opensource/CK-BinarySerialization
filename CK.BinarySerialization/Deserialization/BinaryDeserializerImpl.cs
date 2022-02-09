using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization
{
    class BinaryDeserializerImpl : IBinaryDeserializer
    {
        readonly ICKBinaryReader _reader;
        readonly IDeserializerResolver _resolver;
        readonly List<TypeReadInfo> _types;
        readonly List<object> _objects;

        Stack<(IDeserializationDeferredDriver D, TypeReadInfo T, object O)>? _deferred;
        int _recurseCount;

        int _debugModeCounter;
        int _debugSentinel;
        string? _lastWriteSentinel;
        string? _lastReadSentinel;
        Stack<string>? _debugContext;
        public const string ExceptionPrefixContext = "[WithContext]";

        bool _leaveOpen;

        internal BinaryDeserializerImpl( int version, ICKBinaryReader reader, bool leaveOpen, IDeserializerResolver resolver, IServiceProvider? services )
        {
            SerializationVersion = version;
            _reader = reader;
            _leaveOpen = leaveOpen;
            _resolver = resolver;
            Services = new SimpleServiceContainer( services );
            _types = new List<TypeReadInfo>();
            _objects = new List<object>();
        }

        public void Dispose()
        {
            if( !_leaveOpen && _reader is IDisposable d )
            {
                _leaveOpen = true;
                d.Dispose();
            }
        }

        public ICKBinaryReader Reader => _reader;

        public IServiceProvider Services { get; }

        public int SerializationVersion { get; }

        public object ReadAny()
        {
            var o = ReadAnyNullable();
            if( o == null ) ThrowInvalidDataException( "Expected non null object or value type." );
            return o!;
        }

        public object? ReadAnyNullable()
        {
            var b = (SerializationMarker)_reader.ReadByte();
            if( b == SerializationMarker.Null ) return null;
            if( b == SerializationMarker.Type )
            {
                return ReadTypeInfo().ResolveLocalType();
            }
            if( b == SerializationMarker.ObjectRef )
            {
                int idx = _reader.ReadInt32();
                if( idx >= _objects.Count )
                {
                    ThrowInvalidDataException( $"Unable to resolve reference {idx}. Current is {_objects.Count}." );
                }
                if( _objects[idx] == null )
                {
                    ThrowInvalidDataException( $"Unable to resolve reference {idx}. Object has not been created or has not been registered." );
                }
                return _objects[idx];
            }
            if( b == SerializationMarker.EmptyObject )
            {
                var o = new object();
                _objects.Add( o );
                return o;
            }
            Debug.Assert( b == SerializationMarker.DeferredObject || b == SerializationMarker.Object || b == SerializationMarker.Struct );
            var info = ReadTypeInfo();
            var d = info.GetDeserializationDriver( _resolver );
            object result;
            if( b == SerializationMarker.DeferredObject )
            {
                if( !(d is IDeserializationDeferredDriver defer) )
                {
                    ThrowInvalidDataException( $"Type '{info.TypeName}' has been serialized as a deferred object but its deserializer ({d.GetType().FullName}) is not a {nameof( IDeserializationDeferredDriver )}." );
                    return null!; // never
                }
                if( _deferred == null ) _deferred = new Stack<(IDeserializationDeferredDriver D, TypeReadInfo T, object O)>( 100 );

                result = defer.Allocate( this, info );
                _deferred.Push( (defer, info, result) );
            }
            else
            {
                ++_recurseCount;
                result = d.ReadInstance( this, info );
                --_recurseCount;
            }
            if( _recurseCount == 0 && _deferred != null )
            {
                while( _deferred.TryPop( out var s ) )
                {
                    ++_recurseCount;
                    s.D.ReadInstance( this, s.T, s.O );
                    --_recurseCount;
                }
            }
            Debug.Assert( result.GetType().IsClass == !(result is ValueType) );
            return result;
        }

        public TypeReadInfo ReadTypeInfo()
        {
            int idx = _reader.ReadNonNegativeSmallInt32();
            if( idx < _types.Count ) return _types[idx];
            var k = _reader.ReadByte();
            switch( k )
            {
                case 0: 
                    {
                        var t = new TypeReadInfo( TypeReadInfo.TypeKind.Regular );
                        _types.Add( t );
                        t.ReadNames( _reader );
                        t.ReadBaseType( this );
                        return t;
                    }
                case 1:
                    {
                        var args = _reader.ReadNonNegativeSmallInt32();
                        TypeReadInfo t;
                        t = new TypeReadInfo( args == 0 ? TypeReadInfo.TypeKind.OpenGeneric : TypeReadInfo.TypeKind.Generic );
                        _types.Add( t );
                        if( args != 0 ) t.ReadGenericParameters( this, args );
                        t.ReadNames( _reader );
                        t.ReadBaseType( this );
                        return t;
                    }
                case 2:
                    {
                        var t = new TypeReadInfo( TypeReadInfo.TypeKind.Array );
                        _types.Add( t );
                        t.ReadArray( this );
                        return t;
                    }
                case 3:
                    {
                        var t = new TypeReadInfo( TypeReadInfo.TypeKind.Ref );
                        _types.Add( t );
                        t.ReadRefOrPointerInfo( this );
                        return t;
                    }
                case 4:
                    {
                        var t = new TypeReadInfo( TypeReadInfo.TypeKind.Pointer );
                        _types.Add( t );
                        t.ReadRefOrPointerInfo( this );
                        return t;
                    }
                default: throw new NotSupportedException();
            }
        }

        public T ReadObject<T>() where T : class => (T)ReadAny();

        public T? ReadNullableObject<T>() where T : class => (T?)ReadAnyNullable();

        public T ReadValue<T>() where T : struct
        {
            var b = (SerializationMarker)_reader.ReadByte();
            return DoReadValue<T>( b );
        }

        public T? ReadNullableValue<T>() where T : struct
        {
            var b = (SerializationMarker)_reader.ReadByte();
            if( b == SerializationMarker.Null ) return default;
            return DoReadValue<T>( b );
        }

        T DoReadValue<T>( SerializationMarker b ) where T : struct
        {
            if( b != SerializationMarker.Struct )
            {
                ThrowInvalidDataException( $"Unexpected '{b}' marker while reading non nullable '{typeof( T )}'." );
            }
            var info = ReadTypeInfo();
            var d = (IDeserializationDriver<T>)info.GetDeserializationDriver( _resolver );
            return d.ReadInstance( this, info );
        }

        #region DebugMode methods

        public bool IsDebugMode => _debugModeCounter > 0;

        public bool DebugReadMode()
        {
            switch( _reader.ReadByte() )
            {
                case 182: ++_debugModeCounter; break;
                case 181: --_debugModeCounter; break;
                case 180: break;
                default: ThrowInvalidDataException( $"Expected DebugMode byte marker." ); break;
            }
            return IsDebugMode;
        }

        public void DebugCheckSentinel( [CallerFilePath] string? fileName = null, [CallerLineNumber] int line = 0 )
        {
            if( !IsDebugMode ) return;
            bool success = false;
            Exception? e = null;
            try
            {
                success = _reader.ReadInt32() == 987654321
                          && _reader.ReadInt32() == _debugSentinel
                          && (_lastWriteSentinel = _reader.ReadString()) != null;
            }
            catch( Exception ex )
            {
                e = ex;
            }
            if( !success )
            {
                var msg = $"Sentinel check failure: expected reading sentinel n°{_debugSentinel} at {fileName}({line}). Last successful sentinel was written at {_lastWriteSentinel} and read at {_lastReadSentinel}.";
                ThrowInvalidDataException( msg, e );
            }
            ++_debugSentinel;
            _lastReadSentinel = fileName + '(' + line.ToString() + ')';
        }

        public IDisposable? OpenDebugPushContext( string ctx )
        {
            if( IsDebugMode )
            {
                if( _debugContext == null ) _debugContext = new Stack<string>();
                _debugContext.Push( ctx );
                return Core.Util.CreateDisposableAction( () => _debugContext.Pop() );
            }
            return null;
        }

        void ThrowInvalidDataException( string message, Exception? inner = null )
        {
            StringBuilder b = new StringBuilder();
            b.Append( ExceptionPrefixContext ).Append( message )
             .Append( " => Objects read: " )
             .Append( _objects.Count );
            if( _debugContext != null )
            {
                foreach( var m in _debugContext )
                {
                    b.Append( " > " ).Append( m );
                }
            }
            throw new InvalidDataException( b.ToString(), inner );
        }

        #endregion
    }
}
