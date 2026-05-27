using System.Globalization;

namespace DotNetSettings.Casts;

/// <summary>Casts <see cref="DateTimeOffset"/> to/from an ISO 8601 string.</summary>
public sealed class DateTimeOffsetSettingsCast : ISettingsCast
{
    private const string Format = "O";

    /// <inheritdoc/>
    public object? Get(string? payload, Type targetType)
    {
        if (payload is null) return null;
        var raw = payload.Trim('"');
        return DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    /// <inheritdoc/>
    public string? Set(object? value)
    {
        if (value is null) return null;
        var dto = (DateTimeOffset)value;
        return $"\"{dto.ToString(Format, CultureInfo.InvariantCulture)}\"";
    }
}
