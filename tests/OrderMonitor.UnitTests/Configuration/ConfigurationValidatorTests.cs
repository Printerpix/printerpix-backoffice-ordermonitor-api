using Microsoft.Extensions.Configuration;
using OrderMonitor.Infrastructure.Configuration;
using Xunit;

namespace OrderMonitor.UnitTests.Configuration;

public class ConfigurationValidatorTests
{
    private ConfigurationValidator CreateValidator(Dictionary<string, string?> configValues)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
        return new ConfigurationValidator(configuration);
    }

    [Fact]
    public void Validate_AllRequiredValues_DoesNotThrow()
    {
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "sqlserver",
            ["Database:ConnectionString"] = "Server=localhost;Database=TestDb;",
            ["SmtpSettings:Host"] = "smtp.example.com",
            ["Alerts:Enabled"] = "false"
        });

        var exception = Record.Exception(() => validator.Validate());

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_MissingProvider_ThrowsWithMessage()
    {
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = "Server=localhost;",
            ["SmtpSettings:Host"] = "smtp.example.com"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate());
        Assert.Contains("Database:Provider is required", ex.Message);
    }

    [Fact]
    public void Validate_InvalidProvider_ThrowsWithMessage()
    {
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "oracle",
            ["Database:ConnectionString"] = "Server=localhost;",
            ["SmtpSettings:Host"] = "smtp.example.com"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate());
        Assert.Contains("'oracle' is invalid", ex.Message);
    }

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("mysql")]
    [InlineData("postgresql")]
    [InlineData("SqlServer")]
    [InlineData("POSTGRESQL")]
    public void Validate_ValidProviders_DoNotThrow(string provider)
    {
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            ["Database:Provider"] = provider,
            ["Database:ConnectionString"] = "Server=localhost;",
            ["SmtpSettings:Host"] = "smtp.example.com"
        });

        var exception = Record.Exception(() => validator.Validate());

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_MissingConnectionString_ThrowsWithMessage()
    {
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "sqlserver",
            ["SmtpSettings:Host"] = "smtp.example.com"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate());
        Assert.Contains("Database:ConnectionString", ex.Message);
    }

    [Fact]
    public void Validate_LegacyConnectionString_DoesNotThrow()
    {
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "sqlserver",
            ["ConnectionStrings:BackofficeDb"] = "Server=localhost;Database=BackofficeDb;",
            ["SmtpSettings:Host"] = "smtp.example.com"
        });

        var exception = Record.Exception(() => validator.Validate());

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_MissingSmtpHost_ThrowsWithMessage()
    {
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "sqlserver",
            ["Database:ConnectionString"] = "Server=localhost;"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate());
        Assert.Contains("SmtpSettings:Host is required", ex.Message);
    }

    [Fact]
    public void Validate_AlertsEnabledWithoutRecipients_ThrowsWithMessage()
    {
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "sqlserver",
            ["Database:ConnectionString"] = "Server=localhost;",
            ["SmtpSettings:Host"] = "smtp.example.com",
            ["Alerts:Enabled"] = "true"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate());
        Assert.Contains("Alerts:Recipients is required when Alerts:Enabled is true", ex.Message);
    }

    [Fact]
    public void Validate_AlertsEnabledWithRecipients_DoesNotThrow()
    {
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "sqlserver",
            ["Database:ConnectionString"] = "Server=localhost;",
            ["SmtpSettings:Host"] = "smtp.example.com",
            ["Alerts:Enabled"] = "true",
            ["Alerts:Recipients"] = "admin@example.com"
        });

        var exception = Record.Exception(() => validator.Validate());

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_AlertsDisabled_RecipientsNotRequired()
    {
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "sqlserver",
            ["Database:ConnectionString"] = "Server=localhost;",
            ["SmtpSettings:Host"] = "smtp.example.com",
            ["Alerts:Enabled"] = "false"
        });

        var exception = Record.Exception(() => validator.Validate());

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_MultipleErrors_AllReported()
    {
        var validator = CreateValidator(new Dictionary<string, string?>());

        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate());
        Assert.Contains("Database:Provider is required", ex.Message);
        Assert.Contains("Database:ConnectionString", ex.Message);
        Assert.Contains("SmtpSettings:Host is required", ex.Message);
    }
}
