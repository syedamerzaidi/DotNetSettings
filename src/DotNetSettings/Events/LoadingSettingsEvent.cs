namespace DotNetSettings.Events;

/// <summary>Published immediately before a settings group is loaded from the repository.</summary>
public sealed class LoadingSettingsEvent : ISettingsEvent
{
    /// <summary>The settings group being loaded.</summary>
    public string Group { get; }

    /// <summary>Initialises the event.</summary>
    public LoadingSettingsEvent(string group) => Group = group;
}
