
namespace CK.BinarySerialization;

/// <summary>
/// Typed reader handles a nullable or not typed parameter.
/// </summary>
/// <typeparam name="T">The type.</typeparam>
/// <param name="d">The reader.</param>
/// <param name="info">The type information.</param>
public delegate T TypedReader<T>( IBinaryDeserializer d, ITypeReadInfo info );
