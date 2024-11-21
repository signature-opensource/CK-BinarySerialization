using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CK.BinarySerialization.Tests;

sealed class ByTenInt32Equality : IEqualityComparer<int>
{
    public static readonly ByTenInt32Equality Instance = new ByTenInt32Equality();

    public bool Equals( int x, int y ) => (x / 10) == (y / 10);

    public int GetHashCode( [DisallowNull] int value ) => value / 10;
}
