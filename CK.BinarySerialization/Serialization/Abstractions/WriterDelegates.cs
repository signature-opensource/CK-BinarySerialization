namespace CK.BinarySerialization;

/// <summary>
/// Untyped writer handles a nullable object.
/// </summary>
/// <param name="s">The serializer.</param>
/// <param name="o">The nullable object.</param>
public delegate void UntypedWriter( IBinarySerializer s, in object o );

/// <summary>
/// Typed writer handles a nullable or not typed parameter.
/// </summary>
/// <typeparam name="T">The type.</typeparam>
/// <param name="s">The serializer.</param>
/// <param name="o">The instance.</param>
public delegate void TypedWriter<T>( IBinarySerializer s, in T o );
