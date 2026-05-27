namespace DotNetSettings.Events;

/// <summary>Published after a settings group has been persisted to the repository.</summary>
public sealed class SettingsSavedEvent : ISettingsEvent
{
    /// <summary>The settings group that was saved.</summary>
    public string Group { get; }

    /// <summary>Initialises the event.</summary>
    public SettingsSavedEvent(string group) => Group = group;
}
