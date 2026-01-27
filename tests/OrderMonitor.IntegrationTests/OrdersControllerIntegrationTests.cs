using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Moq;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;

namespace OrderMonitor.IntegrationTests;

/// <summary>
/// Integration tests for OrdersController endpoints.
/// Tests HTTP request/response handling through the full API pipeline.
/// </summary>
public class OrdersControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OrdersControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region GET /api/orders/stuck Tests

    [Fact]
    public async Task GetStuckOrders_WithNoFilters_ReturnsOkWithOrders()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/stuck");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<StuckOrdersResponse>();
        result.Should().NotBeNull();
        result!.Total.Should().Be(5);
        result.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetStuckOrders_WithLimitParameter_ReturnsLimitedResults()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        _factory.MockStuckOrderService
            .Setup(s => s.GetStuckOrdersAsync(
                It.Is<StuckOrderQueryParams>(p => p.Limit == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StuckOrdersResponse
            {
                Total = 100,
                Items = new List<StuckOrderDto>
                {
                    new() { OrderId = "CO12345", OrderNumber = "12345", StatusId = 15, Status = "Submitted", HoursStuck = 24, ThresholdHours = 6, ProductType = "Photo Book" },
                    new() { OrderId = "CO12346", OrderNumber = "12346", StatusId = 17, Status = "Pending Print", HoursStuck = 72, ThresholdHours = 48, ProductType = "Calendar" }
                }
            });
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/stuck?limit=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<StuckOrdersResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetStuckOrders_WithStatusFilter_FiltersCorrectly()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        _factory.MockStuckOrderService
            .Setup(s => s.GetStuckOrdersAsync(
                It.IsAny<StuckOrderQueryParams>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StuckOrdersResponse
            {
                Total = 50,
                Items = new List<StuckOrderDto>
                {
                    new() { OrderId = "CO12345", Status = "Submitted", HoursStuck = 24, ThresholdHours = 6, ProductType = "Photo Book" }
                }
            });
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/stuck?status=Submitted");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<StuckOrdersResponse>();
        result.Should().NotBeNull();
        result!.Total.Should().Be(50);

        // Verify the service was called with the correct parameters
        _factory.MockStuckOrderService.Verify(
            s => s.GetStuckOrdersAsync(
                It.Is<StuckOrderQueryParams>(p => p.Status == "Submitted"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStuckOrders_WithStatusIdFilter_FiltersCorrectly()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        _factory.MockStuckOrderService
            .Setup(s => s.GetStuckOrdersAsync(
                It.Is<StuckOrderQueryParams>(p => p.StatusId == 15),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StuckOrdersResponse
            {
                Total = 30,
                Items = new List<StuckOrderDto>
                {
                    new() { OrderId = "CO12345", StatusId = 15, Status = "Submitted", HoursStuck = 24, ThresholdHours = 6, ProductType = "Photo Book" }
                }
            });
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/stuck?statusId=15");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<StuckOrdersResponse>();
        result.Should().NotBeNull();
        result!.Total.Should().Be(30);
    }

    [Fact]
    public async Task GetStuckOrders_WithMinMaxHoursFilter_FiltersCorrectly()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        _factory.MockStuckOrderService
            .Setup(s => s.GetStuckOrdersAsync(
                It.IsAny<StuckOrderQueryParams>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StuckOrdersResponse
            {
                Total = 20,
                Items = new List<StuckOrderDto>
                {
                    new() { OrderId = "CO12345", HoursStuck = 48, ThresholdHours = 6, ProductType = "Photo Book" }
                }
            });
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/stuck?minHours=24&maxHours=72");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<StuckOrdersResponse>();
        result.Should().NotBeNull();
        result!.Total.Should().Be(20);

        // Verify the service was called with the correct parameters
        _factory.MockStuckOrderService.Verify(
            s => s.GetStuckOrdersAsync(
                It.Is<StuckOrderQueryParams>(p => p.MinHours == 24 && p.MaxHours == 72),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStuckOrders_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        _factory.MockStuckOrderService
            .Setup(s => s.GetStuckOrdersAsync(
                It.Is<StuckOrderQueryParams>(p => p.Offset == 10 && p.Limit == 5),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StuckOrdersResponse
            {
                Total = 100,
                Items = new List<StuckOrderDto>
                {
                    new() { OrderId = "CO12355", HoursStuck = 48, ThresholdHours = 6, ProductType = "Photo Book" }
                }
            });
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/stuck?offset=10&limit=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<StuckOrdersResponse>();
        result.Should().NotBeNull();
        result!.Total.Should().Be(100);
    }

    [Fact]
    public async Task GetStuckOrders_WhenServiceThrows_Returns500()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        _factory.MockStuckOrderService
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/stuck");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetStuckOrders_ReturnsJsonContentType()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/stuck");

        // Assert
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    #endregion

    #region GET /api/orders/{orderId}/status-history Tests

    [Fact]
    public async Task GetOrderStatusHistory_WithValidOrderId_ReturnsHistory()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/CO12345/status-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OrderStatusHistoryResponse>();
        result.Should().NotBeNull();
        result!.OrderId.Should().Be("CO12345");
        result.History.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetOrderStatusHistory_ReturnsHistoryWithCorrectStructure()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/CO12345/status-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OrderStatusHistoryResponse>();
        result.Should().NotBeNull();

        var history = result!.History.ToList();
        history.Should().Contain(h => h.Status == "Submitted" && h.IsStuck == true);
        history.Should().Contain(h => h.Status == "Created" && h.IsStuck == false);
    }

    [Fact]
    public async Task GetOrderStatusHistory_WhenServiceThrows_Returns500()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        _factory.MockStuckOrderService
            .Setup(s => s.GetOrderStatusHistoryAsync("CO99999", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Order not found"));
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/CO99999/status-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/orders/stuck/summary Tests

    [Fact]
    public async Task GetStuckOrdersSummary_ReturnsOkWithSummary()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/stuck/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<StuckOrdersSummary>();
        result.Should().NotBeNull();
        result!.TotalStuckOrders.Should().Be(150);
    }

    [Fact]
    public async Task GetStuckOrdersSummary_ReturnsThresholdBreakdown()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/stuck/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<StuckOrdersSummary>();
        result.Should().NotBeNull();
        result!.ByThreshold.Should().ContainKey("PrepStatuses (6h)");
        result.ByThreshold.Should().ContainKey("FacilityStatuses (48h)");
        result.ByThreshold["PrepStatuses (6h)"].Should().Be(80);
        result.ByThreshold["FacilityStatuses (48h)"].Should().Be(70);
    }

    [Fact]
    public async Task GetStuckOrdersSummary_ReturnsTopStatuses()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/stuck/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<StuckOrdersSummary>();
        result.Should().NotBeNull();
        result!.TopStatuses.Should().HaveCount(3);
        result.TopStatuses.First().Status.Should().Be("Submitted");
        result.TopStatuses.First().Count.Should().Be(50);
    }

    [Fact]
    public async Task GetStuckOrdersSummary_WhenServiceThrows_Returns500()
    {
        // Arrange
        _factory.SetupDefaultMocks();
        _factory.MockStuckOrderService
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database timeout"));
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/stuck/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    #endregion
}
