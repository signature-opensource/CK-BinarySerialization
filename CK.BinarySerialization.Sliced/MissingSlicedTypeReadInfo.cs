using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Missing <see cref="ITypeReadInfo"/> that is provided to a deserialization constructor 
    /// when no type information has been read that matches the local type.
    /// <para>
    /// The only property that is relevant is <see cref="TypePath"/>.
    /// </para>
    /// </summary>
    public class MissingSlicedTypeReadInfo : ITypeReadInfo
    {
        /// <summary>
        /// Initializes a new <see cref="MissingSlicedTypeReadInfo"/>.
        /// </summary>
        /// <param name="leafTypes">The whole type path of the type being deserialized.</param>
        public MissingSlicedTypeReadInfo( IReadOnlyList<ITypeReadInfo> leafTypes )
        {
            TypePath = leafTypes;
        }

        /// <summary>
        /// Gets the whole type path that is being deserialized.
        /// </summary>
        public IReadOnlyList<ITypeReadInfo> TypePath { get; }

        /// <summary>
        /// Always false.
        /// </summary>
        public bool IsNullable => false;

        /// <summary>
        /// Always true.
        /// </summary>
        public bool IsSealed => true;

        /// <summary>
        /// Always false.
        /// </summary>
        public bool IsValueType => false;

        /// <summary>
        /// Always this information.
        /// </summary>
        public ITypeReadInfo ToNonNullable => this;

        /// <summary>
        /// Always <see cref="TypeReadInfoKind.None"/>.
        /// </summary>
        public TypeReadInfoKind Kind => TypeReadInfoKind.None;

        /// <summary>
        /// Always 0.
        /// </summary>
        public int ArrayRank => 0;

        /// <summary>
        /// Always null.
        /// </summary>
        public ITypeReadInfo? BaseTypeReadInfo => null;

        /// <summary>
        /// Always null.
        /// </summary>
        public string? DriverName => null;

        /// <summary>
        /// Always -1.
        /// </summary>
        public int SerializationVersion => -1;

        /// <summary>
        /// Always empty.
        /// </summary>
        public IReadOnlyList<ITypeReadInfo> SubTypes => Array.Empty<ITypeReadInfo>();

        /// <summary>
        /// Always the empty string.
        /// </summary>
        public string AssemblyName => "";

        /// <summary>
        /// Always the empty string.
        /// </summary>
        public string TypeName => "";

        /// <summary>
        /// Always the empty string.
        /// </summary>
        public string TypeNamespace => "";

        /// <summary>
        /// Always false.
        /// </summary>
        public bool HasResolvedConcreteDriver => false;

        /// <summary>
        /// Always null.
        /// </summary>
        public Type? TargetType => null;

        /// <summary>
        /// Always null.
        /// </summary>
        public Type? TryResolveLocalType() => null;

        /// <summary>
        /// Always throws a <see cref="NotSupportedException"/>.
        /// </summary>
        public IDeserializationDriver GetConcreteDriver()
        {
            throw new NotSupportedException( nameof( MissingSlicedTypeReadInfo ) );
        }

        /// <summary>
        /// Always throws a <see cref="NotSupportedException"/>.
        /// </summary>
        public IDeserializationDriver GetPotentiallyAbstractDriver()
        {
            throw new NotSupportedException( nameof( MissingSlicedTypeReadInfo ) );
        }

        /// <summary>
        /// Always throws a <see cref="NotSupportedException"/>.
        /// </summary>
        public Type ResolveLocalType()
        {
            throw new NotSupportedException( nameof( MissingSlicedTypeReadInfo ) );
        }
    }
}
