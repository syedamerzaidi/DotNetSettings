using DotNetSettings.Encryption;
using DotNetSettings.Repositories;
using System.Text.Json;

namespace DotNetSettings.Migrations;

/// <summary>
/// Fluent API for writing settings migrations. Operations use the fully-qualified
/// key format <c>"group.name"</c> (e.g. <c>"general.SiteName"</c>) unless a
/// group scope has been entered via <see cref="InGroup"/>.
/// </summary>
public sealed class SettingsMigrator
{
    private readonly ISettingsRepository _repository;
    private readonly SettingsEncryptor? _encryptor;
    private readonly string? _groupScope;

    internal SettingsMigrator(ISettingsRepository repository, SettingsEncryptor? encryptor, string? groupScope = null)
    {
        _repository = repository;
        _encryptor = encryptor;
        _groupScope = groupScope;
    }

    private (string group, string name) ParseKey(string key)
    {
        if (_groupScope is not null)
            return (_groupScope, key);

        var dot = key.IndexOf('.');
        if (dot < 0) throw new ArgumentException($"Key '{key}' must be in 'group.name' format.", nameof(key));
        return (key[..dot], key[(dot + 1)..]);
    }

    /// <summary>Creates a property with <paramref name="defaultValue"/> if it does not yet exist.</summary>
    public void Add(string key, object? defaultValue)
    {
        var (group, name) = ParseKey(key);
        if (!_repository.PropertyExists(group, name))
            _repository.CreateProperty(group, name, JsonSerializer.Serialize(defaultValue));
    }

    /// <summary>Creates an encrypted property with <paramref name="defaultValue"/> if it does not yet exist.</summary>
    public void AddEncrypted(string key, object? defaultValue)
    {
        var (group, name) = ParseKey(key);
        if (_repository.PropertyExists(group, name)) return;

        var json = JsonSerializer.Serialize(defaultValue);
        var payload = _encryptor is not null ? _encryptor.Encrypt(json) : json;
        _repository.CreateProperty(group, name, payload);
    }

    /// <summary>Moves a property from <paramref name="fromKey"/> to <paramref name="toKey"/>.</summary>
    public void Rename(string fromKey, string toKey)
    {
        var (fromGroup, fromName) = ParseKey(fromKey);
        var (toGroup, toName) = ParseKey(toKey);
        var payload = _repository.GetPropertyPayload(fromGroup, fromName);
        _repository.DeleteProperty(fromGroup, fromName);
        _repository.CreateProperty(toGroup, toName, payload);
    }

    /// <summary>Transforms an existing property's payload using <paramref name="transform"/>.</summary>
    public void Update(string key, Func<string?, string?> transform)
    {
        var (group, name) = ParseKey(key);
        var old = _repository.GetPropertyPayload(group, name);
        _repository.UpdateProperties(group, new Dictionary<string, string?> { [name] = transform(old) });
    }

    /// <summary>Deletes the property entirely.</summary>
    public void Delete(string key)
    {
        var (group, name) = ParseKey(key);
        _repository.DeleteProperty(group, name);
    }

    /// <summary>Returns true when the property already exists.</summary>
    public bool Exists(string key)
    {
        var (group, name) = ParseKey(key);
        return _repository.PropertyExists(group, name);
    }

    /// <summary>Encrypts an existing plaintext property in place.</summary>
    public void Encrypt(string key)
    {
        if (_encryptor is null) throw new InvalidOperationException("Encryption is not configured.");
        var (group, name) = ParseKey(key);
        var payload = _repository.GetPropertyPayload(group, name);
        if (payload is not null)
            _repository.UpdateProperties(group, new Dictionary<string, string?> { [name] = _encryptor.Encrypt(payload) });
    }

    /// <summary>Decrypts an existing encrypted property in place.</summary>
    public void Decrypt(string key)
    {
        if (_encryptor is null) throw new InvalidOperationException("Encryption is not configured.");
        var (group, name) = ParseKey(key);
        var payload = _repository.GetPropertyPayload(group, name);
        if (payload is not null)
            _repository.UpdateProperties(group, new Dictionary<string, string?> { [name] = _encryptor.Decrypt(payload) });
    }

    /// <summary>
    /// Scopes all operations inside <paramref name="configure"/> to <paramref name="group"/>,
    /// so keys can be provided as plain property names without a group prefix.
    /// </summary>
    public void InGroup(string group, Action<SettingsMigrator> configure)
        => configure(new SettingsMigrator(_repository, _encryptor, group));
}
