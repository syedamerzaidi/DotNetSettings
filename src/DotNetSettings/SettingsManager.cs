using DotNetSettings.Attributes;
using DotNetSettings.Casts;
using DotNetSettings.Encryption;
using DotNetSettings.Events;
using DotNetSettings.Repositories;
using System.Reflection;
using System.Text.Json;

namespace DotNetSettings;

/// <summary>
/// Orchestrates loading, saving, locking, and unlocking of settings.
/// Registered as a singleton service by <see cref="SettingsServiceCollectionExtensions"/>.
/// </summary>
public sealed class SettingsManager
{
    private readonly ISettingsRepository _defaultRepository;
    private readonly Dictionary<string, ISettingsRepository> _namedRepositories;
    private readonly Dictionary<Type, ISettingsCast> _globalCasts;
    private readonly SettingsEncryptor? _encryptor;
    private readonly ISettingsEventPublisher _eventPublisher;

    /// <summary>Initialises the manager with all its dependencies.</summary>
    public SettingsManager(
        ISettingsRepository defaultRepository,
        Dictionary<string, ISettingsRepository>? namedRepositories = null,
        Dictionary<Type, ISettingsCast>? globalCasts = null,
        SettingsEncryptor? encryptor = null,
        ISettingsEventPublisher? eventPublisher = null)
    {
        _defaultRepository = defaultRepository;
        _namedRepositories = namedRepositories ?? new();
        _globalCasts = globalCasts ?? new();
        _encryptor = encryptor;
        _eventPublisher = eventPublisher ?? new NoOpSettingsEventPublisher();
    }

    /// <summary>Loads all stored properties into <paramref name="settings"/> via reflection.</summary>
    public void Load(Settings settings)
    {
        _eventPublisher.PublishAsync(new LoadingSettingsEvent(settings.Group)).GetAwaiter().GetResult();

        var repo = ResolveRepository(settings);
        var stored = repo.GetPropertiesInGroup(settings.Group);
        var localCasts = settings.GetCasts();
        var encryptedProps = new HashSet<string>(settings.GetEncryptedProperties(), StringComparer.OrdinalIgnoreCase);

        foreach (var prop in GetSettableProperties(settings))
        {
            if (!stored.TryGetValue(prop.Name, out var payload)) continue;

            var shouldDecrypt = ShouldEncrypt(prop, encryptedProps);
            if (shouldDecrypt && payload is not null && _encryptor is not null)
                payload = _encryptor.Decrypt(payload);

            var cast = ResolveCast(prop, localCasts);
            object? value = cast is not null
                ? cast.Get(payload, prop.PropertyType)
                : DeserializePayload(payload, prop.PropertyType);

            prop.SetValue(settings, value);
        }

        _eventPublisher.PublishAsync(new SettingsLoadedEvent(settings.Group)).GetAwaiter().GetResult();
    }

    /// <summary>Persists all non-locked properties from <paramref name="settings"/> to the repository.</summary>
    public void Save(Settings settings)
    {
        _eventPublisher.PublishAsync(new SavingSettingsEvent(settings.Group)).GetAwaiter().GetResult();

        var repo = ResolveRepository(settings);
        var locked = new HashSet<string>(repo.GetLockedProperties(settings.Group), StringComparer.OrdinalIgnoreCase);
        var oldValues = locked.Count > 0 ? repo.GetPropertiesInGroup(settings.Group) : new Dictionary<string, string?>();
        var localCasts = settings.GetCasts();
        var encryptedProps = new HashSet<string>(settings.GetEncryptedProperties(), StringComparer.OrdinalIgnoreCase);
        var toSave = new Dictionary<string, string?>();

        foreach (var prop in GetSettableProperties(settings))
        {
            if (locked.Contains(prop.Name))
            {
                if (oldValues.TryGetValue(prop.Name, out var old))
                    toSave[prop.Name] = old;
                continue;
            }

            var value = prop.GetValue(settings);
            var cast = ResolveCast(prop, localCasts);
            string? payload = cast is not null ? cast.Set(value) : JsonSerializer.Serialize(value);

            if (ShouldEncrypt(prop, encryptedProps) && payload is not null && _encryptor is not null)
                payload = _encryptor.Encrypt(payload);

            toSave[prop.Name] = payload;
        }

        repo.UpdateProperties(settings.Group, toSave);

        _eventPublisher.PublishAsync(new SettingsSavedEvent(settings.Group)).GetAwaiter().GetResult();
    }

    /// <summary>Locks the specified properties so they cannot be overwritten on save.</summary>
    public void Lock(Settings settings, string[] properties)
        => ResolveRepository(settings).LockProperties(settings.Group, properties);

    /// <summary>Removes the lock from the specified properties.</summary>
    public void Unlock(Settings settings, string[] properties)
        => ResolveRepository(settings).UnlockProperties(settings.Group, properties);

    /// <summary>Returns true when <paramref name="property"/> is currently locked.</summary>
    public bool IsLocked(Settings settings, string property)
        => ResolveRepository(settings).GetLockedProperties(settings.Group)
            .Contains(property, StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns all currently locked property names for this settings group.</summary>
    public string[] GetLockedProperties(Settings settings)
        => ResolveRepository(settings).GetLockedProperties(settings.Group);

    /// <summary>
    /// Creates a <typeparamref name="T"/> instance populated from <paramref name="overrides"/>
    /// without hitting any real repository.
    /// </summary>
    public static T CreateFake<T>(Dictionary<string, object?> overrides) where T : Settings
    {
        var instance = Activator.CreateInstance<T>();
        var fakeRepo = new FakeSettingsRepository();

        foreach (var (key, value) in overrides)
            fakeRepo.SetPropertyInternal(instance.Group, key, JsonSerializer.Serialize(value));

        var manager = new SettingsManager(fakeRepo);
        instance.Repository = fakeRepo;
        instance.Manager = manager;
        manager.Load(instance);
        return instance;
    }

    private ISettingsRepository ResolveRepository(Settings settings)
    {
        if (settings.RepositoryName is { } name && _namedRepositories.TryGetValue(name, out var repo))
            return repo;
        return _defaultRepository;
    }

    private static IEnumerable<PropertyInfo> GetSettableProperties(Settings settings)
        => settings.GetType()
                   .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                   .Where(p => p.CanRead && p.CanWrite
                               && p.Name != nameof(Settings.Group)
                               && p.Name != nameof(Settings.RepositoryName));

    private ISettingsCast? ResolveCast(PropertyInfo prop, Dictionary<string, ISettingsCast> localCasts)
    {
        if (localCasts.TryGetValue(prop.Name, out var local)) return local;
        if (_globalCasts.TryGetValue(prop.PropertyType, out var global)) return global;
        return null;
    }

    private static bool ShouldEncrypt(PropertyInfo prop, HashSet<string> encryptedProps)
        => prop.GetCustomAttribute<EncryptAttribute>() is not null
           || encryptedProps.Contains(prop.Name);

    private static object? DeserializePayload(string? payload, Type targetType)
    {
        if (payload is null) return null;
        try { return JsonSerializer.Deserialize(payload, targetType); }
        catch { return null; }
    }
}
