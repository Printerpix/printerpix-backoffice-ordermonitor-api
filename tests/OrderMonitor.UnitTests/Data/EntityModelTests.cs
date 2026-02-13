using FluentAssertions;
using OrderMonitor.Infrastructure.Data.Entities;

namespace OrderMonitor.UnitTests.Data;

public class EntityModelTests
{
    [Fact]
    public void ConsolidationOrderEntity_DefaultValues()
    {
        var entity = new ConsolidationOrderEntity();
        entity.CONumber.Should().Be(string.Empty);
        entity.OrderNumber.Should().BeNull();
        entity.WebsiteCode.Should().Be(0);
        entity.OrderProductTrackings.Should().BeEmpty();
    }

    [Fact]
    public void OrderProductTrackingEntity_DefaultValues()
    {
        var entity = new OrderProductTrackingEntity();
        entity.Id.Should().Be(0);
        entity.CONumber.Should().Be(string.Empty);
        entity.Status.Should().Be(0);
        entity.LastUpdatedDate.Should().BeNull();
        entity.IsPrimaryComponent.Should().BeFalse();
        entity.TPartnerCode.Should().Be(0);
        entity.OptSnSpId.Should().BeNull();
        entity.OrderDate.Should().BeNull();
    }

    [Fact]
    public void TrackingStatusEntity_DefaultValues()
    {
        var entity = new TrackingStatusEntity();
        entity.TrackingStatusId.Should().Be(0);
        entity.TrackingStatusName.Should().BeNull();
    }

    [Fact]
    public void SnSpecificationEntity_DefaultValues()
    {
        var entity = new SnSpecificationEntity();
        entity.SnId.Should().Be(0);
        entity.MasterProductTypeId.Should().BeNull();
        entity.MajorProductType.Should().BeNull();
    }

    [Fact]
    public void MajorProductTypeEntity_DefaultValues()
    {
        var entity = new MajorProductTypeEntity();
        entity.MProductTypeId.Should().Be(0);
        entity.MajorProductTypeName.Should().BeNull();
    }

    [Fact]
    public void PartnerEntity_DefaultValues()
    {
        var entity = new PartnerEntity();
        entity.PartnerId.Should().Be(0);
        entity.PartnerDisplayName.Should().BeNull();
        entity.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ConsolidationOrderEntity_CanSetProperties()
    {
        var entity = new ConsolidationOrderEntity
        {
            CONumber = "CO12345",
            OrderNumber = "12345",
            WebsiteCode = 1
        };

        entity.CONumber.Should().Be("CO12345");
        entity.OrderNumber.Should().Be("12345");
        entity.WebsiteCode.Should().Be(1);
    }

    [Fact]
    public void OrderProductTrackingEntity_CanSetAllProperties()
    {
        var now = DateTime.UtcNow;
        var entity = new OrderProductTrackingEntity
        {
            Id = 42,
            CONumber = "CO99999",
            Status = 3050,
            LastUpdatedDate = now,
            IsPrimaryComponent = true,
            TPartnerCode = 10,
            OptSnSpId = 100,
            OrderDate = now.AddDays(-1)
        };

        entity.Id.Should().Be(42);
        entity.CONumber.Should().Be("CO99999");
        entity.Status.Should().Be(3050);
        entity.LastUpdatedDate.Should().Be(now);
        entity.IsPrimaryComponent.Should().BeTrue();
        entity.TPartnerCode.Should().Be(10);
        entity.OptSnSpId.Should().Be(100);
        entity.OrderDate.Should().Be(now.AddDays(-1));
    }

    [Fact]
    public void PartnerEntity_CanSetProperties()
    {
        var entity = new PartnerEntity
        {
            PartnerId = 99,
            PartnerDisplayName = "Test Partner",
            IsActive = true
        };

        entity.PartnerId.Should().Be(99);
        entity.PartnerDisplayName.Should().Be("Test Partner");
        entity.IsActive.Should().BeTrue();
    }
}
