using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrderMonitor.Api.Controllers;
using OrderMonitor.Core.Interfaces;

namespace OrderMonitor.UnitTests.Controllers;

public class AlertsControllerTests
{
    private readonly Mock<IAlertService> _alertServiceMock;
    private readonly Mock<ILogger<AlertsController>> _loggerMock;
    private readonly AlertsController _controller;

    public AlertsControllerTests()
    {
        _alertServiceMock = new Mock<IAlertService>();
        _loggerMock = new Mock<ILogger<AlertsController>>();
        _controller = new AlertsController(_alertServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SendTestAlert_WithValidEmail_ReturnsOkWithSuccess()
    {
        // Arrange
        var request = new TestAlertRequest { Email = "test@example.com" };

        _alertServiceMock
            .Setup(s => s.SendTestAlertAsync(request.Email, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SendTestAlert(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TestAlertResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Contain("test@example.com");
        response.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _alertServiceMock.Verify(
            s => s.SendTestAlertAsync("test@example.com", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendTestAlert_WithNullEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new TestAlertRequest { Email = null! };

        // Act
        var result = await _controller.SendTestAlert(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();

        _alertServiceMock.Verify(
            s => s.SendTestAlertAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendTestAlert_WithEmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new TestAlertRequest { Email = "   " };

        // Act
        var result = await _controller.SendTestAlert(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SendTestAlert_WithNullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SendTestAlert(null!);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SendTestAlert_WhenSmtpPasswordNotConfigured_Returns500WithMessage()
    {
        // Arrange
        var request = new TestAlertRequest { Email = "test@example.com" };

        _alertServiceMock
            .Setup(s => s.SendTestAlertAsync(request.Email, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP password not configured"));

        // Act
        var result = await _controller.SendTestAlert(request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task SendTestAlert_WhenSmtpFails_Returns500WithError()
    {
        // Arrange
        var request = new TestAlertRequest { Email = "test@example.com" };

        _alertServiceMock
            .Setup(s => s.SendTestAlertAsync(request.Email, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP connection failed"));

        // Act
        var result = await _controller.SendTestAlert(request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public void Constructor_WithNullAlertService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new AlertsController(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("alertService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new AlertsController(_alertServiceMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }
}
