using DotNetSettings.Encryption;
using DotNetSettings.Models;
using DotNetSettings.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace DotNetSettings.Migrations;

/// <summary>
/// Discovers <see cref="ISettingsMigration"/> implementations in registered assemblies,
/// compares them against the <c>settings_migrations</c> table, and runs any that have
/// not yet been applied — in alphabetical order by class name.
/// </summary>
public sealed class SettingsMigrationRunner
{
    private readonly ISettingsRepository _repository;
    private readonly SettingsDbContext _db;
    private readonly SettingsEncryptor? _encryptor;
    private readonly IEnumerable<Assembly> _assemblies;

    /// <summary>Initialises the runner with its dependencies.</summary>
    public SettingsMigrationRunner(
        ISettingsRepository repository,
        SettingsDbContext db,
        IEnumerable<Assembly> assemblies,
        SettingsEncryptor? encryptor = null)
    {
        _repository = repository;
        _db = db;
        _encryptor = encryptor;
        _assemblies = assemblies;
    }

    /// <summary>
    /// Ensures the migration history table exists then runs all outstanding migrations.
    /// Safe to call multiple times — already-applied migrations are skipped.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        await _db.Database.EnsureCreatedAsync(ct);

        var appliedList = await _db.SettingsMigrations
                                   .Select(m => m.MigrationName)
                                   .ToListAsync(ct);
        var applied = new HashSet<string>(appliedList);

        var pending = DiscoverMigrations()
            .Where(m => !applied.Contains(m.GetType().FullName ?? m.GetType().Name))
            .OrderBy(m => m.GetType().Name)
            .ToList();

        var migrator = new SettingsMigrator(_repository, _encryptor);

        foreach (var migration in pending)
        {
            migration.Up(migrator);

            var name = migration.GetType().FullName ?? migration.GetType().Name;
            _db.SettingsMigrations.Add(new SettingsMigrationRecord
            {
                MigrationName = name,
                AppliedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }
    }

    private IEnumerable<ISettingsMigration> DiscoverMigrations()
    {
        var migrationType = typeof(ISettingsMigration);
        return _assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => migrationType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .Select(t => (ISettingsMigration)Activator.CreateInstance(t)!);
    }
}
