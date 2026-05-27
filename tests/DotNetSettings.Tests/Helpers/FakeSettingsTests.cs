using DotNetSettings.Repositories;
using FluentAssertions;

namespace DotNetSettings.Tests.Helpers;

// BaseSettings alias resolves to DotNetSettings.Settings via GlobalUsings.cs


public class FakeSettingsTests
{
    [Fact]
    public void Fake_returns_instance_with_overridden_values()
    {
        var fake = BaseSettings.Fake<GeneralSettings>(new()
        {
            [nameof(GeneralSettings.SiteName)] = "Test Site",
            [nameof(GeneralSettings.SiteActive)] = true,
            [nameof(GeneralSettings.MaxUploadSizeMb)] = 99
        });

        fake.SiteName.Should().Be("Test Site");
        fake.SiteActive.Should().BeTrue();
        fake.MaxUploadSizeMb.Should().Be(99);
    }

    [Fact]
    public void Fake_without_all_properties_keeps_defaults_for_unspecified()
    {
        var fake = BaseSettings.Fake<GeneralSettings>(new()
        {
            [nameof(GeneralSettings.SiteName)] = "Only This"
        });

        fake.SiteName.Should().Be("Only This");
        fake.MaxUploadSizeMb.Should().Be(0); // default int
        fake.SiteActive.Should().BeFalse(); // default bool
    }

    [Fact]
    public void FakeSettingsRepository_stores_and_retrieves_values()
    {
        var repo = new FakeSettingsRepository();

        repo.CreateProperty("general", "SiteName", "\"Hello\"");

        repo.PropertyExists("general", "SiteName").Should().BeTrue();
        repo.GetPropertyPayload("general", "SiteName").Should().Be("\"Hello\"");
    }

    [Fact]
    public void FakeSettingsRepository_UpdateProperties_upserts_correctly()
    {
        var repo = new FakeSettingsRepository();
        repo.CreateProperty("general", "A", "\"old\"");

        repo.UpdateProperties("general", new() { ["A"] = "\"new\"", ["B"] = "\"created\"" });

        repo.GetPropertyPayload("general", "A").Should().Be("\"new\"");
        repo.GetPropertyPayload("general", "B").Should().Be("\"created\"");
    }

    [Fact]
    public void FakeSettingsRepository_DeleteProperty_removes_record()
    {
        var repo = new FakeSettingsRepository();
        repo.CreateProperty("general", "ToDelete", "\"bye\"");

        repo.DeleteProperty("general", "ToDelete");

        repo.PropertyExists("general", "ToDelete").Should().BeFalse();
    }

    [Fact]
    public void FakeSettingsRepository_tracks_locks()
    {
        var repo = new FakeSettingsRepository();
        repo.CreateProperty("general", "Name", "\"x\"");

        repo.LockProperties("general", new[] { "Name" });
        repo.GetLockedProperties("general").Should().Contain("Name");

        repo.UnlockProperties("general", new[] { "Name" });
        repo.GetLockedProperties("general").Should().BeEmpty();
    }

    [Fact]
    public async Task FakeSettingsRepository_async_methods_work()
    {
        var repo = new FakeSettingsRepository();
        await repo.CreatePropertyAsync("general", "Key", "\"value\"");

        var exists = await repo.PropertyExistsAsync("general", "Key");
        exists.Should().BeTrue();

        var payload = await repo.GetPropertyPayloadAsync("general", "Key");
        payload.Should().Be("\"value\"");
    }
}
