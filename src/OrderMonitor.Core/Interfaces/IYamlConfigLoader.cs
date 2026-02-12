namespace OrderMonitor.Core.Interfaces;

/// <summary>
/// Loads configuration from a YAML file and returns a flat key-value map.
/// </summary>
public interface IYamlConfigLoader
{
    /// <summary>
    /// Loads configuration values from a YAML file.
    /// Keys use double-underscore (__) as hierarchy separator, which maps to ASP.NET Core's colon (:) separator.
    /// </summary>
    /// <param name="filePath">Path to the YAML file.</param>
    /// <returns>Flat dictionary of configuration key-value pairs.</returns>
    IDictionary<string, string> Load(string filePath);
}
