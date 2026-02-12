using Microsoft.Extensions.Configuration;

namespace OrderMonitor.Infrastructure.Configuration;

/// <summary>
/// ASP.NET Core configuration source that loads values from a YAML file.
/// </summary>
public class YamlConfigurationSource : IConfigurationSource
{
    public string FilePath { get; }
    public bool Optional { get; }

    public YamlConfigurationSource(string filePath, bool optional = false)
    {
        FilePath = filePath;
        Optional = optional;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new YamlConfigurationProvider(this);
    }
}

/// <summary>
/// Configuration provider that reads values from a YAML file.
/// </summary>
public class YamlConfigurationProvider : ConfigurationProvider
{
    private readonly YamlConfigurationSource _source;

    public YamlConfigurationProvider(YamlConfigurationSource source)
    {
        _source = source;
    }

    public override void Load()
    {
        if (!File.Exists(_source.FilePath))
        {
            if (_source.Optional)
            {
                Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                return;
            }
            throw new FileNotFoundException($"Configuration file not found: {_source.FilePath}", _source.FilePath);
        }

        var loader = new YamlConfigLoader();
        var values = loader.Load(_source.FilePath);

        Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in values)
        {
            Data[kvp.Key] = kvp.Value;
        }
    }
}

/// <summary>
/// Extension methods for adding YAML configuration to the configuration builder.
/// </summary>
public static class YamlConfigurationExtensions
{
    /// <summary>
    /// Adds a YAML file as a configuration source.
    /// </summary>
    public static IConfigurationBuilder AddYamlFile(
        this IConfigurationBuilder builder,
        string path,
        bool optional = false)
    {
        return builder.Add(new YamlConfigurationSource(path, optional));
    }
}
