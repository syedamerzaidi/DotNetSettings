using StackExchange.Redis;

namespace DotNetSettings.Repositories;

/// <summary>
/// Redis-backed settings repository using StackExchange.Redis.
/// Properties are stored as individual string keys; locks are tracked in a Redis Set.
/// </summary>
public sealed class RedisSettingsRepository : ISettingsRepository
{
    private readonly IDatabase _db;

    private static string PropertyKey(string group, string name) => $"settings:{group}:{name}";
    private static string LockKey(string group) => $"settings:locks:{group}";
    private static string IndexKey(string group) => $"settings:index:{group}";

    /// <summary>Initialises the repository with a Redis database connection.</summary>
    public RedisSettingsRepository(IConnectionMultiplexer redis)
        => _db = redis.GetDatabase();

    /// <inheritdoc/>
    public Dictionary<string, string?> GetPropertiesInGroup(string group)
    {
        var members = _db.SetMembers(IndexKey(group));
        var result = new Dictionary<string, string?>();
        foreach (var m in members)
        {
            var name = m.ToString();
            var val = _db.StringGet(PropertyKey(group, name));
            result[name] = val.IsNull ? null : val.ToString();
        }
        return result;
    }

    /// <inheritdoc/>
    public bool PropertyExists(string group, string name)
        => _db.KeyExists(PropertyKey(group, name));

    /// <inheritdoc/>
    public string? GetPropertyPayload(string group, string name)
    {
        var val = _db.StringGet(PropertyKey(group, name));
        return val.IsNull ? null : val.ToString();
    }

    /// <inheritdoc/>
    public void CreateProperty(string group, string name, string? payload)
    {
        _db.StringSet(PropertyKey(group, name), payload);
        _db.SetAdd(IndexKey(group), name);
    }

    /// <inheritdoc/>
    public void UpdateProperties(string group, Dictionary<string, string?> properties)
    {
        foreach (var (name, payload) in properties)
        {
            _db.StringSet(PropertyKey(group, name), payload);
            _db.SetAdd(IndexKey(group), name);
        }
    }

    /// <inheritdoc/>
    public void DeleteProperty(string group, string name)
    {
        _db.KeyDelete(PropertyKey(group, name));
        _db.SetRemove(IndexKey(group), name);
        _db.SetRemove(LockKey(group), name);
    }

    /// <inheritdoc/>
    public void LockProperties(string group, string[] names)
    {
        foreach (var name in names)
            _db.SetAdd(LockKey(group), name);
    }

    /// <inheritdoc/>
    public void UnlockProperties(string group, string[] names)
    {
        foreach (var name in names)
            _db.SetRemove(LockKey(group), name);
    }

    /// <inheritdoc/>
    public string[] GetLockedProperties(string group)
        => _db.SetMembers(LockKey(group)).Select(m => m.ToString()).ToArray();

    /// <inheritdoc/>
    public async Task<Dictionary<string, string?>> GetPropertiesInGroupAsync(string group, CancellationToken ct = default)
    {
        var members = await _db.SetMembersAsync(IndexKey(group));
        var result = new Dictionary<string, string?>();
        foreach (var m in members)
        {
            var name = m.ToString();
            var val = await _db.StringGetAsync(PropertyKey(group, name));
            result[name] = val.IsNull ? null : val.ToString();
        }
        return result;
    }

    /// <inheritdoc/>
    public Task<bool> PropertyExistsAsync(string group, string name, CancellationToken ct = default)
        => _db.KeyExistsAsync(PropertyKey(group, name));

    /// <inheritdoc/>
    public async Task<string?> GetPropertyPayloadAsync(string group, string name, CancellationToken ct = default)
    {
        var val = await _db.StringGetAsync(PropertyKey(group, name));
        return val.IsNull ? null : val.ToString();
    }

    /// <inheritdoc/>
    public async Task CreatePropertyAsync(string group, string name, string? payload, CancellationToken ct = default)
    {
        await _db.StringSetAsync(PropertyKey(group, name), payload);
        await _db.SetAddAsync(IndexKey(group), name);
    }

    /// <inheritdoc/>
    public async Task UpdatePropertiesAsync(string group, Dictionary<string, string?> properties, CancellationToken ct = default)
    {
        foreach (var (name, payload) in properties)
        {
            await _db.StringSetAsync(PropertyKey(group, name), payload);
            await _db.SetAddAsync(IndexKey(group), name);
        }
    }

    /// <inheritdoc/>
    public async Task DeletePropertyAsync(string group, string name, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync(PropertyKey(group, name));
        await _db.SetRemoveAsync(IndexKey(group), name);
        await _db.SetRemoveAsync(LockKey(group), name);
    }

    /// <inheritdoc/>
    public async Task LockPropertiesAsync(string group, string[] names, CancellationToken ct = default)
    {
        foreach (var name in names)
            await _db.SetAddAsync(LockKey(group), name);
    }

    /// <inheritdoc/>
    public async Task UnlockPropertiesAsync(string group, string[] names, CancellationToken ct = default)
    {
        foreach (var name in names)
            await _db.SetRemoveAsync(LockKey(group), name);
    }

    /// <inheritdoc/>
    public async Task<string[]> GetLockedPropertiesAsync(string group, CancellationToken ct = default)
    {
        var members = await _db.SetMembersAsync(LockKey(group));
        return members.Select(m => m.ToString()).ToArray();
    }
}
