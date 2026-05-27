using DotNetSettings.Repositories;
using DotNetSettings.Tests.Helpers;
using FluentAssertions;
using System.Text.Json;

namespace DotNetSettings.Tests.Settings;

public class GeneralSettingsTests
{
    [Fact]
    public void Can_load_settings_from_repository()
    {
        var (manager, repo) = SettingsManagerFactory.Create();
        repo.SetPropertyInternal("general", "SiteName", "\"Hello World\"");
        repo.SetPropertyInternal("general", "SiteActive", "true");
        repo.SetPropertyInternal("general", "MaxUploadSizeMb", "25");

        var settings = (manager, repo).Load<GeneralSettings>();

        settings.SiteName.Should().Be("Hello World");
        settings.SiteActive.Should().BeTrue();
        settings.MaxUploadSizeMb.Should().Be(25);
    }

    [Fact]
    public void Missing_properties_keep_default_values()
    {
        var (manager, repo) = SettingsManagerFactory.Create();
        // nothing seeded

        var settings = (manager, repo).Load<GeneralSettings>();

        settings.SiteName.Should().Be("");
        settings.SiteActive.Should().BeFalse();
    }

    [Fact]
    public void Save_persists_correct_json_values()
    {
        var (manager, repo) = SettingsManagerFactory.Create();
        var settings = (manager, repo).Load<GeneralSettings>();

        settings.SiteName = "Saved Site";
        settings.SiteActive = true;
        settings.MaxUploadSizeMb = 50;
        settings.Save();

        var stored = repo.GetPropertiesInGroup("general");
        JsonSerializer.Deserialize<string>(stored["SiteName"]!).Should().Be("Saved Site");
        JsonSerializer.Deserialize<bool>(stored["SiteActive"]!).Should().BeTrue();
        JsonSerializer.Deserialize<int>(stored["MaxUploadSizeMb"]!).Should().Be(50);
    }

    [Fact]
    public void Refresh_reloads_from_repository()
    {
        var (manager, repo) = SettingsManagerFactory.Create();
        repo.SetPropertyInternal("general", "SiteName", "\"Original\"");
        var settings = (manager, repo).Load<GeneralSettings>();

        settings.SiteName.Should().Be("Original");

        // Simulate external change
        repo.SetPropertyInternal("general", "SiteName", "\"Updated\"");
        settings.Refresh();

        settings.SiteName.Should().Be("Updated");
    }
}
