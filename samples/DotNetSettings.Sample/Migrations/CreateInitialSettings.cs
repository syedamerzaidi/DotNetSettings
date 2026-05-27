using DotNetSettings.Migrations;

namespace DotNetSettings.Sample.Migrations;

/// <summary>Seeds the initial settings values.</summary>
public class CreateInitialSettings : ISettingsMigration
{
    public void Up(SettingsMigrator migrator)
    {
        migrator.InGroup("general", m =>
        {
            m.Add("SiteName", "My Site");
            m.Add("SiteActive", true);
            m.Add("MaxUploadSizeMb", 10);
            m.Add("LaunchedAt", DateTime.UtcNow);
            m.Add("ApiKey", "");
        });

        migrator.InGroup("mail", m =>
        {
            m.Add("FromAddress", "noreply@example.com");
            m.Add("FromName", "My Site");
            m.Add("SendWelcomeEmail", true);
        });
    }
}
