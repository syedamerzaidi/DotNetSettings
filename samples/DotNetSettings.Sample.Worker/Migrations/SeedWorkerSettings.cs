using DotNetSettings.Migrations;

namespace DotNetSettings.Sample.Worker.Migrations;

public class SeedWorkerSettings : ISettingsMigration
{
    public void Up(SettingsMigrator migrator)
    {
        migrator.InGroup("worker", m =>
        {
            m.Add("PollIntervalSeconds", 30);
            m.Add("ProcessingEnabled", true);
            m.Add("BatchSize", 100);
        });
    }
}
