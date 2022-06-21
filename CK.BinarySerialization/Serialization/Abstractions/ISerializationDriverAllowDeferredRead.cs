namespace CK.BinarySerialization
{
    /// <summary>
    /// Marker interface that allows deferring the read: <see cref="IDeserializationDeferredDriver"/>.
    /// <para>
    /// This can be supported only for reference types.
    /// </para>
    /// </summary>
    public interface ISerializationDriverAllowDeferredRead
    {
    }
}
