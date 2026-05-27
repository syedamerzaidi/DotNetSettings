using DotNetSettings.Migrations;
using DotNetSettings.Repositories;
using DotNetSettings.Tests.Helpers;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DotNetSettings.Tests.Settings;

public class MigrationTests
{
    private static SettingsMigrator CreateMigrator(FakeSettingsRepository repo)
        => new SettingsMigrator(repo, null, null);

    [Fact]
    public void Add_creates_property_with_default_value()
    {
        var repo = new FakeSettingsRepository();
        var migrator = CreateMigrator(repo);

        migrator.Add("general.SiteName", "Default Site");

        repo.PropertyExists("general", "SiteName").Should().BeTrue();
        var payload = repo.GetPropertyPayload("general", "SiteName");
        JsonSerializer.Deserialize<string>(payload!).Should().Be("Default Site");
    }

    [Fact]
    public void Add_does_not_overwrite_existing_property()
    {
        var repo = new FakeSettingsRepository();
        repo.SetPropertyInternal("general", "SiteName", "\"Existing\"");
        var migrator = CreateMigrator(repo);

        migrator.Add("general.SiteName", "Should Not Overwrite");

        var payload = repo.GetPropertyPayload("general", "SiteName");
        JsonSerializer.Deserialize<string>(payload!).Should().Be("Existing");
    }

    [Fact]
    public void Rename_moves_property_to_new_key()
    {
        var repo = new FakeSettingsRepository();
        repo.SetPropertyInternal("general", "OldName", "\"value\"");
        var migrator = CreateMigrator(repo);

        migrator.Rename("general.OldName", "general.NewName");

        repo.PropertyExists("general", "OldName").Should().BeFalse();
        repo.PropertyExists("general", "NewName").Should().BeTrue();
        var payload = repo.GetPropertyPayload("general", "NewName");
        JsonSerializer.Deserialize<string>(payload!).Should().Be("value");
    }

    [Fact]
    public void Update_transforms_existing_value()
    {
        var repo = new FakeSettingsRepository();
        repo.SetPropertyInternal("general", "Count", "5");
        var migrator = CreateMigrator(repo);

        migrator.Update("general.Count", old =>
        {
            var n = JsonSerializer.Deserialize<int>(old ?? "0");
            return JsonSerializer.Serialize(n + 1);
        });

        var payload = repo.GetPropertyPayload("general", "Count");
        JsonSerializer.Deserialize<int>(payload!).Should().Be(6);
    }

    [Fact]
    public void Delete_removes_property()
    {
        var repo = new FakeSettingsRepository();
        repo.SetPropertyInternal("general", "ToDelete", "\"bye\"");
        var migrator = CreateMigrator(repo);

        migrator.Delete("general.ToDelete");

        repo.PropertyExists("general", "ToDelete").Should().BeFalse();
    }

    [Fact]
    public void Exists_returns_correct_state()
    {
        var repo = new FakeSettingsRepository();
        repo.SetPropertyInternal("general", "Present", "\"yes\"");
        var migrator = CreateMigrator(repo);

        migrator.Exists("general.Present").Should().BeTrue();
        migrator.Exists("general.Missing").Should().BeFalse();
    }

    [Fact]
    public void InGroup_scopes_all_operations()
    {
        var repo = new FakeSettingsRepository();
        var migrator = CreateMigrator(repo);

        migrator.InGroup("mail", m =>
        {
            m.Add("FromAddress", "test@example.com");
            m.Add("SendWelcomeEmail", true);
        });

        repo.PropertyExists("mail", "FromAddress").Should().BeTrue();
        repo.PropertyExists("mail", "SendWelcomeEmail").Should().BeTrue();
        var payload = repo.GetPropertyPayload("mail", "FromAddress");
        JsonSerializer.Deserialize<string>(payload!).Should().Be("test@example.com");
    }

    [Fact]
    public async Task Already_run_migrations_are_not_re_run()
    {
        var repo = new FakeSettingsRepository();

        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<SettingsDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new SettingsDbContext(options);

        var runner = new SettingsMigrationRunner(
            repo,
            db,
            new[] { typeof(SeedMigration).Assembly });

        await runner.RunAsync();
        await runner.RunAsync(); // second run — must be idempotent

        var payload = repo.GetPropertyPayload("general", "RunCount");
        JsonSerializer.Deserialize<int>(payload!).Should().Be(1);
    }
}

/// <summary>Counter migration: adds the property on first run, increments on subsequent (should never re-run).</summary>
public class SeedMigration : ISettingsMigration
{
    public void Up(SettingsMigrator migrator)
    {
        if (!migrator.Exists("general.RunCount"))
            migrator.Add("general.RunCount", 1);
        else
            migrator.Update("general.RunCount", p =>
            {
                var n = JsonSerializer.Deserialize<int>(p ?? "0");
                return JsonSerializer.Serialize(n + 1);
            });
    }
}
