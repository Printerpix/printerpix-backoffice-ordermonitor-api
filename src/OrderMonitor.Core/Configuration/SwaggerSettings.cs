namespace OrderMonitor.Core.Configuration;

/// <summary>
/// Swagger/OpenAPI documentation configuration settings.
/// </summary>
public class SwaggerSettings
{
    public const string SectionName = "Swagger";

    /// <summary>
    /// Whether Swagger UI is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// API title shown in Swagger UI.
    /// </summary>
    public string Title { get; set; } = "OrderMonitor API";

    /// <summary>
    /// API version shown in Swagger UI.
    /// </summary>
    public string Version { get; set; } = "v1";
}
