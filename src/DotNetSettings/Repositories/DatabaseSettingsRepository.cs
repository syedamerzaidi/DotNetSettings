using DotNetSettings.Models;
using Microsoft.EntityFrameworkCore;

namespace DotNetSettings.Repositories;

/// <summary>
/// EF Core–backed settings repository. Stores all settings in a relational database.
/// </summary>
public sealed class DatabaseSettingsRepository : ISettingsRepository
{
    private readonly SettingsDbContext _db;

    /// <summary>Initialises the repository with the supplied DbContext.</summary>
    public DatabaseSettingsRepository(SettingsDbContext db) => _db = db;

    // ── Synchronous ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Dictionary<string, string?> GetPropertiesInGroup(string group)
        => _db.Settings
              .Where(r => r.Group == group)
              .ToDictionary(r => r.Name, r => r.Payload);

    /// <inheritdoc/>
    public bool PropertyExists(string group, string name)
        => _db.Settings.Any(r => r.Group == group && r.Name == name);

    /// <inheritdoc/>
    public string? GetPropertyPayload(string group, string name)
        => _db.Settings.Where(r => r.Group == group && r.Name == name)
                       .Select(r => r.Payload)
                       .FirstOrDefault();

    /// <inheritdoc/>
    public void CreateProperty(string group, string name, string? payload)
    {
        _db.Settings.Add(new SettingRecord { Group = group, Name = name, Payload = payload });
        _db.SaveChanges();
    }

    /// <inheritdoc/>
    public void UpdateProperties(string group, Dictionary<string, string?> properties)
    {
        var keys = properties.Keys.ToList();
        var existing = _db.Settings
                          .Where(r => r.Group == group && keys.Contains(r.Name))
                          .ToDictionary(r => r.Name);

        foreach (var (name, payload) in properties)
        {
            if (existing.TryGetValue(name, out var record))
                record.Payload = payload;
            else
                _db.Settings.Add(new SettingRecord { Group = group, Name = name, Payload = payload });
        }
        _db.SaveChanges();
    }

    /// <inheritdoc/>
    public void DeleteProperty(string group, string name)
    {
        var record = _db.Settings.FirstOrDefault(r => r.Group == group && r.Name == name);
        if (record is not null)
        {
            _db.Settings.Remove(record);
            _db.SaveChanges();
        }
    }

    /// <inheritdoc/>
    public void LockProperties(string group, string[] names)
    {
        var namesList = names.ToList();
        var records = _db.Settings
                         .Where(r => r.Group == group && namesList.Contains(r.Name))
                         .ToList();
        foreach (var r in records) r.Locked = true;
        _db.SaveChanges();
    }

    /// <inheritdoc/>
    public void UnlockProperties(string group, string[] names)
    {
        var namesList = names.ToList();
        var records = _db.Settings
                         .Where(r => r.Group == group && namesList.Contains(r.Name))
                         .ToList();
        foreach (var r in records) r.Locked = false;
        _db.SaveChanges();
    }

    /// <inheritdoc/>
    public string[] GetLockedProperties(string group)
        => _db.Settings
              .Where(r => r.Group == group && r.Locked)
              .Select(r => r.Name)
              .ToArray();

    // ── Asynchronous ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Dictionary<string, string?>> GetPropertiesInGroupAsync(string group, CancellationToken ct = default)
        => await _db.Settings
                    .Where(r => r.Group == group)
                    .ToDictionaryAsync(r => r.Name, r => r.Payload, ct);

    /// <inheritdoc/>
    public Task<bool> PropertyExistsAsync(string group, string name, CancellationToken ct = default)
        => _db.Settings.AnyAsync(r => r.Group == group && r.Name == name, ct);

    /// <inheritdoc/>
    public Task<string?> GetPropertyPayloadAsync(string group, string name, CancellationToken ct = default)
        => _db.Settings.Where(r => r.Group == group && r.Name == name)
                       .Select(r => r.Payload)
                       .FirstOrDefaultAsync(ct);

    /// <inheritdoc/>
    public async Task CreatePropertyAsync(string group, string name, string? payload, CancellationToken ct = default)
    {
        _db.Settings.Add(new SettingRecord { Group = group, Name = name, Payload = payload });
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UpdatePropertiesAsync(string group, Dictionary<string, string?> properties, CancellationToken ct = default)
    {
        var keys = properties.Keys.ToList();
        var existing = await _db.Settings
                                .Where(r => r.Group == group && keys.Contains(r.Name))
                                .ToDictionaryAsync(r => r.Name, ct);

        foreach (var (name, payload) in properties)
        {
            if (existing.TryGetValue(name, out var record))
                record.Payload = payload;
            else
                _db.Settings.Add(new SettingRecord { Group = group, Name = name, Payload = payload });
        }
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeletePropertyAsync(string group, string name, CancellationToken ct = default)
    {
        var record = await _db.Settings.FirstOrDefaultAsync(r => r.Group == group && r.Name == name, ct);
        if (record is not null)
        {
            _db.Settings.Remove(record);
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc/>
    public async Task LockPropertiesAsync(string group, string[] names, CancellationToken ct = default)
    {
        var namesList = names.ToList();
        var records = await _db.Settings
                               .Where(r => r.Group == group && namesList.Contains(r.Name))
                               .ToListAsync(ct);
        foreach (var r in records) r.Locked = true;
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UnlockPropertiesAsync(string group, string[] names, CancellationToken ct = default)
    {
        var namesList = names.ToList();
        var records = await _db.Settings
                               .Where(r => r.Group == group && namesList.Contains(r.Name))
                               .ToListAsync(ct);
        foreach (var r in records) r.Locked = false;
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<string[]> GetLockedPropertiesAsync(string group, CancellationToken ct = default)
        => await _db.Settings
                    .Where(r => r.Group == group && r.Locked)
                    .Select(r => r.Name)
                    .ToArrayAsync(ct);
}
