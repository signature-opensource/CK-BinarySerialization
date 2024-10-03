namespace CK.BinarySerialization;

/// <summary>
/// Marker interface that allows deferring the read: see <see cref="IDeserializationDeferredDriver"/>.
/// <para>
/// This can be supported only for reference types.
/// </para>
/// </summary>
public interface ISerializationDriverAllowDeferredRead
{
}
