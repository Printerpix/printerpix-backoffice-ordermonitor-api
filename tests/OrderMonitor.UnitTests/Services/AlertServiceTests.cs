using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OrderMonitor.Core.Models;
using OrderMonitor.Infrastructure.Services;

namespace OrderMonitor.UnitTests.Services;

public class AlertServiceTests
{
    private readonly Mock<ILogger<AlertService>> _loggerMock;
    private readonly AlertService _service;

    public AlertServiceTests()
    {
        _loggerMock = new Mock<ILogger<AlertService>>();
        _service = new AlertService(_loggerMock.Object);
    }

    [Fact]
    public async Task SendStuckOrdersAlertAsync_LogsWarningWithSummary()
    {
        // Arrange
        var summary = new StuckOrdersSummary
        {
            TotalStuckOrders = 10,
            ByThreshold = new Dictionary<string, int>
            {
                ["PrepStatuses (6h)"] = 6,
                ["FacilityStatuses (48h)"] = 4
            },
            ByStatusCategory = new Dictionary<string, int>(),
            TopStatuses = new List<StatusCount>(),
            GeneratedAt = DateTime.UtcNow
        };

        var topOrders = new List<StuckOrderDto>
        {
            new() { OrderId = "CO1", StatusId = 3060, Status = "PreparationDone", HoursStuck = 10 },
            new() { OrderId = "CO2", StatusId = 4800, Status = "ErrorInFacility", HoursStuck = 72 }
        };

        // Act
        await _service.SendStuckOrdersAlertAsync(summary, topOrders);

        // Assert - verify logging occurred (no exception thrown)
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(1));
    }

    [Fact]
    public async Task SendStuckOrdersAlertAsync_WithEmptyOrders_DoesNotThrow()
    {
        // Arrange
        var summary = new StuckOrdersSummary
        {
            TotalStuckOrders = 0,
            ByThreshold = new Dictionary<string, int>(),
            ByStatusCategory = new Dictionary<string, int>(),
            TopStatuses = new List<StatusCount>(),
            GeneratedAt = DateTime.UtcNow
        };

        var topOrders = new List<StuckOrderDto>();

        // Act
        var act = async () => await _service.SendStuckOrdersAlertAsync(summary, topOrders);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendTestAlertAsync_LogsInformation()
    {
        // Arrange
        var recipientEmail = "test@example.com";

        // Act
        await _service.SendTestAlertAsync(recipientEmail);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(1));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new AlertService(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }
}
