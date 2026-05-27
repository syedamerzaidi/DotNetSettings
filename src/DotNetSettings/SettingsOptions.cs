using DotNetSettings.Casts;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace DotNetSettings;

/// <summary>Configuration options for DotNetSettings.</summary>
public sealed class SettingsOptions
{
    internal enum BackendType { Database, Redis }

    internal BackendType Backend { get; private set; }
    internal string? ConnectionString { get; private set; }
    internal Action<DbContextOptionsBuilder>? DbConfigurator { get; private set; }
    internal bool CachingEnabled { get; private set; }
    internal TimeSpan CacheTtl { get; private set; } = TimeSpan.FromMinutes(5);
    internal byte[]? EncryptionKey { get; private set; }
    internal Dictionary<Type, ISettingsCast> GlobalCasts { get; } = new();
    internal List<Assembly> SettingsAssemblies { get; } = new();
    internal List<Assembly> MigrationAssemblies { get; } = new();

    /// <summary>
    /// Uses EF Core with the provided <paramref name="configure"/> action.
    /// Supply a database provider (e.g. <c>b.UseSqlite(…)</c> or <c>b.UseSqlServer(…)</c>).
    /// </summary>
    public SettingsOptions UseDatabase(string connectionString, Action<DbContextOptionsBuilder> configure)
    {
        Backend = BackendType.Database;
        ConnectionString = connectionString;
        DbConfigurator = configure;
        return this;
    }

    /// <summary>Uses StackExchange.Redis as the backing store.</summary>
    public SettingsOptions UseRedis(string connectionString)
    {
        Backend = BackendType.Redis;
        ConnectionString = connectionString;
        return this;
    }

    /// <summary>
    /// Enables distributed caching of settings groups with the given <paramref name="ttl"/>.
    /// Requires <c>IDistributedCache</c> to be registered in DI (e.g. <c>AddDistributedMemoryCache()</c>).
    /// </summary>
    public SettingsOptions EnableCaching(TimeSpan ttl)
    {
        CachingEnabled = true;
        CacheTtl = ttl;
        return this;
    }

    /// <summary>Registers a global cast for <typeparamref name="TType"/>.</summary>
    public SettingsOptions AddGlobalCast<TType, TCast>() where TCast : ISettingsCast, new()
    {
        GlobalCasts[typeof(TType)] = new TCast();
        return this;
    }

    /// <summary>Registers a global cast instance for <typeparamref name="TType"/>.</summary>
    public SettingsOptions AddGlobalCast<TType>(ISettingsCast cast)
    {
        GlobalCasts[typeof(TType)] = cast;
        return this;
    }

    /// <summary>
    /// Enables AES-256-GCM encryption for properties marked with <c>[Encrypt]</c>.
    /// <paramref name="base64Key"/> must be a base64-encoded 32-byte (256-bit) key.
    /// Generate one with: <c>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))</c>.
    /// </summary>
    public SettingsOptions UseEncryption(string base64Key)
    {
        var key = Convert.FromBase64String(base64Key);
        if (key.Length != 32)
            throw new ArgumentException("Encryption key must be 32 bytes (256 bits).", nameof(base64Key));
        EncryptionKey = key;
        return this;
    }

    /// <summary>Auto-discovers all <see cref="Settings"/> subclasses in the given assembly.</summary>
    public SettingsOptions RegisterSettingsFromAssembly(Assembly assembly)
    {
        SettingsAssemblies.Add(assembly);
        return this;
    }

    /// <summary>Auto-discovers all <see cref="Migrations.ISettingsMigration"/> implementations in the given assembly.</summary>
    public SettingsOptions RegisterMigrationsFromAssembly(Assembly assembly)
    {
        MigrationAssemblies.Add(assembly);
        return this;
    }
}
