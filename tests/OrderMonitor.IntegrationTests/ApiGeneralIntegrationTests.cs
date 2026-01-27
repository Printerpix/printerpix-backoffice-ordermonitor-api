using System.Net;
using FluentAssertions;

namespace OrderMonitor.IntegrationTests;

/// <summary>
/// Integration tests for general API behavior.
/// Tests routing, error handling, and infrastructure endpoints.
/// </summary>
public class ApiGeneralIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ApiGeneralIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region Routing Tests

    [Fact]
    public async Task NonExistentEndpoint_Returns404()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WrongHttpMethod_Returns405()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act - GET is not supported for /api/alerts/test
        var response = await client.GetAsync("/api/alerts/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Theory]
    [InlineData("/api/orders/stuck")]
    [InlineData("/api/orders/stuck/summary")]
    [InlineData("/api/orders/CO12345/status-history")]
    public async Task GetEndpoints_AcceptGetMethod(string endpoint)
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
    }

    #endregion

    #region Content Negotiation Tests

    [Fact]
    public async Task ApiEndpoints_ReturnJsonByDefault()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/stuck");

        // Assert
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task ApiEndpoints_ReturnUtf8Encoding()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/stuck");

        // Assert
        response.Content.Headers.ContentType!.CharSet.Should().Be("utf-8");
    }

    #endregion

    #region Base URL Tests

    [Fact]
    public async Task RootUrl_IsAccessible()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert - Root URL might return 404 or redirect, but should not error
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    #endregion
}
