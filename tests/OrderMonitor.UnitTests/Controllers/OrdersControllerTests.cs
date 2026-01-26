using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrderMonitor.Api.Controllers;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;

namespace OrderMonitor.UnitTests.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IStuckOrderService> _stuckOrderServiceMock;
    private readonly Mock<ILogger<OrdersController>> _loggerMock;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _stuckOrderServiceMock = new Mock<IStuckOrderService>();
        _loggerMock = new Mock<ILogger<OrdersController>>();
        _controller = new OrdersController(_stuckOrderServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetStuckOrders_ReturnsOkResult_WithStuckOrders()
    {
        // Arrange
        var expectedResponse = new StuckOrdersResponse
        {
            Total = 2,
            Items = new List<StuckOrderDto>
            {
                new() { OrderId = "CO1", StatusId = 3060, Status = "PreparationDone", HoursStuck = 10 },
                new() { OrderId = "CO2", StatusId = 4800, Status = "ErrorInFacility", HoursStuck = 72 }
            },
            GeneratedAt = DateTime.UtcNow
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetStuckOrders();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<StuckOrdersResponse>().Subject;
        response.Total.Should().Be(2);
        response.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetStuckOrders_WithStatusIdFilter_PassesFilterToService()
    {
        // Arrange
        var expectedResponse = new StuckOrdersResponse { Total = 1, Items = [] };
        StuckOrderQueryParams? capturedParams = null;

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .Callback<StuckOrderQueryParams, CancellationToken>((p, _) => capturedParams = p)
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetStuckOrders(statusId: 3060);

        // Assert
        capturedParams.Should().NotBeNull();
        capturedParams!.StatusId.Should().Be(3060);
    }

    [Fact]
    public async Task GetStuckOrders_WithStatusFilter_PassesFilterToService()
    {
        // Arrange
        var expectedResponse = new StuckOrdersResponse { Total = 1, Items = [] };
        StuckOrderQueryParams? capturedParams = null;

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .Callback<StuckOrderQueryParams, CancellationToken>((p, _) => capturedParams = p)
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetStuckOrders(status: "PreparationDone");

        // Assert
        capturedParams.Should().NotBeNull();
        capturedParams!.Status.Should().Be("PreparationDone");
    }

    [Fact]
    public async Task GetStuckOrders_WithMinMaxHours_PassesFilterToService()
    {
        // Arrange
        var expectedResponse = new StuckOrdersResponse { Total = 1, Items = [] };
        StuckOrderQueryParams? capturedParams = null;

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .Callback<StuckOrderQueryParams, CancellationToken>((p, _) => capturedParams = p)
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetStuckOrders(minHours: 10, maxHours: 100);

        // Assert
        capturedParams.Should().NotBeNull();
        capturedParams!.MinHours.Should().Be(10);
        capturedParams!.MaxHours.Should().Be(100);
    }

    [Fact]
    public async Task GetStuckOrders_WithPagination_PassesPaginationToService()
    {
        // Arrange
        var expectedResponse = new StuckOrdersResponse { Total = 100, Items = [] };
        StuckOrderQueryParams? capturedParams = null;

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .Callback<StuckOrderQueryParams, CancellationToken>((p, _) => capturedParams = p)
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetStuckOrders(limit: 50, offset: 20);

        // Assert
        capturedParams.Should().NotBeNull();
        capturedParams!.Limit.Should().Be(50);
        capturedParams!.Offset.Should().Be(20);
    }

    [Fact]
    public async Task GetStuckOrders_WithDefaultPagination_UsesDefaultValues()
    {
        // Arrange
        var expectedResponse = new StuckOrdersResponse { Total = 10, Items = [] };
        StuckOrderQueryParams? capturedParams = null;

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .Callback<StuckOrderQueryParams, CancellationToken>((p, _) => capturedParams = p)
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetStuckOrders();

        // Assert
        capturedParams.Should().NotBeNull();
        capturedParams!.Limit.Should().Be(100); // Default limit
        capturedParams!.Offset.Should().Be(0);  // Default offset
    }

    [Fact]
    public async Task GetStuckOrders_WhenServiceThrows_Returns500()
    {
        // Arrange
        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetStuckOrders();

        // Assert
        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetStuckOrders_ReturnsEmptyList_WhenNoStuckOrders()
    {
        // Arrange
        var expectedResponse = new StuckOrdersResponse
        {
            Total = 0,
            Items = [],
            GeneratedAt = DateTime.UtcNow
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetStuckOrders();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<StuckOrdersResponse>().Subject;
        response.Total.Should().Be(0);
        response.Items.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new OrdersController(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("stuckOrderService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new OrdersController(_stuckOrderServiceMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #region GetOrderStatusHistory Tests

    [Fact]
    public async Task GetOrderStatusHistory_ReturnsOkResult_WithHistory()
    {
        // Arrange
        var orderId = "CO12345";
        var expectedResponse = new OrderStatusHistoryResponse
        {
            OrderId = orderId,
            History = new List<OrderStatusHistoryDto>
            {
                new() { StatusId = 3001, Status = "Initialized_New", Timestamp = DateTime.UtcNow.AddHours(-48), Duration = "24h", IsStuck = false },
                new() { StatusId = 3060, Status = "PreparationDone", Timestamp = DateTime.UtcNow.AddHours(-24), Duration = "24h+ (STUCK)", IsStuck = true }
            }
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetOrderStatusHistoryAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetOrderStatusHistory(orderId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<OrderStatusHistoryResponse>().Subject;
        response.OrderId.Should().Be(orderId);
        response.History.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrderStatusHistory_WithValidOrderId_CallsServiceWithCorrectId()
    {
        // Arrange
        var orderId = "CO99999";
        string? capturedOrderId = null;

        _stuckOrderServiceMock
            .Setup(s => s.GetOrderStatusHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((id, _) => capturedOrderId = id)
            .ReturnsAsync(new OrderStatusHistoryResponse { OrderId = orderId, History = [] });

        // Act
        await _controller.GetOrderStatusHistory(orderId);

        // Assert
        capturedOrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task GetOrderStatusHistory_WithEmptyHistory_ReturnsEmptyList()
    {
        // Arrange
        var orderId = "CO00001";
        var expectedResponse = new OrderStatusHistoryResponse
        {
            OrderId = orderId,
            History = []
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetOrderStatusHistoryAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetOrderStatusHistory(orderId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<OrderStatusHistoryResponse>().Subject;
        response.History.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrderStatusHistory_WhenServiceThrows_Returns500()
    {
        // Arrange
        var orderId = "CO12345";
        _stuckOrderServiceMock
            .Setup(s => s.GetOrderStatusHistoryAsync(orderId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetOrderStatusHistory(orderId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetOrderStatusHistory_WithInvalidOrderId_ReturnsBadRequest(string? orderId)
    {
        // Act
        var result = await _controller.GetOrderStatusHistory(orderId!);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetOrderStatusHistory_HistoryIncludesDuration_ForEachStatusChange()
    {
        // Arrange
        var orderId = "CO12345";
        var expectedResponse = new OrderStatusHistoryResponse
        {
            OrderId = orderId,
            History = new List<OrderStatusHistoryDto>
            {
                new() { StatusId = 3001, Status = "Initialized_New", Timestamp = DateTime.UtcNow.AddHours(-72), Duration = "2h 30m", IsStuck = false },
                new() { StatusId = 3020, Status = "AwaitingAssets", Timestamp = DateTime.UtcNow.AddHours(-69).AddMinutes(-30), Duration = "45h 30m", IsStuck = false },
                new() { StatusId = 3060, Status = "PreparationDone", Timestamp = DateTime.UtcNow.AddHours(-24), Duration = "24h+ (STUCK)", IsStuck = true }
            }
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetOrderStatusHistoryAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetOrderStatusHistory(orderId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<OrderStatusHistoryResponse>().Subject;
        response.History.Should().AllSatisfy(h => h.Duration.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task GetOrderStatusHistory_FlagsCurrentStatusAsStuck_WhenExceedsThreshold()
    {
        // Arrange
        var orderId = "CO12345";
        var expectedResponse = new OrderStatusHistoryResponse
        {
            OrderId = orderId,
            History = new List<OrderStatusHistoryDto>
            {
                new() { StatusId = 3001, Status = "Initialized_New", Timestamp = DateTime.UtcNow.AddHours(-24), Duration = "12h", IsStuck = false },
                new() { StatusId = 3060, Status = "PreparationDone", Timestamp = DateTime.UtcNow.AddHours(-12), Duration = "12h+ (STUCK)", IsStuck = true }
            }
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetOrderStatusHistoryAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetOrderStatusHistory(orderId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<OrderStatusHistoryResponse>().Subject;
        var lastStatus = response.History.Last();
        lastStatus.IsStuck.Should().BeTrue();
    }

    #endregion

    #region GetStuckOrdersSummary Tests

    [Fact]
    public async Task GetStuckOrdersSummary_ReturnsOkResult_WithSummary()
    {
        // Arrange
        var expectedSummary = new StuckOrdersSummary
        {
            TotalStuckOrders = 150,
            ByThreshold = new Dictionary<string, int>
            {
                ["PrepStatuses (6h)"] = 100,
                ["FacilityStatuses (48h)"] = 50
            },
            ByStatusCategory = new Dictionary<string, int>
            {
                ["Preparation"] = 80,
                ["PrintBoxAlert"] = 20,
                ["Facility"] = 40,
                ["Shipping"] = 10
            },
            TopStatuses = new List<StatusCount>
            {
                new() { StatusId = 3060, Status = "PreparationDone", Count = 45 },
                new() { StatusId = 4800, Status = "ErrorInFacility", Count = 30 },
                new() { StatusId = 3720, Status = "PrintBoxAlert_RenderStatusFailure", Count = 15 }
            },
            GeneratedAt = DateTime.UtcNow
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _controller.GetStuckOrdersSummary();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var summary = okResult.Value.Should().BeOfType<StuckOrdersSummary>().Subject;
        summary.TotalStuckOrders.Should().Be(150);
    }

    [Fact]
    public async Task GetStuckOrdersSummary_ReturnsByThresholdCounts()
    {
        // Arrange
        var expectedSummary = new StuckOrdersSummary
        {
            TotalStuckOrders = 100,
            ByThreshold = new Dictionary<string, int>
            {
                ["PrepStatuses (6h)"] = 60,
                ["FacilityStatuses (48h)"] = 40
            },
            ByStatusCategory = new Dictionary<string, int>(),
            TopStatuses = [],
            GeneratedAt = DateTime.UtcNow
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _controller.GetStuckOrdersSummary();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var summary = okResult.Value.Should().BeOfType<StuckOrdersSummary>().Subject;
        summary.ByThreshold.Should().ContainKey("PrepStatuses (6h)");
        summary.ByThreshold.Should().ContainKey("FacilityStatuses (48h)");
        summary.ByThreshold["PrepStatuses (6h)"].Should().Be(60);
        summary.ByThreshold["FacilityStatuses (48h)"].Should().Be(40);
    }

    [Fact]
    public async Task GetStuckOrdersSummary_ReturnsByStatusCategoryCounts()
    {
        // Arrange
        var expectedSummary = new StuckOrdersSummary
        {
            TotalStuckOrders = 100,
            ByThreshold = new Dictionary<string, int>(),
            ByStatusCategory = new Dictionary<string, int>
            {
                ["Preparation"] = 50,
                ["PrintBoxAlert"] = 20,
                ["Facility"] = 30
            },
            TopStatuses = [],
            GeneratedAt = DateTime.UtcNow
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _controller.GetStuckOrdersSummary();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var summary = okResult.Value.Should().BeOfType<StuckOrdersSummary>().Subject;
        summary.ByStatusCategory.Should().HaveCount(3);
        summary.ByStatusCategory["Preparation"].Should().Be(50);
    }

    [Fact]
    public async Task GetStuckOrdersSummary_ReturnsTopStatuses()
    {
        // Arrange
        var expectedSummary = new StuckOrdersSummary
        {
            TotalStuckOrders = 100,
            ByThreshold = new Dictionary<string, int>(),
            ByStatusCategory = new Dictionary<string, int>(),
            TopStatuses = new List<StatusCount>
            {
                new() { StatusId = 3060, Status = "PreparationDone", Count = 45 },
                new() { StatusId = 4800, Status = "ErrorInFacility", Count = 30 },
                new() { StatusId = 3720, Status = "PrintBoxAlert_RenderStatusFailure", Count = 15 }
            },
            GeneratedAt = DateTime.UtcNow
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _controller.GetStuckOrdersSummary();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var summary = okResult.Value.Should().BeOfType<StuckOrdersSummary>().Subject;
        summary.TopStatuses.Should().HaveCount(3);
        summary.TopStatuses.First().Status.Should().Be("PreparationDone");
        summary.TopStatuses.First().Count.Should().Be(45);
    }

    [Fact]
    public async Task GetStuckOrdersSummary_WhenNoStuckOrders_ReturnsZeroCounts()
    {
        // Arrange
        var expectedSummary = new StuckOrdersSummary
        {
            TotalStuckOrders = 0,
            ByThreshold = new Dictionary<string, int>
            {
                ["PrepStatuses (6h)"] = 0,
                ["FacilityStatuses (48h)"] = 0
            },
            ByStatusCategory = new Dictionary<string, int>(),
            TopStatuses = [],
            GeneratedAt = DateTime.UtcNow
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _controller.GetStuckOrdersSummary();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var summary = okResult.Value.Should().BeOfType<StuckOrdersSummary>().Subject;
        summary.TotalStuckOrders.Should().Be(0);
        summary.TopStatuses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStuckOrdersSummary_WhenServiceThrows_Returns500()
    {
        // Arrange
        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetStuckOrdersSummary();

        // Assert
        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetStuckOrdersSummary_IncludesGeneratedAtTimestamp()
    {
        // Arrange
        var generatedAt = DateTime.UtcNow;
        var expectedSummary = new StuckOrdersSummary
        {
            TotalStuckOrders = 10,
            ByThreshold = new Dictionary<string, int>(),
            ByStatusCategory = new Dictionary<string, int>(),
            TopStatuses = [],
            GeneratedAt = generatedAt
        };

        _stuckOrderServiceMock
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _controller.GetStuckOrdersSummary();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var summary = okResult.Value.Should().BeOfType<StuckOrdersSummary>().Subject;
        summary.GeneratedAt.Should().BeCloseTo(generatedAt, TimeSpan.FromSeconds(1));
    }

    #endregion
}
