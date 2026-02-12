using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderMonitor.Core.Configuration;
using OrderMonitor.Core.Models;
using OrderMonitor.Infrastructure.Data;
using OrderMonitor.Infrastructure.Data.Entities;

namespace OrderMonitor.IntegrationTests.Providers;

/// <summary>
/// Shared test logic for validating EfCoreOrderRepository against real database providers.
/// Each provider test class inherits this and provides its own DbContext configuration.
/// </summary>
public abstract class DatabaseProviderTestBase : IAsyncLifetime
{
    protected OrderMonitorDbContext DbContext { get; private set; } = null!;
    protected EfCoreOrderRepository Repository { get; private set; } = null!;

    protected abstract Task<DbContextOptions<OrderMonitorDbContext>> CreateDbContextOptionsAsync();

    public async Task InitializeAsync()
    {
        var options = await CreateDbContextOptionsAsync();
        DbContext = new OrderMonitorDbContext(options);

        // Create tables from EF model
        await DbContext.Database.EnsureCreatedAsync();

        // Seed test data
        await SeedTestDataAsync();

        Repository = new EfCoreOrderRepository(DbContext);
    }

    public async Task DisposeAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.DisposeAsync();
        await OnDisposeAsync();
    }

    protected virtual Task OnDisposeAsync() => Task.CompletedTask;

    private async Task SeedTestDataAsync()
    {
        // Tracking statuses
        DbContext.TrackingStatuses.AddRange(
            new TrackingStatusEntity { TrackingStatusId = 3050, TrackingStatusName = "PreparationStarted" },
            new TrackingStatusEntity { TrackingStatusId = 3060, TrackingStatusName = "PreparationDone" },
            new TrackingStatusEntity { TrackingStatusId = 4001, TrackingStatusName = "SentToFacility" },
            new TrackingStatusEntity { TrackingStatusId = 4200, TrackingStatusName = "PrintedInFacility" },
            new TrackingStatusEntity { TrackingStatusId = 6400, TrackingStatusName = "Completed" }
        );

        // Product types
        DbContext.MajorProductTypes.AddRange(
            new MajorProductTypeEntity { MProductTypeId = 1, MajorProductTypeName = "Photo Book" },
            new MajorProductTypeEntity { MProductTypeId = 2, MajorProductTypeName = "Calendar" }
        );

        // Specifications
        DbContext.SnSpecifications.AddRange(
            new SnSpecificationEntity { SnId = 100, MasterProductTypeId = 1 },
            new SnSpecificationEntity { SnId = 200, MasterProductTypeId = 2 }
        );

        // Partners
        DbContext.Partners.AddRange(
            new PartnerEntity { PartnerId = 10, PartnerDisplayName = "Facility Alpha", IsActive = true },
            new PartnerEntity { PartnerId = 20, PartnerDisplayName = "Facility Beta", IsActive = false }
        );

        // Orders
        DbContext.ConsolidationOrders.AddRange(
            new ConsolidationOrderEntity { CONumber = "CO001", OrderNumber = "ORD001", WebsiteCode = "US" },
            new ConsolidationOrderEntity { CONumber = "CO002", OrderNumber = "ORD002", WebsiteCode = "UK" },
            new ConsolidationOrderEntity { CONumber = "CO003", OrderNumber = "ORD003", WebsiteCode = "DE" },
            new ConsolidationOrderEntity { CONumber = "CO004", OrderNumber = "ORD004", WebsiteCode = "US" }
        );

        // Stuck prep order (>6h at status 3050)
        DbContext.OrderProductTrackings.Add(new OrderProductTrackingEntity
        {
            Id = 1, CONumber = "CO001", Status = 3050,
            LastUpdatedDate = DateTime.UtcNow.AddHours(-12),
            IsPrimaryComponent = true, OptSnSpId = 100, TPartnerCode = 10,
            OrderDate = DateTime.UtcNow.AddDays(-1)
        });

        // Stuck facility order (>48h at status 4001)
        DbContext.OrderProductTrackings.Add(new OrderProductTrackingEntity
        {
            Id = 2, CONumber = "CO002", Status = 4001,
            LastUpdatedDate = DateTime.UtcNow.AddHours(-72),
            IsPrimaryComponent = true, OptSnSpId = 200, TPartnerCode = 10,
            OrderDate = DateTime.UtcNow.AddDays(-5)
        });

        // Not stuck (status 3060, only 2h)
        DbContext.OrderProductTrackings.Add(new OrderProductTrackingEntity
        {
            Id = 3, CONumber = "CO003", Status = 3060,
            LastUpdatedDate = DateTime.UtcNow.AddHours(-2),
            IsPrimaryComponent = true, OptSnSpId = 100, TPartnerCode = 10,
            OrderDate = DateTime.UtcNow.AddDays(-1)
        });

        // Completed order (should be excluded)
        DbContext.OrderProductTrackings.Add(new OrderProductTrackingEntity
        {
            Id = 4, CONumber = "CO004", Status = 6400,
            LastUpdatedDate = DateTime.UtcNow.AddHours(-100),
            IsPrimaryComponent = true, OptSnSpId = 100,
            OrderDate = DateTime.UtcNow.AddDays(-10)
        });

        // Non-primary component (should be excluded)
        DbContext.OrderProductTrackings.Add(new OrderProductTrackingEntity
        {
            Id = 5, CONumber = "CO001", Status = 3050,
            LastUpdatedDate = DateTime.UtcNow.AddHours(-24),
            IsPrimaryComponent = false, OptSnSpId = 100,
            OrderDate = DateTime.UtcNow.AddDays(-1)
        });

        // Status history entry for CO001
        DbContext.OrderProductTrackings.Add(new OrderProductTrackingEntity
        {
            Id = 6, CONumber = "CO001", Status = 3060,
            LastUpdatedDate = DateTime.UtcNow.AddHours(-20),
            IsPrimaryComponent = true, OptSnSpId = 100,
            OrderDate = DateTime.UtcNow.AddDays(-1)
        });

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetStuckOrders_ReturnsOnlyStuckOrders()
    {
        var result = await Repository.GetStuckOrdersAsync(new StuckOrderQueryParams());
        var orders = result.ToList();

        orders.Should().HaveCount(2);
        orders.Should().Contain(o => o.OrderId == "CO001");
        orders.Should().Contain(o => o.OrderId == "CO002");
    }

    [Fact]
    public async Task GetStuckOrders_DeduplicatesByCONumber()
    {
        var result = await Repository.GetStuckOrdersAsync(new StuckOrderQueryParams());
        var orders = result.ToList();

        orders.Count(o => o.OrderId == "CO001").Should().Be(1);
    }

    [Fact]
    public async Task GetStuckOrders_IncludesRelatedData()
    {
        var result = await Repository.GetStuckOrdersAsync(new StuckOrderQueryParams());
        var order = result.First(o => o.OrderId == "CO001");

        order.OrderNumber.Should().Be("ORD001");
        order.Status.Should().Be("PreparationStarted");
        order.ProductType.Should().Be("Photo Book");
        order.Region.Should().Be("US");
        order.FacilityName.Should().Be("Facility Alpha");
    }

    [Fact]
    public async Task GetStuckOrders_CalculatesThresholdHours()
    {
        var result = await Repository.GetStuckOrdersAsync(new StuckOrderQueryParams());
        var orders = result.ToList();

        var prepOrder = orders.First(o => o.OrderId == "CO001");
        prepOrder.ThresholdHours.Should().Be(OrderStatusConfiguration.PrepThresholdHours);

        var facilityOrder = orders.First(o => o.OrderId == "CO002");
        facilityOrder.ThresholdHours.Should().Be(OrderStatusConfiguration.FacilityThresholdHours);
    }

    [Fact]
    public async Task GetStuckOrders_OrdersByHoursStuckDescending()
    {
        var result = await Repository.GetStuckOrdersAsync(new StuckOrderQueryParams());
        var orders = result.ToList();

        orders.Should().BeInDescendingOrder(o => o.HoursStuck);
    }

    [Fact]
    public async Task GetStuckOrders_AppliesPagination()
    {
        var result = await Repository.GetStuckOrdersAsync(
            new StuckOrderQueryParams { Limit = 1, Offset = 0 });

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetStuckOrders_FiltersByStatusId()
    {
        var result = await Repository.GetStuckOrdersAsync(
            new StuckOrderQueryParams { StatusId = 3050 });

        result.Should().OnlyContain(o => o.StatusId == 3050);
    }

    [Fact]
    public async Task GetStuckOrdersCount_ReturnsDistinctCount()
    {
        var count = await Repository.GetStuckOrdersCountAsync();
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetOrderStatusHistory_ReturnsOrderedTimeline()
    {
        var result = await Repository.GetOrderStatusHistoryAsync("CO001");
        var history = result.ToList();

        history.Should().HaveCountGreaterThanOrEqualTo(2);
        history.Should().BeInAscendingOrder(h => h.Timestamp);
    }

    [Fact]
    public async Task GetOrderStatusHistory_NonExistentOrder_ReturnsEmpty()
    {
        var result = await Repository.GetOrderStatusHistoryAsync("NONEXISTENT");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrderStatusHistory_ExcludesNonPrimaryComponents()
    {
        var result = await Repository.GetOrderStatusHistoryAsync("CO001");
        var history = result.ToList();

        history.Should().OnlyContain(h =>
            h.StatusId == 3050 || h.StatusId == 3060);
    }
}
