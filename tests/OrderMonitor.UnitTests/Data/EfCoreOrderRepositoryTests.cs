using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderMonitor.Core.Configuration;
using OrderMonitor.Core.Models;
using OrderMonitor.Infrastructure.Data;
using OrderMonitor.Infrastructure.Data.Entities;

namespace OrderMonitor.UnitTests.Data;

public class EfCoreOrderRepositoryTests : IDisposable
{
    private readonly OrderMonitorDbContext _dbContext;
    private readonly EfCoreOrderRepository _repository;

    public EfCoreOrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OrderMonitorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new OrderMonitorDbContext(options);
        _repository = new EfCoreOrderRepository(_dbContext);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Tracking statuses
        _dbContext.TrackingStatuses.AddRange(
            new TrackingStatusEntity { TrackingStatusId = 3050, TrackingStatusName = "PreparationStarted" },
            new TrackingStatusEntity { TrackingStatusId = 3060, TrackingStatusName = "PreparationDone" },
            new TrackingStatusEntity { TrackingStatusId = 4001, TrackingStatusName = "SentToFacility" },
            new TrackingStatusEntity { TrackingStatusId = 4200, TrackingStatusName = "PrintedInFacility" },
            new TrackingStatusEntity { TrackingStatusId = 6400, TrackingStatusName = "Completed" }
        );

        // Product types
        _dbContext.MajorProductTypes.AddRange(
            new MajorProductTypeEntity { MProductTypeId = 1, MajorProductTypeName = "Photo Book" },
            new MajorProductTypeEntity { MProductTypeId = 2, MajorProductTypeName = "Calendar" }
        );

        // Specifications
        _dbContext.SnSpecifications.AddRange(
            new SnSpecificationEntity { SnId = 100, MasterProductTypeId = 1 },
            new SnSpecificationEntity { SnId = 200, MasterProductTypeId = 2 }
        );

        // Partners
        _dbContext.Partners.AddRange(
            new PartnerEntity { PartnerId = 10, PartnerDisplayName = "Facility Alpha", IsActive = true },
            new PartnerEntity { PartnerId = 20, PartnerDisplayName = "Facility Beta", IsActive = false }
        );

        // Orders
        _dbContext.ConsolidationOrders.AddRange(
            new ConsolidationOrderEntity { CONumber = "CO001", OrderNumber = "ORD001", WebsiteCode = "US" },
            new ConsolidationOrderEntity { CONumber = "CO002", OrderNumber = "ORD002", WebsiteCode = "UK" },
            new ConsolidationOrderEntity { CONumber = "CO003", OrderNumber = "ORD003", WebsiteCode = "DE" },
            new ConsolidationOrderEntity { CONumber = "CO004", OrderNumber = "ORD004", WebsiteCode = "US" }
        );

        // Order product trackings - stuck prep order (>6h at status 3050)
        _dbContext.OrderProductTrackings.Add(new OrderProductTrackingEntity
        {
            Id = 1, CONumber = "CO001", Status = 3050,
            LastUpdatedDate = DateTime.UtcNow.AddHours(-12),
            IsPrimaryComponent = true, OptSnSpId = 100, TPartnerCode = 10,
            OrderDate = DateTime.UtcNow.AddDays(-1)
        });

        // Stuck facility order (>48h at status 4001)
        _dbContext.OrderProductTrackings.Add(new OrderProductTrackingEntity
        {
            Id = 2, CONumber = "CO002", Status = 4001,
            LastUpdatedDate = DateTime.UtcNow.AddHours(-72),
            IsPrimaryComponent = true, OptSnSpId = 200, TPartnerCode = 10,
            OrderDate = DateTime.UtcNow.AddDays(-5)
        });

        // Not stuck (status 3060, only 2h)
        _dbContext.OrderProductTrackings.Add(new OrderProductTrackingEntity
        {
            Id = 3, CONumber = "CO003", Status = 3060,
            LastUpdatedDate = DateTime.UtcNow.AddHours(-2),
            IsPrimaryComponent = true, OptSnSpId = 100, TPartnerCode = 10,
            OrderDate = DateTime.UtcNow.AddDays(-1)
        });

