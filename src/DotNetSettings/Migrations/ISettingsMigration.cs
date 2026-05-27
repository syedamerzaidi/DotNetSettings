namespace DotNetSettings.Migrations;

/// <summary>
/// Represents a single, idempotent migration that seeds, renames, transforms, or removes
/// settings properties. Implement this interface and register the assembly via
/// <c>options.RegisterMigrationsFromAssembly()</c>.
/// </summary>
public interface ISettingsMigration
{
    /// <summary>Applies the migration using the supplied <paramref name="migrator"/>.</summary>
    void Up(SettingsMigrator migrator);
}
