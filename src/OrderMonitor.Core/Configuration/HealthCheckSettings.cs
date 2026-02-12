namespace OrderMonitor.Core.Configuration;

/// <summary>
/// Health check endpoint configuration settings.
/// </summary>
public class HealthCheckSettings
{
    public const string SectionName = "HealthCheck";

    /// <summary>
    /// Health check endpoint path.
    /// </summary>
    public string Path { get; set; } = "/health";

    /// <summary>
    /// Whether to include database connectivity in health checks.
    /// </summary>
    public bool IncludeDatabase { get; set; } = true;
}
