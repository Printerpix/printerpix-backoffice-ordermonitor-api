using Microsoft.Extensions.Configuration;
using OrderMonitor.Infrastructure.Configuration;
using Xunit;

namespace OrderMonitor.UnitTests.Configuration;

public class YamlConfigurationSourceTests : IDisposable
{
    private readonly string _tempDir;

    public YamlConfigurationSourceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"yaml_cfg_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateTempYaml(string content)
    {
        var path = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.yml");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void AddYamlFile_LoadsIntoConfiguration()
    {
        var path = CreateTempYaml("Database__Provider: sqlserver\nDatabase__ConnectionString: \"Server=test\"");

        var config = new ConfigurationBuilder()
            .AddYamlFile(path)
            .Build();

        Assert.Equal("sqlserver", config["Database:Provider"]);
        Assert.Equal("Server=test", config["Database:ConnectionString"]);
    }

    [Fact]
    public void AddYamlFile_OptionalMissing_DoesNotThrow()
    {
        var missingPath = Path.Combine(_tempDir, "nonexistent.yml");

        var config = new ConfigurationBuilder()
            .AddYamlFile(missingPath, optional: true)
            .Build();

        Assert.Null(config["AnyKey"]);
    }

    [Fact]
    public void AddYamlFile_RequiredMissing_ThrowsFileNotFound()
    {
        var missingPath = Path.Combine(_tempDir, "nonexistent.yml");

        var builder = new ConfigurationBuilder()
            .AddYamlFile(missingPath, optional: false);

        Assert.Throws<FileNotFoundException>(() => builder.Build());
    }

    [Fact]
    public void AddYamlFile_OverridesPreviousValues()
    {
        var base_yaml = CreateTempYaml("Database__Provider: sqlserver");
        var override_yaml = CreateTempYaml("Database__Provider: postgresql");

        var config = new ConfigurationBuilder()
            .AddYamlFile(base_yaml)
            .AddYamlFile(override_yaml)
            .Build();

        Assert.Equal("postgresql", config["Database:Provider"]);
    }

    [Fact]
    public void AddYamlFile_EnvironmentVariablesOverrideYaml()
    {
        var path = CreateTempYaml("Database__Provider: sqlserver");

        var config = new ConfigurationBuilder()
            .AddYamlFile(path)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "postgresql"
            })
            .Build();

        Assert.Equal("postgresql", config["Database:Provider"]);
    }

    [Fact]
    public void YamlConfigurationSource_Build_ReturnsProvider()
    {
        var source = new YamlConfigurationSource("test.yml", optional: true);
        var builder = new ConfigurationBuilder();

        var provider = source.Build(builder);

        Assert.NotNull(provider);
        Assert.IsType<YamlConfigurationProvider>(provider);
    }

    [Fact]
    public void YamlConfigurationSource_StoresProperties()
    {
        var source = new YamlConfigurationSource("/path/to/file.yml", optional: true);

        Assert.Equal("/path/to/file.yml", source.FilePath);
        Assert.True(source.Optional);
    }
}
