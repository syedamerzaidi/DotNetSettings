# DotNetSettings

A strongly-typed, database-backed application settings library for .NET.

Store your application settings in a database or Redis, update them at runtime without redeployment, and access them anywhere via dependency injection with full type safety.

## Features

- Strongly typed settings classes (string, bool, int, DateTime, enums, complex objects)
- Database (EF Core) and Redis (StackExchange.Redis) storage backends
- Settings migrations (seed, rename, update, delete values)
- Built-in caching via `IDistributedCache`
- Per-property encryption via ASP.NET Core Data Protection
- Property locking (make a setting read-only at runtime)
- Lifecycle events (Loading, Loaded, Saving, Saved)
- Pluggable `ISettingsRepository` interface for custom backends
- Test helpers (`Settings.Fake<T>()` and `FakeSettingsRepository`)

## Compatibility

Targets **net6.0** and **net8.0**:

| Runtime | TFM selected |
|---------|-------------|
| .NET 6, .NET 7 | `net6.0` |
| .NET 8, .NET 9, .NET 10 | `net8.0` |

## Installation

```bash
dotnet add package DotNetSettings
```

For the EF Core database backend, also add a database provider:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite   # or SqlServer, Npgsql, etc.
```

## Quick Start

### 1. Define a settings class

```csharp
using DotNetSettings;
using DotNetSettings.Attributes;

public class GeneralSettings : Settings
{
    public string SiteName { get; set; } = "";
    public bool SiteActive { get; set; }
    public int MaxUploadSizeMb { get; set; }
    public DateTime LaunchedAt { get; set; }

    [Encrypt]
    public string ApiKey { get; set; } = "";

    public override string Group => "general";
}
```

### 2. Register with DI

```csharp
builder.Services.AddDotNetSettings(options =>
{
    options.UseDatabase("Data Source=app.db",
        b => b.UseSqlite("Data Source=app.db"));

    options.EnableCaching(ttl: TimeSpan.FromMinutes(5));

    options.RegisterSettingsFromAssembly(typeof(GeneralSettings).Assembly);
    options.RegisterMigrationsFromAssembly(typeof(CreateInitialSettings).Assembly);
});

builder.Services.AddSettings<GeneralSettings>();
```

Add `AddDistributedMemoryCache()` before `AddDotNetSettings()` when caching is enabled.

### 3. Use in your code

```csharp
app.MapGet("/settings", (GeneralSettings settings) => new
{
    settings.SiteName,
    settings.SiteActive
});

app.MapPost("/settings", (GeneralSettings settings, UpdateInput input) =>
{
    settings.SiteName = input.SiteName;
    settings.Save();
    return Results.Ok();
});
```

## Settings Classes

Every settings class:

- Inherits from `Settings`
- Declares a unique `Group` property (used as the namespace in the repository)
- Has public get/set properties for each setting value

```csharp
public class MailSettings : Settings
{
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "";
    public bool SendWelcomeEmail { get; set; }

    public override string Group => "mail";
}
```

### Multiple repositories

Override `RepositoryName` to direct a settings class to a named repository:

```csharp
public override string? RepositoryName => "redis";
```

## Migrations

Create a class implementing `ISettingsMigration` for each schema change:

```csharp
public class CreateGeneralSettings : ISettingsMigration
{
    public void Up(SettingsMigrator migrator)
    {
        migrator.InGroup("general", m =>
        {
            m.Add("SiteName", "My Site");
            m.Add("SiteActive", true);
            m.Add("MaxUploadSizeMb", 10);
            m.AddEncrypted("ApiKey", "");
        });
    }
}
```

Migrations run in alphabetical order by class name and are tracked in a `settings_migrations` table.

Run migrations at startup:

```csharp
using var scope = app.Services.CreateScope();
var runner = scope.ServiceProvider.GetRequiredService<SettingsMigrationRunner>();
await runner.RunAsync();
```

### Migrator API

| Method | Description |
|--------|-------------|
| `Add(key, value)` | Create a property if it doesn't exist |
| `AddEncrypted(key, value)` | Create an encrypted property |
| `Rename(fromKey, toKey)` | Move a property to a new key |
| `Update(key, transform)` | Transform an existing value |
| `Delete(key)` | Remove a property |
| `Exists(key)` | Check whether a property exists |
| `Encrypt(key)` | Encrypt an existing plaintext property |
| `Decrypt(key)` | Decrypt an existing encrypted property |
| `InGroup(group, action)` | Scope operations to a group |

Keys use the format `"group.PropertyName"` outside `InGroup`, or just `"PropertyName"` inside it.

## Casts

### Built-in casts

| Cast | Type |
|------|------|
| `DateTimeSettingsCast` | `DateTime` → ISO 8601 string |
| `DateTimeOffsetSettingsCast` | `DateTimeOffset` → ISO 8601 string |
| `EnumSettingsCast` | `enum` → string name |

Register global casts in `AddDotNetSettings()`:

```csharp
options.AddGlobalCast<DateTime, DateTimeSettingsCast>();
options.AddGlobalCast<DateTimeOffset, DateTimeOffsetSettingsCast>();
```

### Custom casts

Implement `ISettingsCast` and return it from `GetCasts()`:

```csharp
public class MySettings : Settings
{
    public MyType Value { get; set; } = new();

