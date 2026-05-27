namespace DotNetSettings.Models;

/// <summary>Entity that stores a single named setting property for a group.</summary>
public class SettingRecord
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Logical group this setting belongs to (e.g. "general", "mail").</summary>
    public string Group { get; set; } = "";

    /// <summary>Property name within the group.</summary>
    public string Name { get; set; } = "";

    /// <summary>JSON-encoded value, optionally encrypted.</summary>
    public string? Payload { get; set; }

    /// <summary>When true the property value cannot be overwritten on save.</summary>
    public bool Locked { get; set; }

    /// <summary>When true the payload is encrypted via Data Protection.</summary>
    public bool Encrypted { get; set; }
}
