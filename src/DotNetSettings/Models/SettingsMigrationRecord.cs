namespace DotNetSettings.Models;

/// <summary>Tracks which settings migrations have already been applied.</summary>
public class SettingsMigrationRecord
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Full type name of the migration class that was run.</summary>
    public string MigrationName { get; set; } = "";

    /// <summary>UTC timestamp when the migration was applied.</summary>
    public DateTime AppliedAt { get; set; }
}