    public override Dictionary<string, ISettingsCast> GetCasts() => new()
    {
        [nameof(Value)] = new MyTypeCast()
    };
}
```

## Encryption

Mark individual properties with `[Encrypt]`:

```csharp
[Encrypt]
public string ApiKey { get; set; } = "";
```

Or override `GetEncryptedProperties()` on the settings class:

```csharp
public override string[] GetEncryptedProperties() =>
    new[] { nameof(ApiKey), nameof(SecretToken) };
```

Encryption uses ASP.NET Core Data Protection. Call `services.AddDataProtection()` before `AddDotNetSettings()`.

## Locking

Lock a property to prevent it being overwritten on `Save()`:

```csharp
settings.Lock(nameof(GeneralSettings.SiteName));

// Check
bool locked = settings.IsLocked(nameof(GeneralSettings.SiteName));

// List all
string[] locked = settings.GetLockedProperties();

// Unlock
settings.Unlock(nameof(GeneralSettings.SiteName));
```

Locked properties retain their stored value even if the in-memory instance has a different value.

## Caching

Enable caching in `AddDotNetSettings()`:

```csharp
services.AddDistributedMemoryCache(); // or AddStackExchangeRedisCache(...)

builder.Services.AddDotNetSettings(options =>
{
    options.EnableCaching(ttl: TimeSpan.FromMinutes(5));
    // ...
});
```

The cache is keyed by `dotnetsettings:{group}` and invalidated automatically on `Save()`.

## Events

Register a custom `ISettingsEventPublisher` to hook into the settings lifecycle:

```csharp
services.AddSingleton<ISettingsEventPublisher, MyEventPublisher>();
```

Events fired:

| Event | When |
|-------|------|
| `LoadingSettingsEvent` | Before loading from repository |
| `SettingsLoadedEvent` | After loading |
| `SavingSettingsEvent` | Before saving to repository |
| `SettingsSavedEvent` | After saving |

## Sample Projects

| Project | Template | What it shows |
|---------|----------|---------------|
| `samples/DotNetSettings.Sample` | ASP.NET Core Minimal API | DI, endpoints, migrations, caching, locking |
| `samples/DotNetSettings.Sample.ConsoleApp` | Console App (Generic Host) | Manual DI setup, save/lock without a web server |
| `samples/DotNetSettings.Sample.Worker` | Worker Service | Settings refresh in a `BackgroundService` |

## Testing

### `Settings.Fake<T>()`

Create a settings instance without any repository for unit tests:

```csharp
var settings = Settings.Fake<GeneralSettings>(new()
{
    [nameof(GeneralSettings.SiteName)] = "Test Site",
    [nameof(GeneralSettings.SiteActive)] = true
});

Assert.Equal("Test Site", settings.SiteName);
```

### `FakeSettingsRepository`

Use an in-memory repository for integration tests:

```csharp
var repo = new FakeSettingsRepository();
repo.SetPropertyInternal("general", "SiteName", "\"My Site\"");

var manager = new SettingsManager(repo);
var settings = new GeneralSettings();
settings.Repository = repo;
settings.Manager = manager;
manager.Load(settings);

Assert.Equal("My Site", settings.SiteName);
```
