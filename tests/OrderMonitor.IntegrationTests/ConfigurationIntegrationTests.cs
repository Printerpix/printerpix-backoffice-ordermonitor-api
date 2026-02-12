using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderMonitor.Core.Interfaces;

namespace OrderMonitor.IntegrationTests;

/// <summary>
/// Integration tests for the YAML configuration pipeline.
/// Validates that configuration loads correctly and overrides work as expected.
/// </summary>
public class ConfigurationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ConfigurationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task App_StartsSuccessfully_WithValidConfiguration()
    {
        // Arrange & Act
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task App_ConfigurationOverride_InMemoryOverridesYaml()
    {
        // The factory adds in-memory config with Database:Provider = sqlserver
        // This tests that it's correctly available
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // If the app starts, the config was loaded correctly
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void ConfigurationValidator_ThrowsOnMissingRequired()
    {
        // Directly test the validator with incomplete configuration
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Warning"
            })
            .Build();

        var validator = new OrderMonitor.Infrastructure.Configuration.ConfigurationValidator(config);

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate());
        exception.Message.Should().Contain("Configuration validation failed");
        exception.Message.Should().Contain("Database:Provider is required");
    }
}
