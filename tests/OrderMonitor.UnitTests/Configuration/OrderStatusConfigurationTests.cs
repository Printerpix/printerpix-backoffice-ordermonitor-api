using FluentAssertions;
using OrderMonitor.Core.Configuration;

namespace OrderMonitor.UnitTests.Configuration;

public class OrderStatusConfigurationTests
{
    [Fact]
    public void GetAllStatuses_ReturnsAllConfiguredStatuses()
    {
        // Act
        var statuses = OrderStatusConfiguration.GetAllStatuses();

        // Assert
        // 97 prep statuses + 75 facility statuses = 172 total
        statuses.Should().HaveCount(172);
    }

    [Fact]
    public void GetAllStatuses_AllStatusesHaveValidId()
    {
        // Act
        var statuses = OrderStatusConfiguration.GetAllStatuses();

        // Assert
        statuses.Should().AllSatisfy(s => s.StatusId.Should().BeGreaterThan(0));
    }

    [Fact]
    public void GetAllStatuses_AllStatusesHaveNonEmptyName()
    {
        // Act
        var statuses = OrderStatusConfiguration.GetAllStatuses();

        // Assert
        statuses.Should().AllSatisfy(s => s.StatusName.Should().NotBeNullOrWhiteSpace());
    }

    [Theory]
    [InlineData(3001, "Initialized_New", 6)]
    [InlineData(3060, "PreparationDone", 6)]
    [InlineData(3720, "PrintBoxAlert_RenderStatusFailure", 6)]
    [InlineData(3910, "QualityIssueNeedCancellation", 6)]
    [InlineData(4001, "SentToFacility", 48)]
    [InlineData(4800, "ErrorInFacility", 48)]
    [InlineData(5830, "Shipping Voided", 48)]
    public void GetStatusById_ReturnsCorrectStatus(int statusId, string expectedName, int expectedThreshold)
    {
        // Act
        var status = OrderStatusConfiguration.GetStatusById(statusId);

        // Assert
        status.Should().NotBeNull();
        status!.StatusId.Should().Be(statusId);
        status.StatusName.Should().Be(expectedName);
        status.ThresholdHours.Should().Be(expectedThreshold);
    }

    [Fact]
    public void GetStatusById_WithInvalidId_ReturnsNull()
    {
        // Act
        var status = OrderStatusConfiguration.GetStatusById(9999);

        // Assert
        status.Should().BeNull();
    }

    [Fact]
    public void GetPrepStatuses_ReturnsOnlyPrepStatuses()
    {
        // Act
        var statuses = OrderStatusConfiguration.GetPrepStatuses();

        // Assert
        statuses.Should().AllSatisfy(s =>
        {
            s.StatusId.Should().BeInRange(3001, 3910);
            s.ThresholdHours.Should().Be(6);
        });
    }

    [Fact]
    public void GetFacilityStatuses_ReturnsOnlyFacilityStatuses()
    {
        // Act
        var statuses = OrderStatusConfiguration.GetFacilityStatuses();

        // Assert
        statuses.Should().AllSatisfy(s =>
        {
            s.StatusId.Should().BeInRange(4001, 5830);
            s.ThresholdHours.Should().Be(48);
        });
    }

    [Theory]
    [InlineData(3001, 6)]
    [InlineData(3050, 6)]
    [InlineData(3910, 6)]
    [InlineData(4001, 48)]
    [InlineData(4800, 48)]
    [InlineData(5830, 48)]
    public void GetThresholdHours_ReturnsCorrectThreshold(int statusId, int expectedThreshold)
    {
        // Act
        var threshold = OrderStatusConfiguration.GetThresholdHours(statusId);

        // Assert
        threshold.Should().Be(expectedThreshold);
    }

    [Fact]
    public void GetThresholdHours_WithUnknownStatus_ReturnsDefaultThreshold()
    {
        // Act
        var threshold = OrderStatusConfiguration.GetThresholdHours(9999);

        // Assert
        threshold.Should().Be(24); // Default threshold
    }

    [Theory]
    [InlineData(3001, true)]
    [InlineData(3910, true)]
    [InlineData(4001, false)]
    [InlineData(5830, false)]
    public void IsPrepStatus_ReturnsCorrectResult(int statusId, bool expected)
    {
        // Act
        var result = OrderStatusConfiguration.IsPrepStatus(statusId);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(3001, false)]
    [InlineData(3910, false)]
    [InlineData(4001, true)]
    [InlineData(5830, true)]
    public void IsFacilityStatus_ReturnsCorrectResult(int statusId, bool expected)
    {
        // Act
        var result = OrderStatusConfiguration.IsFacilityStatus(statusId);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetStatusCategories_ReturnsAllCategories()
    {
        // Act
        var categories = OrderStatusConfiguration.GetStatusCategories();

        // Assert
        categories.Should().ContainKey("Preparation");
        categories.Should().ContainKey("PrintBoxAlert");
        categories.Should().ContainKey("OnHold");
        categories.Should().ContainKey("Facility");
        categories.Should().ContainKey("Shipping");
    }

    [Fact]
    public void GetStatusesByCategory_ReturnsMatchingStatuses()
    {
        // Act
        var printBoxAlerts = OrderStatusConfiguration.GetStatusesByCategory("PrintBoxAlert");

        // Assert
        printBoxAlerts.Should().NotBeEmpty();
        printBoxAlerts.Should().AllSatisfy(s =>
            s.StatusName.Should().Contain("PrintBox"));
    }

    [Fact]
    public void PrepStatusCount_Is97()
    {
        // Act
        var count = OrderStatusConfiguration.GetPrepStatuses().Count();

        // Assert
        count.Should().Be(97); // 97 prep statuses (3001-3910 range)
    }

    [Fact]
    public void FacilityStatusCount_Is75()
    {
        // Act
        var count = OrderStatusConfiguration.GetFacilityStatuses().Count();

        // Assert
        count.Should().Be(75); // 75 facility statuses (4001-5830 range)
    }
}
