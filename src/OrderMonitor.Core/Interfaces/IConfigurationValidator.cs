namespace OrderMonitor.Core.Interfaces;

/// <summary>
/// Validates that all required configuration values are present and valid at startup.
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// Validates all required configuration values.
    /// Throws <see cref="InvalidOperationException"/> with a clear message if validation fails.
    /// </summary>
    void Validate();
}
