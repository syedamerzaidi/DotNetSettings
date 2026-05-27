using DotNetSettings.Migrations;

namespace DotNetSettings.Sample.ConsoleApp.Migrations;

public class SeedAppSettings : ISettingsMigration
{
    public void Up(SettingsMigrator migrator)
    {
        migrator.InGroup("app", m =>
        {
            m.Add("AppName", "Console Sample");
            m.Add("Version", "1.0.0");
            m.Add("MaxRetries", 3);
            m.Add("VerboseLogging", false);
        });
    }
}
