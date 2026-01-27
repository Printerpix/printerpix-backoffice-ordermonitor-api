using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Moq;
using OrderMonitor.Api.Controllers;
using OrderMonitor.Core.Interfaces;

namespace OrderMonitor.IntegrationTests;

/// <summary>
/// Integration tests for AlertsController endpoints.
/// Tests HTTP request/response handling for alert functionality.
/// </summary>
public class AlertsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AlertsControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region POST /api/alerts/test Tests

    [Fact]
    public async Task SendTestAlert_WithValidEmail_ReturnsOk()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        var request = new TestAlertRequest { Email = "test@example.com" };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/alerts/test", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TestAlertResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Message.Should().Contain("test@example.com");
    }

    [Fact]
    public async Task SendTestAlert_CallsAlertService()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        var request = new TestAlertRequest { Email = "verify@example.com" };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");
        using var client = _factory.CreateClient();

        // Act
        await client.PostAsync("/api/alerts/test", content);

        // Assert
        _factory.MockAlertService.Verify(
            s => s.SendTestAlertAsync("verify@example.com", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendTestAlert_WithEmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        var request = new TestAlertRequest { Email = "" };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/alerts/test", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify the service was NOT called (validation should prevent it)
        _factory.MockAlertService.Verify(
            s => s.SendTestAlertAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendTestAlert_WithNullEmail_ReturnsBadRequest()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        var content = new StringContent(
            "{}",
            Encoding.UTF8,
            "application/json");
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/alerts/test", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendTestAlert_WithWhitespaceEmail_ReturnsBadRequest()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        var request = new TestAlertRequest { Email = "   " };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/alerts/test", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendTestAlert_WhenSmtpPasswordNotConfigured_Returns500WithMessage()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        _factory.MockAlertService
            .Setup(s => s.SendTestAlertAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP password not configured"));

        var request = new TestAlertRequest { Email = "test@example.com" };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/alerts/test", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("SMTP password not configured");
    }

    [Fact]
    public async Task SendTestAlert_WhenSmtpFails_Returns500()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        _factory.MockAlertService
            .Setup(s => s.SendTestAlertAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP server unreachable"));

        var request = new TestAlertRequest { Email = "test@example.com" };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/alerts/test", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SendTestAlert_ReturnsTimestamp()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        var request = new TestAlertRequest { Email = "test@example.com" };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");
        var beforeRequest = DateTime.UtcNow;
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/alerts/test", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TestAlertResponse>();
        result.Should().NotBeNull();
        result!.SentAt.Should().BeOnOrAfter(beforeRequest);
        result.SentAt.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task SendTestAlert_ReturnsJsonContentType()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        var request = new TestAlertRequest { Email = "test@example.com" };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/alerts/test", content);

        // Assert
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    #endregion
}
