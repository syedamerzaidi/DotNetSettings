using DotNetSettings.Models;
using Microsoft.EntityFrameworkCore;

namespace DotNetSettings.Repositories;

/// <summary>
/// EF Core DbContext that owns the <c>settings</c> and <c>settings_migrations</c> tables.
/// Can be used standalone or have its entity configuration applied to an existing DbContext
/// via <see cref="ApplySettingsConfiguration"/>.
/// </summary>
public class SettingsDbContext : DbContext
{
    /// <summary>Initialises the context with the supplied options.</summary>
    public SettingsDbContext(DbContextOptions<SettingsDbContext> options) : base(options) { }

    /// <summary>All stored setting properties.</summary>
    public DbSet<SettingRecord> Settings => Set<SettingRecord>();

    /// <summary>Migration history table.</summary>
    public DbSet<SettingsMigrationRecord> SettingsMigrations => Set<SettingsMigrationRecord>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ApplySettingsConfiguration(modelBuilder);
    }

    /// <summary>
    /// Applies the entity configuration for <see cref="SettingRecord"/> and
    /// <see cref="SettingsMigrationRecord"/> to <paramref name="modelBuilder"/>.
    /// Call this from your own <c>OnModelCreating</c> to share a single DbContext.
    /// </summary>
    public static void ApplySettingsConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SettingRecord>(e =>
        {
            e.ToTable("settings");
            e.HasKey(r => r.Id);
            e.Property(r => r.Group).IsRequired().HasMaxLength(255);
            e.Property(r => r.Name).IsRequired().HasMaxLength(255);
            e.HasIndex(r => new { r.Group, r.Name }).IsUnique();
        });

        modelBuilder.Entity<SettingsMigrationRecord>(e =>
        {
            e.ToTable("settings_migrations");
            e.HasKey(r => r.Id);
            e.Property(r => r.MigrationName).IsRequired().HasMaxLength(512);
        });
    }
}
