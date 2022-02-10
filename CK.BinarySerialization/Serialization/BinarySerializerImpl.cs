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
        readonly Dictionary<Type, (int Idx, IUntypedSerializationDriver? D)> _types;
        readonly Action<IDestroyable>? _destroyedTracker;
        readonly Dictionary<object, int> _seen;

        public const int MaxRecurse = 50;
        int _recurseCount;
        Stack<(IUntypedSerializationDriver D, object O)>? _deferred;

        int _debugModeCounter;
        int _debugSentinel;
        bool _leaveOpen;

        public BinarySerializerImpl( ICKBinaryWriter writer, bool leaveOpen, ISerializerResolver resolver, Action<IDestroyable>? destroyedTracker )
        {
            _writer = writer;
            _leaveOpen = leaveOpen;
            _resolver = resolver;
            _destroyedTracker = destroyedTracker;
            _types = new Dictionary<Type, (int, IUntypedSerializationDriver?)>();
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

        bool WriteTypeInfo( Type t, IUntypedSerializationDriver? knownDriver )
        {
            if( _types.TryGetValue( t, out var info ) )
            {
                _writer.WriteNonNegativeSmallInt32( info.Idx );
                return false;
            }

            void RegisterAndWriteIndex( Type t, IUntypedSerializationDriver? d )
            {
                var i = (_types.Count, d);
                _types.Add( t, i );
                _writer.WriteNonNegativeSmallInt32( i.Item1 );
            }

            bool WriteElementTypeInfo( Type t, bool mustExist )
            {
                Type? e = t.GetElementType();
                if( e == null || e.IsGenericParameter )
                {
                    if( mustExist ) throw new ArgumentException( $"Type '{t}' is not supported. Its ElementType must be not null and must not be IsGenericParameter." );
                    _writer.Write( false );
                    return false;
                }
                if( !mustExist )_writer.Write( true );
                WriteTypeInfo( e );
                return true;
            }

            static string GetNotSoSimpleName( Type t )
            {
                var decl = t.DeclaringType;
                return decl != null ? GetNotSoSimpleName( decl ) + '+' + t.Name : t.Name;
            }

            // Handles special types that have no drivers nor base types.
            if( t.IsPointer )
            {
                RegisterAndWriteIndex( t, null );
                _writer.Write( (byte)4 );
                WriteElementTypeInfo( t, true );
                return true;
            }
            if( t.IsByRef )
            {
                RegisterAndWriteIndex( t, null );
                _writer.Write( (byte)3 );
                WriteElementTypeInfo( t, true );
                return true;
            }
            // Now we may have a driver names.
            var d = knownDriver ?? _resolver.TryFindDriver( t );
            RegisterAndWriteIndex( t, d );
            if( t.IsArray )
            {
                _writer.Write( (byte)2 );
                _writer.WriteSmallInt32( t.GetArrayRank(), 1 );
                if( WriteElementTypeInfo( t, false ) )
                {
                    _writer.WriteSharedString( d?.DriverName );
                }
                return true;
            }
            if( t.IsGenericType )
            {
                _writer.Write( (byte)1 );
                // For Generics we consider only Opened vs. Closed ones.
                if( t.ContainsGenericParameters )
                {
                    // There's at least one free parameter T: it's not closed.
                    _writer.WriteNonNegativeSmallInt32( 0 );
                }
                else
                {
                    var args = t.GetGenericArguments();
                    _writer.WriteNonNegativeSmallInt32( args.Length );
                    foreach( var p in args )
                    {
                        WriteTypeInfo( p );
                    }
                }
            }
            else
            {
                _writer.Write( (byte)0 );
            }
            // Write Names.
            if( d != null )
            {
                _writer.WriteSharedString( d.DriverName );
                _writer.WriteSmallInt32( d.SerializationVersion );
            }
            else
            {
                _writer.WriteSharedString( null );
            }
            _writer.WriteSharedString( t.Namespace );
            _writer.Write( GetNotSoSimpleName( t ) );
            _writer.WriteSharedString( t.Assembly.GetName().Name );
            // Write base types recursively.
            var b = t.BaseType;
            if( b != null && b != typeof( object ) && b != typeof( ValueType ) )
            {
                _writer.Write( true );
                WriteTypeInfo( b );
            }
            else _writer.Write( false );

            return true;
        }

        public bool WriteObject<T>( T o ) where T : class => WriteAny( o );

        public bool WriteNullableObject<T>( T? o ) where T : class => WriteAnyNullable( o );

        public void WriteNullableValue<T>( in T? value ) where T : struct
        {
            if( value.HasValue )
            {
                WriteValue( value.Value );
            }
            else
            {
                _writer.Write( (byte)SerializationMarker.Null );
            }
        }

        public void WriteValue<T>( in T value ) where T : struct
        {
            var d = _resolver.FindDriver<T>();
            // Writing the Struct marker enables this to be read as any object.
            _writer.Write( (byte)SerializationMarker.Struct );
            WriteTypeInfo( typeof( T ) );
            d.WriteData( this, value );
        }

        public bool WriteAnyNullable( object? o )
        {
            if( o == null )
            {
                _writer.Write( (byte)SerializationMarker.Null );
                return true;
            }
            return DoWriteObject( o );
        }

        public bool WriteAny( object o )
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
            IUntypedSerializationDriver driver = _resolver.FindDriver( t );
            if( _recurseCount > MaxRecurse 
                && marker == SerializationMarker.Object
                && driver is ISerializationDriverAllowDeferredRead )
            {
                if( _deferred == null ) _deferred = new Stack<(IUntypedSerializationDriver D, object O)>( 200 );
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