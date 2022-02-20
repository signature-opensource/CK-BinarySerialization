using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.BinarySerialization
{
    readonly struct NullableTypeRoot : IEquatable<NullableTypeRoot>
    {
        public NullableTypeRoot( Type t, bool? nullable )
        {
            if( t == null ) throw new ArgumentNullException( "t" );
            if( t.IsByRef || t.IsPointer )
            {
                // ByRef, pointer or ref struct.
                // They are non nullable since only the type itself
                // must be registered (and may be restored on the other side).
                Type = t;
                IsNullable = false;
            }
            else if( t.IsValueType )
            {
                if( t.IsGenericType && t.GetGenericTypeDefinition() == typeof( Nullable<> ) )
                {
                    Type = Nullable.GetUnderlyingType( t )!;
                    IsNullable = nullable ?? true;
                }
                else
                { 
                    Type = t;
                    IsNullable = nullable ?? false;
                }
            }
            else
            {
                Type = t;
                IsNullable = nullable ?? true;
            }
        }

        NullableTypeRoot( bool n, Type t )
        {
            Type = t;
            IsNullable = n;
        }

        public NullableTypeRoot ToNonNullable() => new NullableTypeRoot( false, Type );

        /// <summary>
        /// Gets the type.
        /// When this type is nullable value type (a <see cref="Nullable{T}"/>), then this type is the inner type, not the Nullable generic type.
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// Gets whether this type is nullable.
        /// </summary>
        public readonly bool IsNullable;

        public bool Equals( [AllowNull] NullableTypeRoot other ) => Type == other.Type && IsNullable == other.IsNullable;

        public override bool Equals( object? obj ) => obj is NullableTypeRoot o && Equals( o );

        override public int GetHashCode() => IsNullable ? Type.GetHashCode() : -Type.GetHashCode();
    }
}
