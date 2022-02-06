using System;
using CK.Core;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace CK.BinarySerialization
{
    class BinarySerializerImpl : IBinarySerializer
    {
        readonly ICKBinaryWriter _writer;
        readonly ISerializerResolver _resolver;
        readonly Dictionary<Type, (int Idx, ITypeSerializationDriver? D)> _types;
        readonly Action<IDestroyable>? _destroyedTracker;
        readonly Dictionary<object, int> _seen;

        public const int MaxRecurse = 50;
        int _recurseCount;
        Stack<(ITypeSerializationDriver D, object O)>? _deferred;

        int _debugModeCounter;
        int _debugSentinel;
        bool _leaveOpen;

        public BinarySerializerImpl( ICKBinaryWriter writer, bool leaveOpen, ISerializerResolver resolver, Action<IDestroyable>? destroyedTracker )
        {
            _writer = writer;
            _leaveOpen = leaveOpen;
            _resolver = resolver;
            _destroyedTracker = destroyedTracker;
            _types = new Dictionary<Type, (int, ITypeSerializationDriver?)>();
            _seen = new Dictionary<object, int>( PureObjectRefEqualityComparer<object>.Default );
        }

        public void Dispose()
        {
            if( !_leaveOpen && _writer is IDisposable d )
            {
                _leaveOpen = true;
                d.Dispose();
            }
        }

        public ICKBinaryWriter Writer => _writer;

        public bool WriteTypeInfo( Type t )
        {
            if( t == null ) throw new ArgumentNullException( nameof( t ) );
            return WriteTypeInfo( t, null );
        }

        bool WriteTypeInfo( Type t, ITypeSerializationDriver? knownDriver )
        {
            if( _types.TryGetValue( t, out var info ) )
            {
                _writer.WriteNonNegativeSmallInt32( info.Idx );
                return false;
            }
            var d = knownDriver ?? _resolver.TryFindDriver( t );
            info = (_types.Count, d);
            _types.Add( t, info );
            _writer.WriteNonNegativeSmallInt32( info.Idx );
            if( d != null )
            {
                _writer.WriteSharedString( d.DriverName );
                _writer.WriteSmallInt32( d.SerializationVersion );
            }
            else
            {
                _writer.WriteSharedString( null );
                _writer.WriteSmallInt32( -1 );
            }
            _writer.WriteSharedString( t.FullName );
            _writer.WriteSharedString( t.Name );
            _writer.WriteSharedString( t.Assembly.FullName );
            // Write base types recursively.
            var b = t.BaseType;
            if( b != null && b != typeof( object ) && b != typeof( ValueType ) )
            {
                _writer.Write( true );
                WriteTypeInfo( b );
            }
            else _writer.Write( false );
            // Writes generic parameter types if any and if the type is a closed generic.
            if( t.IsConstructedGenericType )
            {
                var args = t.GetGenericArguments();
                _writer.WriteNonNegativeSmallInt32( args.Length );
                foreach( var p in t.GetGenericArguments() )
                {
                    WriteTypeInfo( p );
                }
            }
            else _writer.WriteNonNegativeSmallInt32( 0 );
            return true;
        }

        bool WriteNullableObject<T>( T? o ) where T : class;
        
        public bool WriteObject<T>( T o ) where T : class
        {
            if( o == null ) throw new ArgumentNullException( "o" );
            if( !TrackObject( o ) ) return false;
            var d = _resolver.FindDriver<T>();
            WriteTypeInfo( typeof( T ) );
            d.WriteData( this, value );

        }


        public void WriteNullableValue<T>( T? value ) where T : struct
        {
            if( value.HasValue )
            {
                _writer.Write( true );
                WriteValue(value.Value);
            }
            else _writer.Write( false );
        }

        public void WriteValue<T>( T value ) where T : struct
        {
            var d = _resolver.FindDriver<T>();
            WriteTypeInfo( typeof( T ) );
            d.WriteData( this, value );
        }

        public bool WriteNullableObject( object? o )
        {
            if( o == null )
            {
                _writer.Write( (byte)SerializationMarker.Null );
                return true;
            }
            return DoWriteObject( o );
        }

        public bool WriteObject( object o )
        {
            if( o == null ) throw new ArgumentNullException( nameof(o) );
            return DoWriteObject( o );
        }

        bool TrackObject<T>( T o ) where T : class
        {
            if( _seen.TryGetValue( o, out var num ) )
            {
                _writer.Write( (byte)SerializationMarker.ObjectRef );
                _writer.Write( num );
                return false;
            }
            _seen.Add( o, _seen.Count );
            return true;
        }

        bool DoWriteObject( object o )
        {
            if( o is Type oT )
            {
                _writer.Write( (byte)SerializationMarker.Type );
                return WriteTypeInfo( oT );
            }
            SerializationMarker marker;
            var t = o.GetType();
            if( t.IsClass )
            {
                if( !TrackObject( o ) ) return false;
                if( t == typeof( object ) )
                {
                    _writer.Write( (byte)SerializationMarker.EmptyObject );
                    return true;
                }
                marker = SerializationMarker.Object;
            }
            else
            {
                marker = SerializationMarker.Struct;
            }
            ITypeSerializationDriver driver = _resolver.FindDriver( t );
            if( _recurseCount > MaxRecurse 
                && marker == SerializationMarker.Object
                && driver is ITypeSerializationDriverAllowDeferredRead )
            {
                if( _deferred == null ) _deferred = new Stack<(ITypeSerializationDriver D, object O)>( 200 );
                _deferred.Push( (driver, o) );
                _writer.Write( (byte)SerializationMarker.DeferredObject );
                WriteTypeInfo( t, driver );
            }
            else
            {
                ++_recurseCount;
                _writer.Write( (byte)marker );
                WriteTypeInfo( t, driver );
                driver.WriteData( this, o );
                --_recurseCount;
            }
            if( _recurseCount == 0 && _deferred != null )
            {
                while( _deferred.TryPop( out var d ) )
                {
                    ++_recurseCount;
                    d.D.WriteData( this, d.O );
                    --_recurseCount;
                }
            }
            return true;
        }

        #region DebugMode methods
        public bool IsDebugMode => _debugModeCounter > 0;

        public void DebugWriteMode( bool? active )
        {
            if( active.HasValue )
            {
                if( active.Value )
                {
                    _writer.Write( (byte)182 );
                    ++_debugModeCounter;
                }
                else
                {
                    _writer.Write( (byte)181 );
                    --_debugModeCounter;
                }
            }
            else _writer.Write( (byte)180 );
        }

        public void DebugWriteSentinel( [CallerFilePath] string? fileName = null, [CallerLineNumber] int line = 0 )
        {
            if( IsDebugMode )
            {
                _writer.Write( 987654321 );
                _writer.Write( _debugSentinel++ );
                _writer.Write( fileName + '(' + line.ToString() + ')' );
            }
        }

        #endregion 


    }
}