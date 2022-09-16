using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.BinarySerialization
{
    class BinaryDeserializerImpl : IBinaryDeserializer, IDisposable
    {
        ICKBinaryReader _reader;
        readonly RewindableStream _rewindableStream;
        readonly BinaryDeserializerContext _context;
        readonly List<ITypeReadInfo> _types;
        readonly List<object> _objects;

        // Deferred drivers are either IDeserializationDeferredDriver or IValueTypeDeserializerWithRefInternal.
        Stack<(IDeserializationDriverInternal D, ITypeReadInfo T, object O)>? _deferred;
        int _recurseCount;

        int _debugModeCounter;
        int _debugSentinel;
        string? _lastWriteSentinel;
        string? _lastReadSentinel;
        Stack<string>? _debugContext;
        
        // Special class to struct mutation queue.
        Queue<object>? _deferredValueQueue;
        
        public const string ExceptionPrefixContext = "[WithContext]";

        public BinaryDeserializerImpl( RewindableStream s, BinaryDeserializerContext context )
        {
            (_context = context).Acquire( this );
            _rewindableStream = s;
            _reader = s.Reader;
            _types = new List<ITypeReadInfo>();
            _objects = new List<object>();
            PostActions = new Deserialization.PostActions();
        }

        public void Dispose()
        {
            _context.Release();
            _rewindableStream.Dispose();
        }

        public Deserialization.PostActions PostActions { get; }

        public ICKBinaryReader Reader => _reader;

        public BinaryDeserializerContext Context => _context;

        public IBinaryDeserializer.IStreamInfo StreamInfo => _rewindableStream;

        internal bool ShouldStartSecondPass()
        {
            Debug.Assert( !_rewindableStream.SecondPass );
            if( _deferredValueQueue == null ) return false;

            _rewindableStream.Reset();
            _reader = _rewindableStream.Reader;
            // Clears type and objects of the first pass.
            // Reusing first pass types would be possible but requires the TypeReadInfo
            // to be able to skip its data. This may be error prone and, since running the pass n°2
            // is rather rare, it's not an issue.
            _types.Clear();
            _objects.Clear();
            PostActions.Clear();

            // And the debug state.
            _debugModeCounter = 0;
            _debugSentinel = 0;
            _lastWriteSentinel = null;
            _lastReadSentinel = null;
            _debugContext?.Clear();

            Debug.Assert( _rewindableStream.SecondPass );
            return true;
        }

        public object ReadAny() => ReadAny( null );

        object ReadAny( Type? expected )
        {
            var o = ReadAnyNullable( expected );
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

        public object? ReadAnyNullable() => ReadAnyNullable( null );

        object? ReadAnyNullable( Type? expected )
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
            if( b != SerializationMarker.DeferredObject && b != SerializationMarker.ObjectData )
            {
                ThrowInvalidDataException( $"Expecting marker ObjectData or DeferredObject. Got '{b}'." );
            }
            var info = ReadTypeInfo();
            return ReadObjectCore( b, info, (IDeserializationDriverInternal)info.GetConcreteDriver( expected ).ToNonNullable );
        }

        internal object ReadObjectCore( SerializationMarker b, ITypeReadInfo info, IDeserializationDriverInternal d )
        {
            object result;
            if( b == SerializationMarker.DeferredObject )
            {
                var defer = d as IDeserializationDeferredDriver;
                // It the deserialization driver is not a Deferred one, check that we are on a class to struct
                // mutation: if we are not, this is up to the drivers to handle this. We don't want the 2 passes to be a way to support badly written drivers!
                if( defer == null )
                {
                    if( !d.ResolvedType.IsValueType )
                    {
                        Throw.Exception( $"Class '{info.TypeNamespace}.{info.TypeName}' has been serialized as a deferred object but its deserialization driver ({d.GetType().FullName}) is not a {nameof( IDeserializationDeferredDriver )} and the deserialized type '{d.ResolvedType.FullName}' is not a value type." );
                    }
                    if( d is not IValueTypeDeserializerWithRefInternal )
                    {
                        Throw.Exception( $"Class '{info.TypeNamespace}.{info.TypeName}' is now the struct '{d.ResolvedType.FullName}'. Its deserialization driver ({d.GetType().FullName}) must be a ValueTypeDeserializerWithRef<T>." );
                    }
                }
                Debug.Assert( defer != null || (d.ResolvedType.IsValueType && d is IValueTypeDeserializerWithRefInternal), "Just to be clear: regular class or struct with a ValueTypeDeserializerWithRef driver." );
                if( defer != null || !_rewindableStream.SecondPass )
                {
                    // For class, it's always the same code path.
                    // For struct, the first pass is the same: we return and track a fake unitialized instance, except that
                    // we know that a second pass is required
                    result = RuntimeHelpers.GetUninitializedObject( d.ResolvedType );
                    Track( result );
                    if( _deferred == null ) _deferred = new Stack<(IDeserializationDriverInternal D, ITypeReadInfo T, object O)>( 100 );
                    // There's no real need of the result object for the value type but since it's already there and boxed, we can use it.
                    _deferred.Push( (d, info, result) );
                }
                else
                {
                    // Second pass on a struct: its value is known.
                    Debug.Assert( _deferredValueQueue != null );
                    result = _deferredValueQueue.Dequeue();
                    // We track it. It's value will be set where it was referenced.
                    Track( result );
                    Debug.Assert( _deferred != null, "The deferred stack has already been allocated during the first pass (and emptied at the end)." );
                    // No need of an actual object for the value type.
                    _deferred.Push( (d, info, null!) );
                }
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
                    var defer = s.D as IDeserializationDeferredDriver;
                    if( defer != null )
                    {
                        // Regular class deferring. The unitialized tracked instance is initialized in place.
                        defer.ReadInstance( this, s.T, s.O );
                    }
                    else
                    {
                        var vD = (IValueTypeDeserializerWithRefInternal)s.D;
                        // The class is now a struct.
                        // We must always read it, either to skip or store it.
                        if( _rewindableStream.SecondPass )
                        {
                            // Skip.
                            vD.ReadRawObjectData( this, s.T );
                        }
                        else
                        {
                            if( _deferredValueQueue == null ) _deferredValueQueue = new Queue<object>();
                            // Store for the second pass.
                            _deferredValueQueue.Enqueue( vD.ReadRawObjectData( this, s.T ) );
                        }
                    }
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
                case (byte)'i':
                    {
                        var k = nMark switch
                        {
                            (byte)'v' => TypeReadInfoKind.ValueType,
                            (byte)'c' => TypeReadInfoKind.Class,
                            (byte)'s' => TypeReadInfoKind.SealedClass,
                            (byte)'i' => TypeReadInfoKind.Interface,
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
                case (byte)'I':
                    {
                        var k = nMark switch
                        {
                            (byte)'V' => TypeReadInfoKind.GenericValueType,
                            (byte)'C' => TypeReadInfoKind.GenericClass,
                            (byte)'S' => TypeReadInfoKind.GenericSealedClass,
                            (byte)'I' => TypeReadInfoKind.GenericInterface,
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

        public T ReadObject<T>() where T : class => (T)ReadAny( typeof(T) );

        public T? ReadNullableObject<T>() where T : class => (T?)ReadAnyNullable( typeof(T) );

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
            // We must allow ObjectData, ObjectRef and DeferredObject for class to struct mutation.
            // We can throw on Null and on EmptyObject or KnownObject since these are necessarily reference type.
            if( b != SerializationMarker.ObjectData )
            {
                if( b == SerializationMarker.ObjectRef ) return (T)ReadObjectRef();
                if( b == SerializationMarker.DeferredObject )
                {
                    var deferredInfo = ReadTypeInfo();
                    return (T)ReadObjectCore( b, deferredInfo, (IDeserializationDriverInternal)deferredInfo.GetConcreteDriver( typeof( T ) ).ToNonNullable );
                }
                ThrowInvalidDataException( $"Unexpected '{b}' marker while reading non nullable '{typeof( T )}'." );
            }
            var info = ReadTypeInfo();
            var d = (IValueTypeNonNullableDeserializationDriver<T>)info.GetConcreteDriver( typeof(T) );
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
            Throw.InvalidDataException( b.ToString(), inner );
        }

        #endregion

    }
}
