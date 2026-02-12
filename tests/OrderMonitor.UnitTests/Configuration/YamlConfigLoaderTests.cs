using OrderMonitor.Infrastructure.Configuration;
using Xunit;

namespace OrderMonitor.UnitTests.Configuration;

public class YamlConfigLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly YamlConfigLoader _loader;

    public YamlConfigLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"yaml_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _loader = new YamlConfigLoader();
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
    public void Load_ValidYaml_ReturnsFlatDictionary()
    {
        var path = CreateTempYaml("Key1: Value1\nKey2: Value2");

        var result = _loader.Load(path);

        Assert.Equal("Value1", result["Key1"]);
        Assert.Equal("Value2", result["Key2"]);
    }

    [Fact]
    public void Load_DoubleUnderscore_MapsToColon()
    {
        var path = CreateTempYaml("Database__ConnectionString: \"Server=localhost\"");

        var result = _loader.Load(path);

        Assert.True(result.ContainsKey("Database:ConnectionString"));
        Assert.Equal("Server=localhost", result["Database:ConnectionString"]);
    }

    [Fact]
    public void Load_MultipleNestedKeys_AllMapped()
    {
        var yaml = @"Database__Provider: sqlserver
Database__ConnectionString: ""Server=localhost""
SmtpSettings__Host: smtp.example.com
SmtpSettings__Port: ""587""
Alerts__Enabled: ""true""
Alerts__Recipients: admin@example.com";

        var path = CreateTempYaml(yaml);
        var result = _loader.Load(path);

        Assert.Equal("sqlserver", result["Database:Provider"]);
        Assert.Equal("Server=localhost", result["Database:ConnectionString"]);
        Assert.Equal("smtp.example.com", result["SmtpSettings:Host"]);
        Assert.Equal("587", result["SmtpSettings:Port"]);
        Assert.Equal("true", result["Alerts:Enabled"]);
        Assert.Equal("admin@example.com", result["Alerts:Recipients"]);
    }

    [Fact]
    public void Load_FileNotFound_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(_tempDir, "nonexistent.yml");

        Assert.Throws<FileNotFoundException>(() => _loader.Load(nonExistentPath));
    }

    [Fact]
    public void Load_EmptyFile_ReturnsEmptyDictionary()
    {
        var path = CreateTempYaml("");

        var result = _loader.Load(path);

        Assert.Empty(result);
    }

    [Fact]
    public void Load_WhitespaceOnlyFile_ReturnsEmptyDictionary()
    {
        var path = CreateTempYaml("   \n  \n  ");

        var result = _loader.Load(path);

        Assert.Empty(result);
    }

    [Fact]
    public void Load_InvalidYaml_ThrowsFormatException()
    {
        var path = CreateTempYaml("{ invalid yaml [[[");

        Assert.Throws<FormatException>(() => _loader.Load(path));
    }

    [Fact]
    public void Load_KeysAreCaseInsensitive()
    {
        var path = CreateTempYaml("Database__Provider: sqlserver");

        var result = _loader.Load(path);

        Assert.True(result.ContainsKey("database:provider"));
        Assert.True(result.ContainsKey("DATABASE:PROVIDER"));
    }

    [Fact]
    public void Load_NullValues_ConvertedToEmptyString()
    {
        var path = CreateTempYaml("EmptyKey: ");

        var result = _loader.Load(path);

        Assert.True(result.ContainsKey("EmptyKey"));
        Assert.Equal("", result["EmptyKey"]);
    }

    [Fact]
    public void Load_CommentsIgnored()
    {
        var yaml = "# This is a comment\nKey1: Value1\n# Another comment\nKey2: Value2";
        var path = CreateTempYaml(yaml);

        var result = _loader.Load(path);

        Assert.Equal(2, result.Count);
        Assert.Equal("Value1", result["Key1"]);
        Assert.Equal("Value2", result["Key2"]);
    }
}
