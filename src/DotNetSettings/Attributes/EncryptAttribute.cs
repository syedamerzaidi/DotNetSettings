namespace DotNetSettings.Attributes;

/// <summary>
/// Marks a settings property for at-rest encryption via ASP.NET Core Data Protection.
/// The raw value is encrypted before writing to the repository and decrypted on load.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class EncryptAttribute : Attribute { }
