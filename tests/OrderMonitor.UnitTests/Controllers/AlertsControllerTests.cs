using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrderMonitor.Api.Controllers;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;

namespace OrderMonitor.UnitTests.Controllers;

public class AlertsControllerTests
{
    private readonly Mock<IAlertService> _alertServiceMock;
    private readonly Mock<IStuckOrderService> _stuckOrderServiceMock;
    private readonly Mock<ILogger<AlertsController>> _loggerMock;
    private readonly AlertsController _controller;

    public AlertsControllerTests()
    {
        _alertServiceMock = new Mock<IAlertService>();
        _stuckOrderServiceMock = new Mock<IStuckOrderService>();
        _loggerMock = new Mock<ILogger<AlertsController>>();
        _controller = new AlertsController(
            _alertServiceMock.Object,
            _stuckOrderServiceMock.Object,
            _loggerMock.Object);
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
        var act = () => new AlertsController(null!, _stuckOrderServiceMock.Object, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("alertService");
    }

    [Fact]
    public void Constructor_WithNullStuckOrderService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new AlertsController(_alertServiceMock.Object, null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("stuckOrderService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new AlertsController(_alertServiceMock.Object, _stuckOrderServiceMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #region TriggerStuckOrdersAlert Tests

    [Fact]
    public async Task TriggerStuckOrdersAlert_WithNoStuckOrders_ReturnsOkWithZeroCount()
    {
        // Arrange
        var summary = new StuckOrdersSummary
        {
            TotalStuckOrders = 0,
            GeneratedAt = DateTime.UtcNow
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        // Act
        var result = await _controller.TriggerStuckOrdersAlert();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TriggerAlertResponse>().Subject;
        response.Success.Should().BeTrue();
        response.StuckOrdersCount.Should().Be(0);
        response.Message.Should().Contain("No stuck orders");

        _alertServiceMock.Verify(
            s => s.SendStuckOrdersAlertAsync(It.IsAny<StuckOrdersSummary>(), It.IsAny<IEnumerable<StuckOrderDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TriggerStuckOrdersAlert_WithStuckOrders_SendsAlertAndReturnsOk()
    {
        // Arrange
        var summary = new StuckOrdersSummary
        {
            TotalStuckOrders = 5,
            GeneratedAt = DateTime.UtcNow,
            TopStatuses = new List<StatusCount>()
        };

        var stuckOrders = new StuckOrdersResponse
        {
            Items = new List<StuckOrderDto>
            {
                new() { OrderId = "CO1", StatusId = 3060, Status = "PreparationDone", HoursStuck = 10 }
            },
            Total = 1,
            GeneratedAt = DateTime.UtcNow
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stuckOrders);

        _alertServiceMock
            .Setup(s => s.SendStuckOrdersAlertAsync(summary, stuckOrders.Items, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.TriggerStuckOrdersAlert();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TriggerAlertResponse>().Subject;
        response.Success.Should().BeTrue();
        response.StuckOrdersCount.Should().Be(5);
        response.Message.Should().Contain("5 stuck orders");

        _alertServiceMock.Verify(
            s => s.SendStuckOrdersAlertAsync(summary, stuckOrders.Items, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TriggerStuckOrdersAlert_WhenSmtpFails_Returns500()
    {
        // Arrange
        var summary = new StuckOrdersSummary { TotalStuckOrders = 1, GeneratedAt = DateTime.UtcNow };
        var stuckOrders = new StuckOrdersResponse { Items = new List<StuckOrderDto>(), Total = 0, GeneratedAt = DateTime.UtcNow };

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stuckOrders);

        _alertServiceMock
            .Setup(s => s.SendStuckOrdersAlertAsync(It.IsAny<StuckOrdersSummary>(), It.IsAny<IEnumerable<StuckOrderDto>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP password not configured"));

        // Act
        var result = await _controller.TriggerStuckOrdersAlert();

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion
}
