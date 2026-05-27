namespace DotNetSettings.Events;

/// <summary>Published immediately before a settings group is persisted to the repository.</summary>
public sealed class SavingSettingsEvent : ISettingsEvent
{
    /// <summary>The settings group being saved.</summary>
    public string Group { get; }

    /// <summary>Initialises the event.</summary>
    public SavingSettingsEvent(string group) => Group = group;
}
