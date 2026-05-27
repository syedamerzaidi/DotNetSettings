using DotNetSettings;

namespace DotNetSettings.Sample.Settings;

/// <summary>Email / SMTP settings.</summary>
public class MailSettings : DotNetSettings.Settings
{
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "";
    public bool SendWelcomeEmail { get; set; }

    public override string Group => "mail";
}
