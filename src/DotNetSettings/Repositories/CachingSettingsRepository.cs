using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace DotNetSettings.Repositories;

/// <summary>
/// Decorator that adds distributed caching on top of any <see cref="ISettingsRepository"/>.
/// All reads for a group are served from cache after the first load.
/// The cache is invalidated when <see cref="UpdateProperties"/> is called.
/// </summary>
public sealed class CachingSettingsRepository : ISettingsRepository
{
    private readonly ISettingsRepository _inner;
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _ttl;

    private static string CacheKey(string group) => $"dotnetsettings:{group}";

    /// <summary>Wraps <paramref name="inner"/> with distributed caching using the given TTL.</summary>
    public CachingSettingsRepository(ISettingsRepository inner, IDistributedCache cache, TimeSpan ttl)
    {
        _inner = inner;
        _cache = cache;
        _ttl = ttl;
    }

    /// <inheritdoc/>
    public Dictionary<string, string?> GetPropertiesInGroup(string group)
    {
        var key = CacheKey(group);
        var cached = _cache.GetString(key);
        if (cached is not null)
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(cached) ?? new();

        var data = _inner.GetPropertiesInGroup(group);
        _cache.SetString(key, JsonSerializer.Serialize(data),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _ttl });
        return data;
    }

    /// <inheritdoc/>
    public bool PropertyExists(string group, string name) => _inner.PropertyExists(group, name);

    /// <inheritdoc/>
    public string? GetPropertyPayload(string group, string name) => _inner.GetPropertyPayload(group, name);

    /// <inheritdoc/>
    public void CreateProperty(string group, string name, string? payload)
    {
        _inner.CreateProperty(group, name, payload);
        _cache.Remove(CacheKey(group));
    }

    /// <inheritdoc/>
    public void UpdateProperties(string group, Dictionary<string, string?> properties)
    {
        _inner.UpdateProperties(group, properties);
        _cache.Remove(CacheKey(group));
    }

    /// <inheritdoc/>
    public void DeleteProperty(string group, string name)
    {
        _inner.DeleteProperty(group, name);
        _cache.Remove(CacheKey(group));
    }

    /// <inheritdoc/>
    public void LockProperties(string group, string[] names) => _inner.LockProperties(group, names);

    /// <inheritdoc/>
    public void UnlockProperties(string group, string[] names) => _inner.UnlockProperties(group, names);

    /// <inheritdoc/>
    public string[] GetLockedProperties(string group) => _inner.GetLockedProperties(group);

    /// <inheritdoc/>
    public async Task<Dictionary<string, string?>> GetPropertiesInGroupAsync(string group, CancellationToken ct = default)
    {
        var key = CacheKey(group);
        var cached = await _cache.GetStringAsync(key, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(cached) ?? new();

        var data = await _inner.GetPropertiesInGroupAsync(group, ct);
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(data),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _ttl }, ct);
        return data;
    }

    /// <inheritdoc/>
    public Task<bool> PropertyExistsAsync(string group, string name, CancellationToken ct = default)
        => _inner.PropertyExistsAsync(group, name, ct);

    /// <inheritdoc/>
    public Task<string?> GetPropertyPayloadAsync(string group, string name, CancellationToken ct = default)
        => _inner.GetPropertyPayloadAsync(group, name, ct);

    /// <inheritdoc/>
    public async Task CreatePropertyAsync(string group, string name, string? payload, CancellationToken ct = default)
    {
        await _inner.CreatePropertyAsync(group, name, payload, ct);
        await _cache.RemoveAsync(CacheKey(group), ct);
    }

    /// <inheritdoc/>
    public async Task UpdatePropertiesAsync(string group, Dictionary<string, string?> properties, CancellationToken ct = default)
    {
        await _inner.UpdatePropertiesAsync(group, properties, ct);
        await _cache.RemoveAsync(CacheKey(group), ct);
    }

    /// <inheritdoc/>
    public async Task DeletePropertyAsync(string group, string name, CancellationToken ct = default)
    {
        await _inner.DeletePropertyAsync(group, name, ct);
        await _cache.RemoveAsync(CacheKey(group), ct);
    }

    /// <inheritdoc/>
    public Task LockPropertiesAsync(string group, string[] names, CancellationToken ct = default)
        => _inner.LockPropertiesAsync(group, names, ct);

    /// <inheritdoc/>
    public Task UnlockPropertiesAsync(string group, string[] names, CancellationToken ct = default)
        => _inner.UnlockPropertiesAsync(group, names, ct);

    /// <inheritdoc/>
    public Task<string[]> GetLockedPropertiesAsync(string group, CancellationToken ct = default)
        => _inner.GetLockedPropertiesAsync(group, ct);
}
