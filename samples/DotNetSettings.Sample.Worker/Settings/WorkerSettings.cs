using DotNetSettings;

namespace DotNetSettings.Sample.Worker.Settings;

public class WorkerSettings : DotNetSettings.Settings
{
    public int PollIntervalSeconds { get; set; } = 30;
    public bool ProcessingEnabled { get; set; } = true;
    public int BatchSize { get; set; } = 100;

    public override string Group => "worker";
}
