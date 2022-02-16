using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization
{
    /// <summary>
    /// Untyped writer handles a nullable object.
    /// </summary>
    /// <param name="w">The writer.</param>
    /// <param name="o">The nullable object.</param>
    public delegate void UntypedWriter( IBinarySerializer w, in object o );

    /// <summary>
    /// Typed writer handles a nullable or not typed parameter.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="w">The writer.</param>
    /// <param name="o">The instance.</param>
    public delegate void TypedWriter<T>( IBinarySerializer w, in T o );
}
