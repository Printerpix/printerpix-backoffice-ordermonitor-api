using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderMonitor.Infrastructure.Data;
using OrderMonitor.Infrastructure.Data.Entities;

namespace OrderMonitor.UnitTests.Data;

public class OrderMonitorDbContextTests : IDisposable
{
    private readonly OrderMonitorDbContext _dbContext;

    public OrderMonitorDbContextTests()
    {
        var options = new DbContextOptionsBuilder<OrderMonitorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new OrderMonitorDbContext(options);
    }

    [Fact]
    public void DbContext_CanBeCreated()
    {
        _dbContext.Should().NotBeNull();
    }

    [Fact]
    public void DbSets_AreAvailable()
    {
        _dbContext.ConsolidationOrders.Should().NotBeNull();
        _dbContext.OrderProductTrackings.Should().NotBeNull();
        _dbContext.TrackingStatuses.Should().NotBeNull();
        _dbContext.SnSpecifications.Should().NotBeNull();
        _dbContext.MajorProductTypes.Should().NotBeNull();
        _dbContext.Partners.Should().NotBeNull();
    }

    [Fact]
    public async Task CanInsertAndRetrieveConsolidationOrder()
    {
        var order = new ConsolidationOrderEntity
        {
            CONumber = "CO12345",
            OrderNumber = "12345",
            WebsiteCode = 1
        };

        _dbContext.ConsolidationOrders.Add(order);
        await _dbContext.SaveChangesAsync();

        var retrieved = await _dbContext.ConsolidationOrders.FindAsync("CO12345");
        retrieved.Should().NotBeNull();
        retrieved!.OrderNumber.Should().Be("12345");
        retrieved.WebsiteCode.Should().Be(1);
    }

    [Fact]
    public async Task CanInsertAndRetrieveTrackingStatus()
    {
        var status = new TrackingStatusEntity
        {
            TrackingStatusId = 3001,
            TrackingStatusName = "Initialized_New"
        };

        _dbContext.TrackingStatuses.Add(status);
        await _dbContext.SaveChangesAsync();

        var retrieved = await _dbContext.TrackingStatuses.FindAsync(3001);
        retrieved.Should().NotBeNull();
        retrieved!.TrackingStatusName.Should().Be("Initialized_New");
    }

    [Fact]
    public async Task CanInsertOrderProductTracking_WithRelations()
    {
        // Seed related entities
        _dbContext.ConsolidationOrders.Add(new ConsolidationOrderEntity
        {
            CONumber = "CO99999",
            OrderNumber = "99999"
        });
        _dbContext.TrackingStatuses.Add(new TrackingStatusEntity
        {
            TrackingStatusId = 3050,
            TrackingStatusName = "PreparationStarted"
        });
        _dbContext.MajorProductTypes.Add(new MajorProductTypeEntity
        {
            MProductTypeId = 1,
            MajorProductTypeName = "Photo Book"
        });
        _dbContext.SnSpecifications.Add(new SnSpecificationEntity
        {
            SnId = 100,
            MasterProductTypeId = 1
        });
        await _dbContext.SaveChangesAsync();

        // Insert tracking record
        var tracking = new OrderProductTrackingEntity
        {
            Id = 1,
            CONumber = "CO99999",
            Status = 3050,
            LastUpdatedDate = DateTime.UtcNow.AddHours(-10),
            IsPrimaryComponent = true,
            OptSnSpId = 100,
            OrderDate = DateTime.UtcNow.AddDays(-1)
        };

        _dbContext.OrderProductTrackings.Add(tracking);
        await _dbContext.SaveChangesAsync();

        var retrieved = await _dbContext.OrderProductTrackings
            .Include(e => e.ConsolidationOrder)
            .Include(e => e.TrackingStatus)
            .FirstOrDefaultAsync(e => e.Id == 1);

        retrieved.Should().NotBeNull();
        retrieved!.ConsolidationOrder.Should().NotBeNull();
        retrieved.ConsolidationOrder!.CONumber.Should().Be("CO99999");
        retrieved.TrackingStatus.Should().NotBeNull();
        retrieved.TrackingStatus!.TrackingStatusName.Should().Be("PreparationStarted");
    }

    [Fact]
    public async Task CanInsertPartner()
    {
        var partner = new PartnerEntity
        {
            PartnerId = 42,
            PartnerDisplayName = "Test Facility",
            IsActive = true
        };

        _dbContext.Partners.Add(partner);
        await _dbContext.SaveChangesAsync();

        var retrieved = await _dbContext.Partners.FindAsync(42);
        retrieved.Should().NotBeNull();
        retrieved!.PartnerDisplayName.Should().Be("Test Facility");
        retrieved.IsActive.Should().BeTrue();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
