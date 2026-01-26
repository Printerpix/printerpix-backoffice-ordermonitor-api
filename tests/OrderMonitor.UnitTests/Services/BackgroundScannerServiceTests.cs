using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OrderMonitor.Core.Configuration;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;
using OrderMonitor.Infrastructure.Services;

namespace OrderMonitor.UnitTests.Services;

public class BackgroundScannerServiceTests
{
    private readonly Mock<IStuckOrderService> _stuckOrderServiceMock;
    private readonly Mock<IAlertService> _alertServiceMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<BackgroundScannerService>> _loggerMock;
    private readonly ScannerSettings _settings;

    public BackgroundScannerServiceTests()
    {
        _stuckOrderServiceMock = new Mock<IStuckOrderService>();
        _alertServiceMock = new Mock<IAlertService>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<BackgroundScannerService>>();

        _settings = new ScannerSettings
        {
            Enabled = true,
            IntervalMinutes = 15,
            BatchSize = 100
        };

        // Setup scope factory chain
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IStuckOrderService)))
            .Returns(_stuckOrderServiceMock.Object);
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IAlertService)))
            .Returns(_alertServiceMock.Object);
        _scopeMock
            .Setup(s => s.ServiceProvider)
            .Returns(_serviceProviderMock.Object);
        _scopeFactoryMock
            .Setup(f => f.CreateScope())
            .Returns(_scopeMock.Object);
    }

    private BackgroundScannerService CreateService(ScannerSettings? settings = null)
    {
        var options = Options.Create(settings ?? _settings);
        return new BackgroundScannerService(
            _scopeFactoryMock.Object,
            options,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteScanAsync_WhenStuckOrdersExist_SendsAlert()
    {
        // Arrange
        var summary = new StuckOrdersSummary
        {
            TotalStuckOrders = 5,
            ByThreshold = new Dictionary<string, int> { ["PrepStatuses (6h)"] = 3, ["FacilityStatuses (48h)"] = 2 },
            ByStatusCategory = new Dictionary<string, int>(),
            TopStatuses = new List<StatusCount>(),
            GeneratedAt = DateTime.UtcNow
        };

        var stuckOrders = new List<StuckOrderDto>
        {
            new() { OrderId = "CO1", StatusId = 3060, Status = "PreparationDone", HoursStuck = 10 },
            new() { OrderId = "CO2", StatusId = 4800, Status = "ErrorInFacility", HoursStuck = 72 }
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StuckOrdersResponse { Items = stuckOrders, Total = 2, GeneratedAt = DateTime.UtcNow });

        var service = CreateService();

        // Act
        var result = await service.ExecuteScanAsync();

        // Assert
        result.Should().BeTrue();
        _alertServiceMock.Verify(
            a => a.SendStuckOrdersAlertAsync(summary, It.IsAny<IEnumerable<StuckOrderDto>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteScanAsync_WhenNoStuckOrders_DoesNotSendAlert()
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

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var service = CreateService();

        // Act
        var result = await service.ExecuteScanAsync();

        // Assert
        result.Should().BeFalse();
        _alertServiceMock.Verify(
            a => a.SendStuckOrdersAlertAsync(It.IsAny<StuckOrdersSummary>(), It.IsAny<IEnumerable<StuckOrderDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteScanAsync_WhenScannerDisabled_DoesNotScan()
    {
        // Arrange
        var disabledSettings = new ScannerSettings { Enabled = false };
        var service = CreateService(disabledSettings);

        // Act
        var result = await service.ExecuteScanAsync();

        // Assert
        result.Should().BeFalse();
        _stuckOrderServiceMock.Verify(
            s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteScanAsync_UsesBatchSizeFromSettings()
    {
        // Arrange
        var settings = new ScannerSettings { Enabled = true, BatchSize = 50 };
        var summary = new StuckOrdersSummary
        {
            TotalStuckOrders = 10,
            ByThreshold = new Dictionary<string, int>(),
            ByStatusCategory = new Dictionary<string, int>(),
            TopStatuses = new List<StatusCount>(),
            GeneratedAt = DateTime.UtcNow
        };

        StuckOrderQueryParams? capturedParams = null;

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .Callback<StuckOrderQueryParams, CancellationToken>((p, _) => capturedParams = p)
            .ReturnsAsync(new StuckOrdersResponse { Items = new List<StuckOrderDto>(), Total = 0, GeneratedAt = DateTime.UtcNow });

        var service = CreateService(settings);

        // Act
        await service.ExecuteScanAsync();

        // Assert
        capturedParams.Should().NotBeNull();
        capturedParams!.Limit.Should().Be(50);
    }

    [Fact]
    public async Task ExecuteScanAsync_WhenServiceThrows_ReturnsFalseAndLogs()
    {
        // Arrange
        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var service = CreateService();

        // Act
        var result = await service.ExecuteScanAsync();

        // Assert
        result.Should().BeFalse();
        _alertServiceMock.Verify(
            a => a.SendStuckOrdersAlertAsync(It.IsAny<StuckOrdersSummary>(), It.IsAny<IEnumerable<StuckOrderDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteScanAsync_WhenAlertServiceThrows_ReturnsFalse()
    {
        // Arrange
        var summary = new StuckOrdersSummary
        {
            TotalStuckOrders = 5,
            ByThreshold = new Dictionary<string, int>(),
            ByStatusCategory = new Dictionary<string, int>(),
            TopStatuses = new List<StatusCount>(),
            GeneratedAt = DateTime.UtcNow
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StuckOrdersResponse { Items = new List<StuckOrderDto>(), Total = 0, GeneratedAt = DateTime.UtcNow });

        _alertServiceMock
            .Setup(a => a.SendStuckOrdersAlertAsync(It.IsAny<StuckOrdersSummary>(), It.IsAny<IEnumerable<StuckOrderDto>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP error"));

        var service = CreateService();

        // Act
        var result = await service.ExecuteScanAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullScopeFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new BackgroundScannerService(
            null!,
            Options.Create(_settings),
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("scopeFactory");
    }

    [Fact]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new BackgroundScannerService(
            _scopeFactoryMock.Object,
            null!,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new BackgroundScannerService(
            _scopeFactoryMock.Object,
            Options.Create(_settings),
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }
}
