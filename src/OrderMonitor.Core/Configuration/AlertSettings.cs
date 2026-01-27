namespace OrderMonitor.Core.Configuration;

/// <summary>
/// Alert notification configuration settings.
/// </summary>
public class AlertSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Alerts";

    /// <summary>
    /// Whether alerts are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// List of email recipients for alerts.
    /// </summary>
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// Subject line prefix for alert emails.
    /// </summary>
    public string SubjectPrefix { get; set; } = "[Order Monitor]";
}
