namespace DotNetSettings.Repositories;

/// <summary>
/// In-memory settings repository for use in unit and integration tests.
/// No database or external dependencies required.
/// </summary>
public sealed class FakeSettingsRepository : ISettingsRepository
{
    private readonly Dictionary<string, Dictionary<string, string?>> _store = new();
    private readonly Dictionary<string, HashSet<string>> _locks = new();

    private Dictionary<string, string?> GroupStore(string group)
    {
        if (!_store.TryGetValue(group, out var d))
            _store[group] = d = new();
        return d;
    }

    private HashSet<string> GroupLocks(string group)
    {
        if (!_locks.TryGetValue(group, out var s))
            _locks[group] = s = new();
        return s;
    }

    /// <summary>Directly seeds a value without going through JSON serialization.</summary>
    public void SetPropertyInternal(string group, string name, string? payload)
        => GroupStore(group)[name] = payload;

    /// <inheritdoc/>
    public Dictionary<string, string?> GetPropertiesInGroup(string group)
        => new(GroupStore(group));

    /// <inheritdoc/>
    public bool PropertyExists(string group, string name)
        => GroupStore(group).ContainsKey(name);

    /// <inheritdoc/>
    public string? GetPropertyPayload(string group, string name)
        => GroupStore(group).TryGetValue(name, out var v) ? v : null;

    /// <inheritdoc/>
    public void CreateProperty(string group, string name, string? payload)
        => GroupStore(group)[name] = payload;

    /// <inheritdoc/>
    public void UpdateProperties(string group, Dictionary<string, string?> properties)
    {
        var store = GroupStore(group);
        foreach (var (name, payload) in properties)
            store[name] = payload;
    }

    /// <inheritdoc/>
    public void DeleteProperty(string group, string name)
    {
        GroupStore(group).Remove(name);
        GroupLocks(group).Remove(name);
    }

    /// <inheritdoc/>
    public void LockProperties(string group, string[] names)
    {
        var locks = GroupLocks(group);
        foreach (var n in names) locks.Add(n);
    }

    /// <inheritdoc/>
    public void UnlockProperties(string group, string[] names)
    {
        var locks = GroupLocks(group);
        foreach (var n in names) locks.Remove(n);
    }

    /// <inheritdoc/>
    public string[] GetLockedProperties(string group)
        => GroupLocks(group).ToArray();

    /// <inheritdoc/>
    public Task<Dictionary<string, string?>> GetPropertiesInGroupAsync(string group, CancellationToken ct = default)
        => Task.FromResult(GetPropertiesInGroup(group));

    /// <inheritdoc/>
    public Task<bool> PropertyExistsAsync(string group, string name, CancellationToken ct = default)
        => Task.FromResult(PropertyExists(group, name));

    /// <inheritdoc/>
    public Task<string?> GetPropertyPayloadAsync(string group, string name, CancellationToken ct = default)
        => Task.FromResult(GetPropertyPayload(group, name));

    /// <inheritdoc/>
    public Task CreatePropertyAsync(string group, string name, string? payload, CancellationToken ct = default)
    {
        CreateProperty(group, name, payload);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UpdatePropertiesAsync(string group, Dictionary<string, string?> properties, CancellationToken ct = default)
    {
        UpdateProperties(group, properties);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeletePropertyAsync(string group, string name, CancellationToken ct = default)
    {
        DeleteProperty(group, name);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task LockPropertiesAsync(string group, string[] names, CancellationToken ct = default)
    {
        LockProperties(group, names);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UnlockPropertiesAsync(string group, string[] names, CancellationToken ct = default)
    {
        UnlockProperties(group, names);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<string[]> GetLockedPropertiesAsync(string group, CancellationToken ct = default)
        => Task.FromResult(GetLockedProperties(group));
}
