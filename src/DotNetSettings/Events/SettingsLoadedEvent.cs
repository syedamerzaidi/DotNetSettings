namespace DotNetSettings.Events;

/// <summary>Published after a settings group has been fully loaded into its typed class.</summary>
public sealed class SettingsLoadedEvent : ISettingsEvent
{
    /// <summary>The settings group that was loaded.</summary>
    public string Group { get; }

    /// <summary>Initialises the event.</summary>
    public SettingsLoadedEvent(string group) => Group = group;
}
