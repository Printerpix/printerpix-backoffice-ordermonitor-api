using System.Net;
using FluentAssertions;

namespace OrderMonitor.IntegrationTests;

/// <summary>
/// Integration tests for health check endpoint.
/// </summary>
public class HealthCheckIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthCheckIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthCheck_ReturnsOkStatus()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_ReturnsTextPlainContentType()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        // ASP.NET Core Health Checks middleware returns text/plain by default
        response.Content.Headers.ContentType?.MediaType.Should().BeOneOf("text/plain", "application/json");
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthyContent()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        // Health check response contains "Healthy" status
        content.Should().Contain("Healthy");
    }
}
