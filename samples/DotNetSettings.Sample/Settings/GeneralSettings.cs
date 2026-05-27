using DotNetSettings;
using DotNetSettings.Attributes;

namespace DotNetSettings.Sample.Settings;

/// <summary>Site-wide general settings.</summary>
public class GeneralSettings : DotNetSettings.Settings
{
    public string SiteName { get; set; } = "";
    public bool SiteActive { get; set; }
    public int MaxUploadSizeMb { get; set; }
    public DateTime LaunchedAt { get; set; }

    public override string Group => "general";

    [Encrypt]
    public string ApiKey { get; set; } = "";
}
