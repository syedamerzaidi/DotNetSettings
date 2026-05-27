using DotNetSettings.Tests.Helpers;
using FluentAssertions;
using System.Text.Json;

namespace DotNetSettings.Tests.Settings;

public class LockingTests
{
    [Fact]
    public void Locked_property_is_not_overwritten_on_save()
    {
        var (manager, repo) = SettingsManagerFactory.Create();
        repo.SetPropertyInternal("general", "SiteName", "\"Locked Value\"");
        var settings = (manager, repo).Load<GeneralSettings>();

        settings.Lock(nameof(GeneralSettings.SiteName));

        settings.SiteName = "New Value";
        settings.Save();

        var stored = repo.GetPropertiesInGroup("general");
        JsonSerializer.Deserialize<string>(stored["SiteName"]!).Should().Be("Locked Value");
    }

    [Fact]
    public void Can_lock_and_unlock_properties()
    {
        var (manager, repo) = SettingsManagerFactory.Create();
        var settings = (manager, repo).Load<GeneralSettings>();

        settings.IsLocked(nameof(GeneralSettings.SiteName)).Should().BeFalse();

        settings.Lock(nameof(GeneralSettings.SiteName));
        settings.IsLocked(nameof(GeneralSettings.SiteName)).Should().BeTrue();

        settings.Unlock(nameof(GeneralSettings.SiteName));
        settings.IsLocked(nameof(GeneralSettings.SiteName)).Should().BeFalse();
    }

    [Fact]
    public void IsLocked_returns_correct_state()
    {
        var (manager, repo) = SettingsManagerFactory.Create();
        var settings = (manager, repo).Load<GeneralSettings>();

        settings.Lock(nameof(GeneralSettings.SiteName), nameof(GeneralSettings.SiteActive));

        settings.IsLocked(nameof(GeneralSettings.SiteName)).Should().BeTrue();
        settings.IsLocked(nameof(GeneralSettings.SiteActive)).Should().BeTrue();
        settings.IsLocked(nameof(GeneralSettings.MaxUploadSizeMb)).Should().BeFalse();
    }

    [Fact]
    public void GetLockedProperties_returns_all_locked_names()
    {
        var (manager, repo) = SettingsManagerFactory.Create();
        var settings = (manager, repo).Load<GeneralSettings>();

        settings.Lock(nameof(GeneralSettings.SiteName), nameof(GeneralSettings.MaxUploadSizeMb));

        var locked = settings.GetLockedProperties();
        locked.Should().Contain(nameof(GeneralSettings.SiteName));
        locked.Should().Contain(nameof(GeneralSettings.MaxUploadSizeMb));
        locked.Should().HaveCount(2);
    }

    [Fact]
    public void Unlocked_property_can_be_overwritten_on_save()
    {
        var (manager, repo) = SettingsManagerFactory.Create();
        repo.SetPropertyInternal("general", "SiteName", "\"Original\"");
        var settings = (manager, repo).Load<GeneralSettings>();

        settings.Lock(nameof(GeneralSettings.SiteName));
        settings.Unlock(nameof(GeneralSettings.SiteName));

        settings.SiteName = "Changed";
        settings.Save();

        var stored = repo.GetPropertiesInGroup("general");
        System.Text.Json.JsonSerializer.Deserialize<string>(stored["SiteName"]!).Should().Be("Changed");
    }
}
