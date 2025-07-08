namespace CK.BinarySerialization;

/// <summary>
/// This singleton type is a marker used by the "empty reversed deserializer constructor".
/// </summary>
public class Sliced
{
    /// <summary>
    /// Single instance.
    /// </summary>
    public static readonly Sliced Instance = new Sliced();

    Sliced() { }
}
