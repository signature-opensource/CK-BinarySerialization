using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization
{
    class BinaryDeserializerImpl : IDisposableBinaryDeserializer
    {
        readonly ICKBinaryReader _reader;
        readonly BinaryDeserializerContext _context;
        readonly List<ITypeReadInfo> _types;
        readonly List<object> _objects;

        Stack<(IDeserializationDeferredDriver D, ITypeReadInfo T, object O)>? _deferred;
        int _recurseCount;

        int _debugModeCounter;
        int _debugSentinel;
        string? _lastWriteSentinel;
        string? _lastReadSentinel;
        Stack<string>? _debugContext;
        public const string ExceptionPrefixContext = "[WithContext]";

        bool _sameEndianness;
        bool _leaveOpen;

        internal BinaryDeserializerImpl( int version,
                                         ICKBinaryReader reader,
                                         bool leaveOpen,
                                         BinaryDeserializerContext context,
                                         bool sameEndianness )
        {
            (_context = context).Acquire( this );
            SerializerVersion = version;
            _reader = reader;
            _leaveOpen = leaveOpen;
            _types = new List<ITypeReadInfo>();
            _objects = new List<object>();
            _sameEndianness = sameEndianness;
        }

        public void Dispose()
        {
            _context.Release();
            if( !_leaveOpen && _reader is IDisposable d )
            {
                _leaveOpen = true;
                d.Dispose();
            }
        }

        public ICKBinaryReader Reader => _reader;

        public BinaryDeserializerContext Context => _context;

        public int SerializerVersion { get; }

        public object ReadAny()
        {
            var o = ReadAnyNullable();
            if( o == null ) ThrowInvalidDataException( "Expected non null object or value type." );
            return o!;
        }

        internal int PreTrack()
        {
            _objects.Add( null! );
            return _objects.Count - 1;
        }

        internal IBinaryDeserializer Track( object o )
        {
            _objects.Add( o );
            return this;
        }

        internal IBinaryDeserializer PostTrack( int idx, object o )
        {
            _objects[idx] = o;
            return this;
        }

        internal object ReadObjectRef()
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

        public object? ReadAnyNullable()
        {
            var b = (SerializationMarker)_reader.ReadByte();
            switch( b )
            {
                case SerializationMarker.Null: return null;
                case SerializationMarker.Type: return ReadTypeInfo().ResolveLocalType();
                case SerializationMarker.ObjectRef:
                    {
                        return ReadObjectRef();
                    }
                case SerializationMarker.EmptyObject:
                    {
                        var o = new object();
                        _objects.Add( o );
                        return o;
                    }
                case SerializationMarker.KnownObject:
                    {
                        var key = _reader.ReadString()!;
                        object? o = _context.GetKnownObject( key );
                        if( o == null )
                        {
                            ThrowInvalidDataException( $"Known Object key '{key}' cannot be resolved." );
                            return null; // never.
                        }
                        _objects.Add( o );
                        return o;
                    }
            }
            Debug.Assert( b == SerializationMarker.DeferredObject || b == SerializationMarker.Object || b == SerializationMarker.Struct );
            var info = ReadTypeInfo();
            var d = (IDeserializationDriverInternal)info.GetConcreteDriver().ToNonNullable;
            object result;
            if( b == SerializationMarker.DeferredObject )
            {
                if( !(d is IDeserializationDeferredDriver defer) )
                {
                    ThrowInvalidDataException( $"Type '{info.TypeName}' has been serialized as a deferred object but its deserializer ({d.GetType().FullName}) is not a {nameof( IDeserializationDeferredDriver )}." );
                    return null!; // never
                }
                if( _deferred == null ) _deferred = new Stack<(IDeserializationDeferredDriver D, ITypeReadInfo T, object O)>( 100 );

                result = RuntimeHelpers.GetUninitializedObject( d.ResolvedType );
                _deferred.Push( (defer, info, result) );
            }
            else
            {
                ++_recurseCount;
                result = d.ReadObjectData( this, info );
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


        public ITypeReadInfo ReadTypeInfo()
        {
            int idx = _reader.ReadNonNegativeSmallInt32();
            if( idx < _types.Count ) return _types[idx];
            var nMark = _reader.ReadByte();
            switch( nMark )
            {
                case (byte)'?':
                    {
                        var t = new NullableTypeReadInfo();
                        _types.Add( t );
                        t.Init( ReadTypeInfo() );
                        return t;
                    }
                case (byte)'E':
                    {
                        var t = new TypeReadInfo( this, TypeReadInfoKind.Enum );
                        _types.Add( t );
                        t.ReadEnum();
                        t.ReadNames( _reader );
                        return t;
                    }
                case (byte)'a':
                case (byte)'A':
                    {
                        var t = new TypeReadInfo( this, nMark == (byte)'A' ? TypeReadInfoKind.OpenArray : TypeReadInfoKind.Array );
                        _types.Add( t );
                        t.ReadArray();
                        return t;
                    }
                case (byte)'O':
                    {
                        TypeReadInfo t = new TypeReadInfo( this, TypeReadInfoKind.OpenGeneric );
                        _types.Add( t );
                        t.ReadNames( _reader );
                        t.ReadBaseType();
                        return t;
                    }
                case (byte)'v':
                case (byte)'c':
                case (byte)'s':
                    {
                        var k = nMark switch
                        {
                            (byte)'v' => TypeReadInfoKind.ValueType,
                            (byte)'c' => TypeReadInfoKind.Class,
                            (byte)'s' => TypeReadInfoKind.SealedClass,
                            _ => throw new NotSupportedException()
                        };
                        var t = new TypeReadInfo( this, k );
                        _types.Add( t );
                        t.ReadNames( _reader );
                        if( k != TypeReadInfoKind.ValueType ) t.ReadBaseType();
                        return t;
                    }
                case (byte)'V':
                case (byte)'C':
                case (byte)'S':
                    {
                        var k = nMark switch
                        {
                            (byte)'V' => TypeReadInfoKind.GenericValueType,
                            (byte)'C' => TypeReadInfoKind.GenericClass,
                            (byte)'S' => TypeReadInfoKind.GenericSealedClass,
                            _ => throw new NotSupportedException()
                        };
                        TypeReadInfo t = new TypeReadInfo( this, k );
                        var args = _reader.ReadNonNegativeSmallInt32();
                        _types.Add( t );
                        if( args != 0 ) t.ReadGenericParameters( args );
                        t.ReadNames( _reader );
                        if( k != TypeReadInfoKind.GenericValueType ) t.ReadBaseType();
                        return t;
                    }
                case (byte)'R':
                    {
                        var t = new TypeReadInfo( this, TypeReadInfoKind.Ref );
                        _types.Add( t );
                        t.ReadRefOrPointerInfo();
                        return t;
                    }
                case (byte)'P':
                    {
                        var t = new TypeReadInfo( this, TypeReadInfoKind.Pointer );
                        _types.Add( t );
                        t.ReadRefOrPointerInfo();
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
            if( b != SerializationMarker.Struct && b != SerializationMarker.Object )
            {
                ThrowInvalidDataException( $"Unexpected '{b}' marker while reading non nullable '{typeof( T )}'." );
            }
            var info = ReadTypeInfo();
            var d = (IValueTypeNonNullableDeserializationDriver<T>)info.GetConcreteDriver();
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
