using DotNetSettings;

namespace DotNetSettings.Sample.ConsoleApp.Settings;

public class AppSettings : DotNetSettings.Settings
{
    public string AppName { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public int MaxRetries { get; set; }
    public bool VerboseLogging { get; set; }

    public override string Group => "app";
}
