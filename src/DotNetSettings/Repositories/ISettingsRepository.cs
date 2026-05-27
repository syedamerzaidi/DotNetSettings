namespace DotNetSettings.Repositories;

/// <summary>
/// Abstraction over the backing store for settings. Implement this interface
/// to add a custom backend (e.g. Consul, Azure App Configuration).
/// </summary>
public interface ISettingsRepository
{
    // ── Synchronous API ───────────────────────────────────────────────────────

    /// <summary>Returns all (name → payload) pairs stored for <paramref name="group"/>.</summary>
    Dictionary<string, string?> GetPropertiesInGroup(string group);

    /// <summary>Returns true when a property with <paramref name="name"/> exists in <paramref name="group"/>.</summary>
    bool PropertyExists(string group, string name);

    /// <summary>Returns the raw JSON payload for the property, or <c>null</c> if not found.</summary>
    string? GetPropertyPayload(string group, string name);

    /// <summary>Inserts a new property record.</summary>
    void CreateProperty(string group, string name, string? payload);

    /// <summary>Upserts all provided (name → payload) pairs for <paramref name="group"/>.</summary>
    void UpdateProperties(string group, Dictionary<string, string?> properties);

    /// <summary>Permanently removes the named property.</summary>
    void DeleteProperty(string group, string name);

    /// <summary>Marks the specified properties as locked so they cannot be overwritten on save.</summary>
    void LockProperties(string group, string[] names);

    /// <summary>Removes the lock from the specified properties.</summary>
    void UnlockProperties(string group, string[] names);

    /// <summary>Returns all currently locked property names in <paramref name="group"/>.</summary>
    string[] GetLockedProperties(string group);

    // ── Asynchronous API ─────────────────────────────────────────────────────

    /// <inheritdoc cref="GetPropertiesInGroup"/>
    Task<Dictionary<string, string?>> GetPropertiesInGroupAsync(string group, CancellationToken ct = default);

    /// <inheritdoc cref="PropertyExists"/>
    Task<bool> PropertyExistsAsync(string group, string name, CancellationToken ct = default);

    /// <inheritdoc cref="GetPropertyPayload"/>
    Task<string?> GetPropertyPayloadAsync(string group, string name, CancellationToken ct = default);

    /// <inheritdoc cref="CreateProperty"/>
    Task CreatePropertyAsync(string group, string name, string? payload, CancellationToken ct = default);

    /// <inheritdoc cref="UpdateProperties"/>
    Task UpdatePropertiesAsync(string group, Dictionary<string, string?> properties, CancellationToken ct = default);

    /// <inheritdoc cref="DeleteProperty"/>
    Task DeletePropertyAsync(string group, string name, CancellationToken ct = default);

    /// <inheritdoc cref="LockProperties"/>
    Task LockPropertiesAsync(string group, string[] names, CancellationToken ct = default);

    /// <inheritdoc cref="UnlockProperties"/>
    Task UnlockPropertiesAsync(string group, string[] names, CancellationToken ct = default);

    /// <inheritdoc cref="GetLockedProperties"/>
    Task<string[]> GetLockedPropertiesAsync(string group, CancellationToken ct = default);
}
