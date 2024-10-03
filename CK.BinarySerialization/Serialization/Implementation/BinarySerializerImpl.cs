using System;
using CK.Core;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace CK.BinarySerialization;


class BinarySerializerImpl : IDisposableBinarySerializer
{
    readonly ICKBinaryWriter _writer;
    // Waiting for NullableTypeTree: the bool is for notNullable reference type which is 
    // currently only true for reference types that appear as a dictionary key.
    readonly Dictionary<NullableTypeRoot, (int Idx, ISerializationDriver? D)> _types;
    readonly Dictionary<object, int> _seen;
    readonly BinarySerializerContext _context;

    int _recurseCount;
    Stack<(ISerializationDriverInternal D, object O)>? _deferred;

    int _debugModeCounter;
    int _debugSentinel;
    
    public BinarySerializerImpl( ICKBinaryWriter writer, BinarySerializerContext context )
    {
        (_context = context).Acquire();
        _writer = writer;
        _types = new Dictionary<NullableTypeRoot, (int, ISerializationDriver?)>();
        _seen = new Dictionary<object, int>( PureObjectRefEqualityComparer<object>.Default );
    }

    public void Dispose()
    {
        _context.Release();
        if( _writer is IDisposable d ) d.Dispose();
    }

    public ICKBinaryWriter Writer => _writer;

    public BinarySerializerContext Context => _context;

    public event Action<IDestroyable>? OnDestroyedObject;

    public bool WriteTypeInfo( Type t, bool? nullable = null )
    {
        return WriteTypeInfo( new NullableTypeRoot( t, nullable ), null, false );
    }

    bool WriteTypeInfo( NullableTypeRoot nT, ISerializationDriver? driver, bool driverLookupDone )
    {
        if( _types.TryGetValue( nT, out var info ) )
        {
            _writer.WriteNonNegativeSmallInt32( info.Idx );
            return false;
        }

        void RegisterAndWriteIndex( NullableTypeRoot nT, ISerializationDriver? d )
        {
            var i = (_types.Count, d);
            _types.Add( nT, i );
            _writer.WriteNonNegativeSmallInt32( i.Item1 );
        }

        bool WriteElementTypeInfo( Type t )
        {
            Type? e = t.GetElementType();
            if( e == null || e.IsGenericParameter )
            {
                Throw.ArgumentException( nameof( t ), $"Type '{t}' is not supported. Its ElementType must be not null and must not be IsGenericParameter." );
            }
            WriteTypeInfo( e );
            return true;
        }

        static string GetNotSoSimpleName( Type t )
        {
            var decl = t.DeclaringType;
            return decl != null ? GetNotSoSimpleName( decl ) + '+' + t.Name : t.Name;
        }

        // Here t is the non nullable value type or the reference type (or a byref/pointer but these will be handled right below).
        var t = nT.Type;
        // Handles special types that have no drivers nor base types.
        if( t.IsPointer )
        {
            RegisterAndWriteIndex( nT, null );
            _writer.Write( (byte)'P' );
            WriteElementTypeInfo( t );
            return true;
        }
        if( t.IsByRef )
        {
            RegisterAndWriteIndex( nT, null );
            _writer.Write( (byte)'R' );
            WriteElementTypeInfo( t );
            return true;
        }
        // Now we may have a driver names.
        if( driver == null && !driverLookupDone )
        {
            driver = _context.TryFindDriver( t );
            if( driver != null ) driver = nT.IsNullable ? driver.ToNullable : driver.ToNonNullable;
        }
        RegisterAndWriteIndex( nT, driver );
        // We don't write nullable types, we just emit a '?' and then the 
        // non nullable type info.
        if( nT.IsNullable )
        {
            _writer.Write( (byte)'?' );
            WriteTypeInfo( nT.ToNonNullable(), driver?.ToNonNullable, true );
            return true;
        }
        // Now we only write the non nullable info after a byte
        // that qualifies the type.
        // Gives the serialization driver the opportunity to write another type
        // that the actual one.
        //
        // This enables IPoco objects to be typed by their primary interface rather
        // than the generated type. The generated IPoco implementation
        // is suffixed by "_CK" and can be implemented in the StObjAutAssembly or in the
        // final assembly: we must not rely on this, instead, the deserializer uses the
        // PocoDirectory and (currently) PocoJsonSerializer to simply deserialize the object,
        // wherever the Poco code is.
        if( driver is ISerializationDriverTypeRewriter r )
        {
            t = r.GetTypeToWrite( t );
        }

        if( t.IsArray )
        {
            if( t.ContainsGenericParameters )
            {
                _writer.Write( (byte)'A' );
                _writer.WriteSmallInt32( t.GetArrayRank(), 1 );
            }
            else
            {
                _writer.Write( (byte)'a' );
                _writer.WriteSmallInt32( t.GetArrayRank(), 1 );
                WriteElementTypeInfo( t );
                _writer.WriteSharedString( driver?.DriverName );
            }
            return true;
        }
        if( t.IsGenericType )
        {
            // For Generics we consider only Opened vs. Closed ones.
            if( t.ContainsGenericParameters )
            {
                _writer.Write( (byte)'O' );
            }
            else
            {
                // Use uppercase for generic types.
                if( t.IsClass )
                {
                    if( t.IsSealed )
                    {
                        _writer.Write( (byte)'S' );
                    }
                    else
                    {
                        _writer.Write( (byte)'C' );
                    }
                }
                else
                {
                    if( t.IsInterface )
                    {
                        _writer.Write( (byte)'I' );
                    }
                    else
                    {
                        _writer.Write( (byte)'V' );
                    }
                }
                var args = t.GetGenericArguments();
                // Currently we work in oblivious nullable mode: all reference types are de facto nullable,
                // except one: the dictionary key.
                if( args.Length == 2
                    && (t.GetGenericTypeDefinition() == typeof( Dictionary<,> )
                        || t.GetGenericTypeDefinition() == typeof( IDictionary<,> )
                        || t.GetGenericTypeDefinition() == typeof( IReadOnlyDictionary<,> )) )
                {
                    _writer.WriteNonNegativeSmallInt32( 2 );
                    WriteTypeInfo( args[0], false );
                    WriteTypeInfo( args[1] );
                }
                else
                {
                    _writer.WriteNonNegativeSmallInt32( args.Length );
                    foreach( var p in args )
                    {
                        WriteTypeInfo( p );
                    }
                }
            }
        }
        else if( t.IsEnum )
        {
            _writer.Write( (byte)'E' );
            WriteTypeInfo( t.GetEnumUnderlyingType() );
        }
        else
        {
            // Uses lowercase for non generic types.
            if( t.IsClass )
            {
                if( t.IsSealed )
                {
                    _writer.Write( (byte)'s' );
                }
                else
                {
                    _writer.Write( (byte)'c' );
                }
            }
            else
            {
                if( t.IsInterface )
                {
                    _writer.Write( (byte)'i' );
                }
                else
                {
                    _writer.Write( (byte)'v' );
                }
            }
        }
        // Write Names.
        if( driver != null )
        {
            _writer.WriteSharedString( driver.DriverName );
            _writer.WriteSmallInt32( driver.SerializationVersion );
        }
        else
        {
            _writer.WriteSharedString( null );
        }
        _writer.WriteSharedString( t.Namespace );
        _writer.Write( GetNotSoSimpleName( t ) );
        _writer.WriteSharedString( t.Assembly.GetName().Name );
        if( !t.IsValueType )
        {
            var b = t.BaseType;
            if( b != null && b != typeof( object ) && b != typeof( ValueType ) )
            {
                _writer.Write( true );
                WriteTypeInfo( b );
            }
            else _writer.Write( false );
        }
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
        var d = _context.FindDriver( typeof( T ) );
        _writer.Write( (byte)SerializationMarker.ObjectData );
        WriteTypeInfo( new NullableTypeRoot( typeof( T ), false ), d, true );
        ((TypedWriter<T>)d.TypedWriter)( this, value );
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
        Throw.CheckNotNullArgument( o );
        return DoWriteObject( o );
    }

