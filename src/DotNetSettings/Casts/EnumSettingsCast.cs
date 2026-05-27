namespace DotNetSettings.Casts;

/// <summary>Casts enum values to/from their string name (e.g. <c>"Active"</c>).</summary>
public sealed class EnumSettingsCast : ISettingsCast
{
    /// <inheritdoc/>
    public object? Get(string? payload, Type targetType)
    {
        if (payload is null) return null;
        var name = payload.Trim('"');
        return Enum.Parse(targetType, name, ignoreCase: true);
    }

    /// <inheritdoc/>
    public string? Set(object? value)
    {
        if (value is null) return null;
        return $"\"{value}\"";
    }
}
