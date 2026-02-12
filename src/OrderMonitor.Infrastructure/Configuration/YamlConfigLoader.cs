using OrderMonitor.Core.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OrderMonitor.Infrastructure.Configuration;

/// <summary>
/// Loads configuration from a YAML file and returns a flat key-value dictionary.
/// Double-underscore (__) in YAML keys maps to colon (:) for ASP.NET Core configuration.
/// </summary>
public class YamlConfigLoader : IYamlConfigLoader
{
    public IDictionary<string, string> Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}", filePath);

        var content = File.ReadAllText(filePath);

        if (string.IsNullOrWhiteSpace(content))
            return new Dictionary<string, string>();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();

        Dictionary<string, string>? yamlData;
        try
        {
            yamlData = deserializer.Deserialize<Dictionary<string, string>>(content);
        }
        catch (Exception ex)
        {
            throw new FormatException($"Invalid YAML format in {filePath}: {ex.Message}", ex);
        }

        if (yamlData == null)
            return new Dictionary<string, string>();

        // Map double-underscore to colon for ASP.NET Core config hierarchy
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in yamlData)
        {
            var configKey = kvp.Key.Replace("__", ":");
            result[configKey] = kvp.Value ?? string.Empty;
        }

        return result;
    }
}