    internal bool TrackObject<T>( T o ) where T : class
    {
        if( _seen.TryGetValue( o, out var num ) )
        {
            _writer.Write( (byte)SerializationMarker.ObjectRef );
            _writer.Write( num );
            return false;
        }
        _seen.Add( o, _seen.Count );
        if( OnDestroyedObject != null && o is IDestroyable d && d.IsDestroyed )
        {
            OnDestroyedObject( d );
        }
        return true;
    }

    internal bool DoWriteObject( object o )
    {
        if( o is Type oT )
        {
            _writer.Write( (byte)SerializationMarker.Type );
            return WriteTypeInfo( oT );
        }
        SerializationMarker marker = SerializationMarker.ObjectData;
        var t = o.GetType();
        bool isClass = t.IsClass;
        if( isClass )
        {
            if( !TrackObject( o ) ) return false;
            if( t == typeof( object ) )
            {
                _writer.Write( (byte)SerializationMarker.EmptyObject );
                return true;
            }
            string? knownObject = _context.GetKnownObjectKey( o );
            if( knownObject != null )
            {
                _writer.Write( (byte)SerializationMarker.KnownObject );
                _writer.Write( knownObject );
                return true;
            }
        }
        var driver = (ISerializationDriverInternal)_context.FindDriver( t ).ToNonNullable;
        if( _recurseCount > _context.MaxRecursionDepth 
            && isClass
            && driver is ISerializationDriverAllowDeferredRead )
        {
            if( _deferred == null ) _deferred = new Stack<(ISerializationDriverInternal D, object O)>( 200 );
            _deferred.Push( (driver, o) );
            _writer.Write( (byte)SerializationMarker.DeferredObject );
            WriteTypeInfo( new NullableTypeRoot( t, false ), driver, true );
        }
        else
        {
            ++_recurseCount;
            _writer.Write( (byte)marker );
            WriteTypeInfo( new NullableTypeRoot( t, false ), driver, true );
            driver.WriteObjectData( this, o );
            --_recurseCount;
        }
        if( _recurseCount == 0 && _deferred != null )
        {
            while( _deferred.TryPop( out var d ) )
            {
                ++_recurseCount;
                d.D.WriteObjectData( this, d.O );
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

    internal static bool NextInArray( int[] coords, int[] lengths )
    {
        int i = coords.Length - 1;
        while( ++coords[i] >= lengths[i] )
        {
            if( i == 0 ) return false;
            coords[i--] = 0;
        }
        return true;
    }


}
