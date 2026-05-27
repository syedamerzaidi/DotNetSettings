using DotNetSettings.Casts;
using DotNetSettings.Repositories;

namespace DotNetSettings;

/// <summary>
/// Abstract base class for all settings groups. Inherit from this class,
/// declare your settings as public properties, and register the class via
/// <c>services.AddSettings&lt;TSettings&gt;()</c>.
/// </summary>
public abstract class Settings
{
    /// <summary>Unique identifier for this settings group (e.g. <c>"general"</c>, <c>"mail"</c>).</summary>
    public abstract string Group { get; }

    /// <summary>
    /// Optional name of a named repository to use for this settings class.
    /// When <c>null</c> (the default) the default repository is used.
    /// </summary>
    public virtual string? RepositoryName => null;

    /// <summary>
    /// Returns property names that should be encrypted at rest.
    /// The <c>[Encrypt]</c> attribute is an alternative per-property option.
    /// </summary>
    public virtual string[] GetEncryptedProperties() => Array.Empty<string>();

    /// <summary>
    /// Returns per-property cast overrides keyed by property name.
    /// Local casts take precedence over globally registered casts.
    /// </summary>
    public virtual Dictionary<string, ISettingsCast> GetCasts() => new();

    internal ISettingsRepository? Repository { get; set; }
    internal SettingsManager? Manager { get; set; }

    /// <summary>Persists the current property values to the repository.</summary>
    public void Save() => Manager!.Save(this);

    /// <summary>Reloads all properties from the repository, discarding in-memory changes.</summary>
    public void Refresh() => Manager!.Load(this);

    /// <summary>Locks the specified <paramref name="properties"/> so they cannot be overwritten on save.</summary>
    public void Lock(params string[] properties) => Manager!.Lock(this, properties);

    /// <summary>Removes the lock from the specified <paramref name="properties"/>.</summary>
    public void Unlock(params string[] properties) => Manager!.Unlock(this, properties);

    /// <summary>Returns <c>true</c> when <paramref name="property"/> is currently locked.</summary>
    public bool IsLocked(string property) => Manager!.IsLocked(this, property);

    /// <summary>Returns all currently locked property names for this settings group.</summary>
    public string[] GetLockedProperties() => Manager!.GetLockedProperties(this);

    /// <summary>
    /// Creates a <typeparamref name="T"/> instance populated from <paramref name="overrides"/>
    /// without touching any real repository. Useful in unit tests.
    /// </summary>
    public static T Fake<T>(Dictionary<string, object?> overrides) where T : Settings
        => SettingsManager.CreateFake<T>(overrides);
}
