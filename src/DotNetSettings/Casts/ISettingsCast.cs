namespace DotNetSettings.Casts;

/// <summary>
/// Converts between a strongly-typed CLR value and its JSON string representation
/// stored in the settings repository.
/// </summary>
public interface ISettingsCast
{
    /// <summary>Deserialize <paramref name="payload"/> into <paramref name="targetType"/>.</summary>
    object? Get(string? payload, Type targetType);

    /// <summary>Serialize <paramref name="value"/> to a string payload for storage.</summary>
    string? Set(object? value);
}
