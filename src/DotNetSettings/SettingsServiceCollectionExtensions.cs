using DotNetSettings.Casts;
using DotNetSettings.Encryption;
using DotNetSettings.Events;
using DotNetSettings.Migrations;
using DotNetSettings.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace DotNetSettings;

/// <summary>Extension methods that wire up DotNetSettings in the DI container.</summary>
public static class SettingsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the DotNetSettings infrastructure. Call <see cref="AddSettings{T}"/>
    /// afterwards to expose individual settings classes for injection.
    /// </summary>
    public static IServiceCollection AddDotNetSettings(
        this IServiceCollection services,
        Action<SettingsOptions> configure)
    {
        var options = new SettingsOptions();
        configure(options);

        options.GlobalCasts.TryAdd(typeof(DateTime), new DateTimeSettingsCast());
        options.GlobalCasts.TryAdd(typeof(DateTimeOffset), new DateTimeOffsetSettingsCast());

        switch (options.Backend)
        {
            case SettingsOptions.BackendType.Database:
                services.AddDbContext<SettingsDbContext>(options.DbConfigurator!);
                services.AddScoped<DatabaseSettingsRepository>();
                services.AddScoped<ISettingsRepository>(sp =>
                {
                    ISettingsRepository inner = sp.GetRequiredService<DatabaseSettingsRepository>();
                    return WrapWithCache(sp, inner, options);
                });
                break;

            case SettingsOptions.BackendType.Redis:
                services.AddSingleton<IConnectionMultiplexer>(
                    _ => ConnectionMultiplexer.Connect(options.ConnectionString!));
                services.AddSingleton<RedisSettingsRepository>();
                services.AddSingleton<ISettingsRepository>(sp =>
                {
                    ISettingsRepository inner = sp.GetRequiredService<RedisSettingsRepository>();
                    return WrapWithCache(sp, inner, options);
                });
                break;
        }

        SettingsEncryptor? encryptor = options.EncryptionKey is { } key
            ? new SettingsEncryptor(key)
            : null;

        if (encryptor is not null)
            services.AddSingleton(encryptor);

        services.TryAddSingleton<ISettingsEventPublisher, NoOpSettingsEventPublisher>();

        var globalCasts = options.GlobalCasts;
        services.AddSingleton<SettingsManager>(sp => new SettingsManager(
            sp.GetRequiredService<ISettingsRepository>(),
            namedRepositories: null,
            globalCasts: globalCasts,
            encryptor: encryptor,
            eventPublisher: sp.GetRequiredService<ISettingsEventPublisher>()
        ));

        if (options.MigrationAssemblies.Count > 0)
        {
            services.AddScoped<SettingsMigrationRunner>(sp => new SettingsMigrationRunner(
                sp.GetRequiredService<ISettingsRepository>(),
                sp.GetRequiredService<SettingsDbContext>(),
                options.MigrationAssemblies,
                encryptor
            ));
        }

        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="T"/> as a scoped service that is automatically
    /// loaded from the repository when resolved from DI.
    /// </summary>
    public static IServiceCollection AddSettings<T>(this IServiceCollection services)
        where T : Settings
    {
        services.AddScoped<T>(sp =>
        {
            var instance = Activator.CreateInstance<T>();
            var manager = sp.GetRequiredService<SettingsManager>();
            var repo = sp.GetRequiredService<ISettingsRepository>();
            instance.Repository = repo;
            instance.Manager = manager;
            manager.Load(instance);
            return instance;
        });
        return services;
    }

    private static ISettingsRepository WrapWithCache(
        IServiceProvider sp,
        ISettingsRepository inner,
        SettingsOptions options)
    {
        if (!options.CachingEnabled) return inner;
        var cache = sp.GetService<IDistributedCache>();
        return cache is null ? inner : new CachingSettingsRepository(inner, cache, options.CacheTtl);
    }
}
