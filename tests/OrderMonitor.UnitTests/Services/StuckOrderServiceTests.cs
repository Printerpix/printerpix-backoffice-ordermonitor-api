using FluentAssertions;
using Moq;
using OrderMonitor.Core.Configuration;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;
using OrderMonitor.Core.Services;

namespace OrderMonitor.UnitTests.Services;

public class StuckOrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly StuckOrderService _service;

    public StuckOrderServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _service = new StuckOrderService(_orderRepositoryMock.Object);
    }

    [Fact]
    public async Task GetStuckOrdersAsync_ReturnsStuckOrdersResponse()
    {
        // Arrange
        var queryParams = new StuckOrderQueryParams { Limit = 10, Offset = 0 };
        var stuckOrders = new List<StuckOrderDto>
        {
            new() { OrderId = "CO1", StatusId = 3060, Status = "PreparationDone", HoursStuck = 10 },
            new() { OrderId = "CO2", StatusId = 4800, Status = "ErrorInFacility", HoursStuck = 72 }
        };

        _orderRepositoryMock
            .Setup(r => r.GetStuckOrdersAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stuckOrders);

        // Act
        var result = await _service.GetStuckOrdersAsync(queryParams);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetStuckOrdersAsync_WithEmptyResult_ReturnsEmptyResponse()
    {
        // Arrange
        var queryParams = new StuckOrderQueryParams();
        _orderRepositoryMock
            .Setup(r => r.GetStuckOrdersAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StuckOrderDto>());

        // Act
        var result = await _service.GetStuckOrdersAsync(queryParams);

        // Assert
        result.Total.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStuckOrdersAsync_PassesParametersToRepository()
    {
        // Arrange
        var queryParams = new StuckOrderQueryParams
        {
            StatusId = 3060,
            Status = "PreparationDone",
            MinHours = 10,
            MaxHours = 100,
            Limit = 50,
            Offset = 10
        };
        StuckOrderQueryParams? capturedParams = null;

        _orderRepositoryMock
            .Setup(r => r.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .Callback<StuckOrderQueryParams, CancellationToken>((p, _) => capturedParams = p)
            .ReturnsAsync(new List<StuckOrderDto>());

        // Act
        await _service.GetStuckOrdersAsync(queryParams);

        // Assert
        capturedParams.Should().NotBeNull();
        capturedParams!.StatusId.Should().Be(3060);
        capturedParams.Status.Should().Be("PreparationDone");
        capturedParams.MinHours.Should().Be(10);
        capturedParams.MaxHours.Should().Be(100);
        capturedParams.Limit.Should().Be(50);
        capturedParams.Offset.Should().Be(10);
    }

    [Fact]
    public async Task GetStuckOrdersSummaryAsync_ReturnsSummaryWithCounts()
    {
        // Arrange
        _orderRepositoryMock
            .Setup(r => r.GetStuckOrdersCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        _orderRepositoryMock
            .Setup(r => r.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StuckOrderDto>
            {
                new() { StatusId = 3060, Status = "PreparationDone", HoursStuck = 10 },
                new() { StatusId = 3060, Status = "PreparationDone", HoursStuck = 8 },
                new() { StatusId = 4800, Status = "ErrorInFacility", HoursStuck = 72 }
            });

        // Act
        var result = await _service.GetStuckOrdersSummaryAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalStuckOrders.Should().Be(42);
        result.TopStatuses.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetOrderStatusHistoryAsync_ReturnsHistoryForOrder()
    {
        // Arrange
        var orderId = "CO12345";
        var historyItems = new List<OrderStatusHistoryDto>
        {
            new() { StatusId = 3001, Status = "Initialized_New", Timestamp = DateTime.UtcNow.AddHours(-24) },
            new() { StatusId = 3060, Status = "PreparationDone", Timestamp = DateTime.UtcNow.AddHours(-12) }
        };

        _orderRepositoryMock
            .Setup(r => r.GetOrderStatusHistoryAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(historyItems);

        // Act
        var result = await _service.GetOrderStatusHistoryAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderId);
        result.History.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrderStatusHistoryAsync_WithNoHistory_ReturnsEmptyHistory()
    {
        // Arrange
        var orderId = "CO99999";
        _orderRepositoryMock
            .Setup(r => r.GetOrderStatusHistoryAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderStatusHistoryDto>());

        // Act
        var result = await _service.GetOrderStatusHistoryAsync(orderId);

        // Assert
        result.OrderId.Should().Be(orderId);
        result.History.Should().BeEmpty();
    }

    [Theory]
    [InlineData(3060, -10, true)]   // Prep status, 10 hours ago -> stuck (> 6h threshold)
    [InlineData(3060, -2, false)]   // Prep status, 2 hours ago -> not stuck (< 6h threshold)
    [InlineData(4800, -72, true)]   // Facility status, 72 hours ago -> stuck (> 48h threshold)
    [InlineData(4800, -24, false)]  // Facility status, 24 hours ago -> not stuck (< 48h threshold)
    [InlineData(9999, -30, true)]   // Unknown status, 30 hours ago -> stuck (> 24h default)
    [InlineData(9999, -12, false)]  // Unknown status, 12 hours ago -> not stuck (< 24h default)
    public void IsOrderStuck_ReturnsCorrectResult(int statusId, int hoursAgo, bool expected)
    {
        // Arrange
        var statusUpdatedAt = DateTime.UtcNow.AddHours(hoursAgo);

        // Act
        var result = _service.IsOrderStuck(statusId, statusUpdatedAt);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new StuckOrderService(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("orderRepository");
    }
}
