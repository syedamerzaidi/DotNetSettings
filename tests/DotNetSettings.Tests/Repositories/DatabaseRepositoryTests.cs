using DotNetSettings.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DotNetSettings.Tests.Repositories;

public sealed class DatabaseRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SettingsDbContext _db;
    private readonly DatabaseSettingsRepository _repo;

    public DatabaseRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SettingsDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new SettingsDbContext(options);
        _db.Database.EnsureCreated();
        _repo = new DatabaseSettingsRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void GetPropertiesInGroup_returns_all_properties_for_group()
    {
        _repo.CreateProperty("general", "SiteName", "\"My Site\"");
        _repo.CreateProperty("general", "SiteActive", "true");
        _repo.CreateProperty("mail", "FromAddress", "\"a@b.com\"");

        var props = _repo.GetPropertiesInGroup("general");

        props.Should().HaveCount(2);
        props.Should().ContainKey("SiteName");
        props.Should().ContainKey("SiteActive");
        props.Should().NotContainKey("FromAddress");
    }

    [Fact]
    public void CreateProperty_inserts_a_new_record()
    {
        _repo.CreateProperty("general", "Key", "\"value\"");

        _repo.PropertyExists("general", "Key").Should().BeTrue();
        _repo.GetPropertyPayload("general", "Key").Should().Be("\"value\"");
    }

    [Fact]
    public void UpdateProperties_upserts_correctly()
    {
        _repo.CreateProperty("general", "Existing", "\"old\"");

        _repo.UpdateProperties("general", new()
        {
            ["Existing"] = "\"new\"",
            ["New"] = "\"created\""
        });

        _repo.GetPropertyPayload("general", "Existing").Should().Be("\"new\"");
        _repo.GetPropertyPayload("general", "New").Should().Be("\"created\"");
    }

    [Fact]
    public void DeleteProperty_removes_the_record()
    {
        _repo.CreateProperty("general", "ToDelete", "\"bye\"");

        _repo.DeleteProperty("general", "ToDelete");

        _repo.PropertyExists("general", "ToDelete").Should().BeFalse();
    }

    [Fact]
    public void LockProperties_sets_locked_flag()
    {
        _repo.CreateProperty("general", "SiteName", "\"x\"");

        _repo.LockProperties("general", new[] { "SiteName" });

        _repo.GetLockedProperties("general").Should().Contain("SiteName");
    }

    [Fact]
    public void UnlockProperties_clears_locked_flag()
    {
        _repo.CreateProperty("general", "SiteName", "\"x\"");
        _repo.LockProperties("general", new[] { "SiteName" });

        _repo.UnlockProperties("general", new[] { "SiteName" });

        _repo.GetLockedProperties("general").Should().BeEmpty();
    }

    [Fact]
    public void GetLockedProperties_returns_only_locked_names()
    {
        _repo.CreateProperty("general", "A", "\"1\"");
        _repo.CreateProperty("general", "B", "\"2\"");
        _repo.CreateProperty("general", "C", "\"3\"");

        _repo.LockProperties("general", new[] { "A", "C" });

        var locked = _repo.GetLockedProperties("general");
        locked.Should().Contain("A");
        locked.Should().Contain("C");
        locked.Should().NotContain("B");
    }

    [Fact]
    public async Task Async_GetPropertiesInGroup_works()
    {
        _repo.CreateProperty("general", "Prop", "\"val\"");

        var props = await _repo.GetPropertiesInGroupAsync("general");

        props.Should().ContainKey("Prop");
    }

    [Fact]
    public async Task Async_UpdateProperties_upserts()
    {
        await _repo.UpdatePropertiesAsync("general", new() { ["K"] = "\"v\"" });

        var payload = await _repo.GetPropertyPayloadAsync("general", "K");
        payload.Should().Be("\"v\"");
    }

    [Fact]
    public async Task Async_LockUnlock_works()
    {
        _repo.CreateProperty("general", "P", "\"1\"");

        await _repo.LockPropertiesAsync("general", new[] { "P" });
        var locked = await _repo.GetLockedPropertiesAsync("general");
        locked.Should().Contain("P");

        await _repo.UnlockPropertiesAsync("general", new[] { "P" });
        locked = await _repo.GetLockedPropertiesAsync("general");
        locked.Should().BeEmpty();
    }
}
