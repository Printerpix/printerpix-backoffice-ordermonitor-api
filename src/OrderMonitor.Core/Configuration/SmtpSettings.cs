namespace OrderMonitor.Core.Configuration;

/// <summary>
/// SMTP server configuration settings.
/// </summary>
public class SmtpSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "SmtpSettings";

    /// <summary>
    /// SMTP server host.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port.
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// SMTP username for authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password (from environment variable SMTP_PASSWORD).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Email address to send from.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use SSL/TLS.
    /// </summary>
    public bool UseSsl { get; set; } = true;
}
