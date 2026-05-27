using System.Globalization;

namespace DotNetSettings.Casts;

/// <summary>Casts <see cref="DateTime"/> to/from an ISO 8601 string.</summary>
public sealed class DateTimeSettingsCast : ISettingsCast
{
    private const string Format = "O"; // round-trip ISO 8601

    /// <inheritdoc/>
    public object? Get(string? payload, Type targetType)
    {
        if (payload is null) return null;
        var raw = payload.Trim('"');
        return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    /// <inheritdoc/>
    public string? Set(object? value)
    {
        if (value is null) return null;
        var dt = (DateTime)value;
        return $"\"{dt.ToString(Format, CultureInfo.InvariantCulture)}\"";
    }
}
