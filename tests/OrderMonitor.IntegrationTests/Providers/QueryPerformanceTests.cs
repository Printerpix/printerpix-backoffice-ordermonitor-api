using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderMonitor.Core.Configuration;
using OrderMonitor.Core.Models;
using OrderMonitor.Infrastructure.Data;
using OrderMonitor.Infrastructure.Data.Entities;
using Testcontainers.PostgreSql;

namespace OrderMonitor.IntegrationTests.Providers;

/// <summary>
/// Performance benchmarks for EfCoreOrderRepository queries.
/// Validates that query response times stay within acceptable thresholds.
/// Uses PostgreSQL as the reference provider with realistic data volumes.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Performance")]
public class QueryPerformanceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("ordermonitor_perf")
        .WithUsername("perfuser")
        .WithPassword("perfpass123!")
        .Build();

    private OrderMonitorDbContext _dbContext = null!;
    private EfCoreOrderRepository _repository = null!;

    private const int SeedOrderCount = 500;
    private const int MaxQueryTimeMs = 2000;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        var options = new DbContextOptionsBuilder<OrderMonitorDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _dbContext = new OrderMonitorDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        await SeedPerformanceDataAsync();
        _repository = new EfCoreOrderRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
        await _container.DisposeAsync();
    }

    private async Task SeedPerformanceDataAsync()
    {
        // Seed reference data
        var statuses = new[]
        {
            new TrackingStatusEntity { TrackingStatusId = 3001, TrackingStatusName = "Initialized" },
            new TrackingStatusEntity { TrackingStatusId = 3050, TrackingStatusName = "PreparationStarted" },
            new TrackingStatusEntity { TrackingStatusId = 3060, TrackingStatusName = "PreparationDone" },
            new TrackingStatusEntity { TrackingStatusId = 3910, TrackingStatusName = "ReadyForFacility" },
            new TrackingStatusEntity { TrackingStatusId = 4001, TrackingStatusName = "SentToFacility" },
            new TrackingStatusEntity { TrackingStatusId = 4200, TrackingStatusName = "PrintedInFacility" },
            new TrackingStatusEntity { TrackingStatusId = 5830, TrackingStatusName = "Shipped" },
            new TrackingStatusEntity { TrackingStatusId = 6400, TrackingStatusName = "Completed" }
        };
        _dbContext.TrackingStatuses.AddRange(statuses);

        var productTypes = new[]
        {
            new MajorProductTypeEntity { MProductTypeId = 1, MajorProductTypeName = "Photo Book" },
            new MajorProductTypeEntity { MProductTypeId = 2, MajorProductTypeName = "Calendar" },
            new MajorProductTypeEntity { MProductTypeId = 3, MajorProductTypeName = "Canvas" },
            new MajorProductTypeEntity { MProductTypeId = 4, MajorProductTypeName = "Mug" }
        };
        _dbContext.MajorProductTypes.AddRange(productTypes);

        var specs = new[]
        {
            new SnSpecificationEntity { SnId = 100, MasterProductTypeId = 1 },
            new SnSpecificationEntity { SnId = 200, MasterProductTypeId = 2 },
            new SnSpecificationEntity { SnId = 300, MasterProductTypeId = 3 },
            new SnSpecificationEntity { SnId = 400, MasterProductTypeId = 4 }
        };
        _dbContext.SnSpecifications.AddRange(specs);

        _dbContext.Partners.AddRange(
            new PartnerEntity { PartnerId = 10, PartnerDisplayName = "Facility Alpha", IsActive = true },
            new PartnerEntity { PartnerId = 20, PartnerDisplayName = "Facility Beta", IsActive = true },
            new PartnerEntity { PartnerId = 30, PartnerDisplayName = "Facility Gamma", IsActive = false }
        );

        await _dbContext.SaveChangesAsync();

        // Seed orders with mixed statuses
        var statusIds = new[] { 3050, 3060, 4001, 4200, 6400 };
        var specIds = new[] { 100, 200, 300, 400 };
        var partnerIds = new[] { 10, 20 };
        var websites = new[] { "US", "UK", "DE", "FR", "AU" };
        var random = new Random(42); // deterministic

        long optId = 1;
        for (var i = 0; i < SeedOrderCount; i++)
        {
            var coNumber = $"CO{i:D5}";
            _dbContext.ConsolidationOrders.Add(new ConsolidationOrderEntity
            {
                CONumber = coNumber,
                OrderNumber = $"ORD{i:D5}",
                WebsiteCode = websites[random.Next(websites.Length)]
            });

            // Add 1-3 tracking entries per order
            var entryCount = random.Next(1, 4);
            for (var j = 0; j < entryCount; j++)
            {
                var statusId = statusIds[random.Next(statusIds.Length)];
                var hoursAgo = random.Next(1, 200);
                _dbContext.OrderProductTrackings.Add(new OrderProductTrackingEntity
                {
                    Id = optId++,
                    CONumber = coNumber,
                    Status = statusId,
                    LastUpdatedDate = DateTime.UtcNow.AddHours(-hoursAgo),
                    IsPrimaryComponent = j == 0, // first entry is primary
                    OptSnSpId = specIds[random.Next(specIds.Length)],
                    TPartnerCode = partnerIds[random.Next(partnerIds.Length)],
                    OrderDate = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                });
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetStuckOrders_Performance_WithinThreshold()
    {
        var sw = Stopwatch.StartNew();
        var result = await _repository.GetStuckOrdersAsync(new StuckOrderQueryParams { Limit = 50 });
        sw.Stop();

        var orders = result.ToList();
        orders.Should().NotBeEmpty("there should be stuck orders in the seeded data");
        sw.ElapsedMilliseconds.Should().BeLessThan(MaxQueryTimeMs,
            $"GetStuckOrders with {SeedOrderCount} orders should complete within {MaxQueryTimeMs}ms");
    }

    [Fact]
    public async Task GetStuckOrdersCount_Performance_WithinThreshold()
    {
        var sw = Stopwatch.StartNew();
        var count = await _repository.GetStuckOrdersCountAsync();
        sw.Stop();

        count.Should().BeGreaterThan(0);
        sw.ElapsedMilliseconds.Should().BeLessThan(MaxQueryTimeMs,
            $"GetStuckOrdersCount with {SeedOrderCount} orders should complete within {MaxQueryTimeMs}ms");
    }

    [Fact]
    public async Task GetOrderStatusHistory_Performance_WithinThreshold()
    {
        var sw = Stopwatch.StartNew();
        var result = await _repository.GetOrderStatusHistoryAsync("CO00001");
        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(MaxQueryTimeMs,
            "GetOrderStatusHistory for a single order should complete within threshold");
    }

    [Fact]
    public async Task GetStuckOrders_WithFilters_Performance_WithinThreshold()
    {
        var sw = Stopwatch.StartNew();
        var result = await _repository.GetStuckOrdersAsync(
            new StuckOrderQueryParams { StatusId = 3050, Limit = 20 });
        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(MaxQueryTimeMs,
            "Filtered GetStuckOrders should complete within threshold");
    }

    [Fact]
    public async Task GetStuckOrders_Pagination_Performance_WithinThreshold()
    {
        var sw = Stopwatch.StartNew();
        var result = await _repository.GetStuckOrdersAsync(
            new StuckOrderQueryParams { Limit = 10, Offset = 20 });
        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(MaxQueryTimeMs,
            "Paginated GetStuckOrders should complete within threshold");
    }
}