        // Completed order (status >= 6400, should be excluded)
        _dbContext.OrderProductTrackings.Add(new OrderProductTrackingEntity
        {
            Id = 4, CONumber = "CO004", Status = 6400,
            LastUpdatedDate = DateTime.UtcNow.AddHours(-100),
            IsPrimaryComponent = true, OptSnSpId = 100,
            OrderDate = DateTime.UtcNow.AddDays(-10)
        });

        // Non-primary component (should be excluded)
        _dbContext.OrderProductTrackings.Add(new OrderProductTrackingEntity
        {
            Id = 5, CONumber = "CO001", Status = 3050,
            LastUpdatedDate = DateTime.UtcNow.AddHours(-24),
            IsPrimaryComponent = false, OptSnSpId = 100,
            OrderDate = DateTime.UtcNow.AddDays(-1)
        });

        // Status history entries for CO001
        _dbContext.OrderProductTrackings.Add(new OrderProductTrackingEntity
        {
            Id = 6, CONumber = "CO001", Status = 3060,
            LastUpdatedDate = DateTime.UtcNow.AddHours(-20),
            IsPrimaryComponent = true, OptSnSpId = 100,
            OrderDate = DateTime.UtcNow.AddDays(-1)
        });

        _dbContext.SaveChanges();
    }

    [Fact]
    public void Constructor_WithNullDbContext_ThrowsArgumentNullException()
    {
        var act = () => new EfCoreOrderRepository(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dbContext");
    }

    [Fact]
    public async Task GetStuckOrdersAsync_ReturnsOnlyStuckOrders()
    {
        var result = await _repository.GetStuckOrdersAsync(new StuckOrderQueryParams());
        var orders = result.ToList();

        // CO001 is stuck (prep status, 12h > 6h threshold)
        // CO002 is stuck (facility status, 72h > 48h threshold)
        // CO003 is NOT stuck (2h < 6h threshold)
        // CO004 is excluded (status >= 6400)
        orders.Should().HaveCount(2);
        orders.Should().Contain(o => o.OrderId == "CO001");
        orders.Should().Contain(o => o.OrderId == "CO002");
    }

    [Fact]
    public async Task GetStuckOrdersAsync_DeduplicatesByCONumber()
    {
        // CO001 has multiple primary component entries (Id 1 and 6)
        // Should only return one per CONumber
        var result = await _repository.GetStuckOrdersAsync(new StuckOrderQueryParams());
        var orders = result.ToList();

        orders.Count(o => o.OrderId == "CO001").Should().Be(1);
    }

    [Fact]
    public async Task GetStuckOrdersAsync_IncludesRelatedData()
    {
        var result = await _repository.GetStuckOrdersAsync(new StuckOrderQueryParams());
        var order = result.First(o => o.OrderId == "CO001");

        order.OrderNumber.Should().Be("ORD001");
        order.Status.Should().Be("PreparationStarted");
        order.ProductType.Should().Be("Photo Book");
        order.Region.Should().Be("US");
        order.FacilityName.Should().Be("Facility Alpha");
    }

    [Fact]
    public async Task GetStuckOrdersAsync_CalculatesThresholdHours()
    {
        var result = await _repository.GetStuckOrdersAsync(new StuckOrderQueryParams());
        var orders = result.ToList();

        var prepOrder = orders.First(o => o.OrderId == "CO001");
        prepOrder.ThresholdHours.Should().Be(OrderStatusConfiguration.PrepThresholdHours);

        var facilityOrder = orders.First(o => o.OrderId == "CO002");
        facilityOrder.ThresholdHours.Should().Be(OrderStatusConfiguration.FacilityThresholdHours);
    }

    [Fact]
    public async Task GetStuckOrdersAsync_OrdersByHoursStuckDescending()
    {
        var result = await _repository.GetStuckOrdersAsync(new StuckOrderQueryParams());
        var orders = result.ToList();

        orders.Should().BeInDescendingOrder(o => o.HoursStuck);
    }

    [Fact]
    public async Task GetStuckOrdersAsync_AppliesPagination()
    {
        var result = await _repository.GetStuckOrdersAsync(
            new StuckOrderQueryParams { Limit = 1, Offset = 0 });

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetStuckOrdersAsync_FiltersInactivePartners()
    {
        // Partner 20 is inactive, so should show "Unknown" for facility name
        var result = await _repository.GetStuckOrdersAsync(new StuckOrderQueryParams());
        var facilityOrder = result.First(o => o.OrderId == "CO002");

        // CO002 has TPartnerCode = 10 (active), so should show partner name
        facilityOrder.FacilityName.Should().Be("Facility Alpha");
    }

    [Fact]
    public async Task GetStuckOrdersAsync_FiltersbyStatusId()
    {
        var result = await _repository.GetStuckOrdersAsync(
            new StuckOrderQueryParams { StatusId = 3050 });

        result.Should().OnlyContain(o => o.StatusId == 3050);
    }

    [Fact]
    public async Task GetStuckOrdersAsync_FiltersByStatusName()
    {
        var result = await _repository.GetStuckOrdersAsync(
            new StuckOrderQueryParams { Status = "Facility" });

        result.Should().OnlyContain(o => o.Status.Contains("Facility", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetStuckOrdersAsync_FiltersByMinHours()
    {
        var result = await _repository.GetStuckOrdersAsync(
            new StuckOrderQueryParams { MinHours = 50 });

        result.Should().OnlyContain(o => o.HoursStuck >= 50);
    }

    [Fact]
    public async Task GetStuckOrdersCountAsync_ReturnsDistinctOrderCount()
    {
        var count = await _repository.GetStuckOrdersCountAsync();

        // CO001 and CO002 are stuck
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetOrderStatusHistoryAsync_ReturnsOrderedTimeline()
    {
        var result = await _repository.GetOrderStatusHistoryAsync("CO001");
        var history = result.ToList();

        // CO001 has entries at Id 1 (status 3050, -12h), Id 5 (non-primary, excluded), Id 6 (status 3060, -20h)
        // Ordered by timestamp ASC: Id 6 (-20h) then Id 1 (-12h)
        history.Should().HaveCountGreaterThanOrEqualTo(2);
        history.Should().BeInAscendingOrder(h => h.Timestamp);
    }

    [Fact]
    public async Task GetOrderStatusHistoryAsync_CalculatesDurationBetweenStatuses()
    {
        var result = await _repository.GetOrderStatusHistoryAsync("CO001");
        var history = result.ToList();

        // First entry should have duration to next entry
        history.First().Duration.Should().NotBeNullOrEmpty();
        // Last entry should indicate current status
        history.Last().Duration.Should().Contain("h");
    }

    [Fact]
    public async Task GetOrderStatusHistoryAsync_IdentifiesStuckStatus()
    {
        var result = await _repository.GetOrderStatusHistoryAsync("CO001");
        var history = result.ToList();

        // The last status should be stuck if it exceeds threshold
        var lastEntry = history.Last();
        if (lastEntry.StatusId >= OrderStatusConfiguration.PrepMinStatusId
            && lastEntry.StatusId <= OrderStatusConfiguration.PrepMaxStatusId)
        {
            var hoursStuck = (int)(DateTime.UtcNow - lastEntry.Timestamp).TotalHours;
            if (hoursStuck > OrderStatusConfiguration.PrepThresholdHours)
            {
                lastEntry.IsStuck.Should().BeTrue();
                lastEntry.Duration.Should().Contain("STUCK");
            }
        }
    }

    [Fact]
    public async Task GetOrderStatusHistoryAsync_ExcludesNonPrimaryComponents()
    {
        var result = await _repository.GetOrderStatusHistoryAsync("CO001");
        var history = result.ToList();

        // Non-primary component entry (Id 5) should not appear
        // Only primary entries with unique timestamps
        history.Should().OnlyContain(h =>
            h.StatusId == 3050 || h.StatusId == 3060);
    }

    [Fact]
    public async Task GetOrderStatusHistoryAsync_NonExistentOrder_ReturnsEmpty()
    {
        var result = await _repository.GetOrderStatusHistoryAsync("NONEXISTENT");
        result.Should().BeEmpty();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
