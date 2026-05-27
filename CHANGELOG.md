# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] — 2026-05-28

### Added

- `Settings` abstract base class with `Save()`, `Refresh()`, `Lock()`, `Unlock()`, `IsLocked()`, `GetLockedProperties()`, and `Fake<T>()` static helper
- `SettingsManager` — singleton orchestrator for load/save lifecycle
- `ISettingsRepository` interface with full sync and async API
- `DatabaseSettingsRepository` — EF Core implementation with `SettingsDbContext`
- `RedisSettingsRepository` — StackExchange.Redis implementation
- `FakeSettingsRepository` — in-memory implementation for unit tests
- `CachingSettingsRepository` — `IDistributedCache` decorator for any repository
- `[Encrypt]` attribute and `SettingsEncryptor` via ASP.NET Core Data Protection
- Built-in casts: `DateTimeSettingsCast`, `DateTimeOffsetSettingsCast`, `EnumSettingsCast`
- `ISettingsCast` interface for custom per-property and global casts
- `ISettingsMigration` / `SettingsMigrator` / `SettingsMigrationRunner` for schema migrations
- `ISettingsEventPublisher` lifecycle events: `LoadingSettingsEvent`, `SettingsLoadedEvent`, `SavingSettingsEvent`, `SettingsSavedEvent`
- `AddDotNetSettings()` and `AddSettings<T>()` DI extension methods
- 43 unit and integration tests (xUnit + FluentAssertions)
- Sample ASP.NET Core minimal-API project
