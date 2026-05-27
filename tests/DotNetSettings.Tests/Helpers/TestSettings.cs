using DotNetSettings;
using DotNetSettings.Attributes;
using DotNetSettings.Casts;

namespace DotNetSettings.Tests.Helpers;

public class GeneralSettings : BaseSettings
{
    public string SiteName { get; set; } = "";
    public bool SiteActive { get; set; }
    public int MaxUploadSizeMb { get; set; }
    public DateTime LaunchedAt { get; set; }

    [Encrypt]
    public string ApiKey { get; set; } = "";

    public override string Group => "general";
}

public class MailSettings : BaseSettings
{
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "";
    public bool SendWelcomeEmail { get; set; }

    public override string Group => "mail";
}

public enum Status { Active, Inactive, Pending }

public class CastSettings : BaseSettings
{
    public DateTime CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Status CurrentStatus { get; set; }
    public CustomValue? Custom { get; set; }

    public override string Group => "cast";

    public override Dictionary<string, ISettingsCast> GetCasts() =>
        new() { [nameof(Custom)] = new CustomValueCast() };
}

public record CustomValue(string Inner);

public class CustomValueCast : ISettingsCast
{
    public object? Get(string? payload, Type targetType) =>
        payload is null ? null : new CustomValue(payload.Trim('"'));

    public string? Set(object? value) =>
        value is CustomValue cv ? $"\"{cv.Inner}\"" : null;
}
