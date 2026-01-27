using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OrderMonitor.Core.Configuration;
using OrderMonitor.Core.Models;
using OrderMonitor.Infrastructure.Services;

namespace OrderMonitor.UnitTests.Services;

public class AlertServiceTests
{
    private readonly Mock<ILogger<AlertService>> _loggerMock;
    private readonly SmtpSettings _smtpSettings;
    private readonly AlertSettings _alertSettings;

    public AlertServiceTests()
    {
        _loggerMock = new Mock<ILogger<AlertService>>();
        _smtpSettings = new SmtpSettings
        {
            Host = "smtp.test.com",
            Port = 587,
            Username = "test@test.com",
            Password = "testpassword",
            FromEmail = "test@test.com",
            UseSsl = true
        };
        _alertSettings = new AlertSettings
        {
            Enabled = true,
            Recipients = new List<string> { "recipient@test.com" },
            SubjectPrefix = "[Test]"
        };
    }

    private AlertService CreateService(SmtpSettings? smtp = null, AlertSettings? alerts = null)
    {
        return new AlertService(
            Options.Create(smtp ?? _smtpSettings),
            Options.Create(alerts ?? _alertSettings),
            _loggerMock.Object);
    }

    [Fact]
    public async Task SendStuckOrdersAlertAsync_WhenAlertsDisabled_DoesNotSendEmail()
    {
        // Arrange
        var disabledAlerts = new AlertSettings { Enabled = false };
        var service = CreateService(alerts: disabledAlerts);

        var summary = new StuckOrdersSummary
        {
            TotalStuckOrders = 10,
            ByThreshold = new Dictionary<string, int>(),
            ByStatusCategory = new Dictionary<string, int>(),
            TopStatuses = new List<StatusCount>(),
            GeneratedAt = DateTime.UtcNow
        };

        // Act - should not throw
        await service.SendStuckOrdersAlertAsync(summary, new List<StuckOrderDto>());

        // Assert - verify debug log about disabled alerts
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("disabled")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendStuckOrdersAlertAsync_WhenNoRecipients_LogsWarning()
    {
        // Arrange
        var noRecipientsAlerts = new AlertSettings { Enabled = true, Recipients = new List<string>() };
        var service = CreateService(alerts: noRecipientsAlerts);

        var summary = new StuckOrdersSummary
        {
            TotalStuckOrders = 10,
            ByThreshold = new Dictionary<string, int>(),
            ByStatusCategory = new Dictionary<string, int>(),
            TopStatuses = new List<StatusCount>(),
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        await service.SendStuckOrdersAlertAsync(summary, new List<StuckOrderDto>());

        // Assert - verify warning log about no recipients
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No alert recipients")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNullSmtpSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new AlertService(
            null!,
            Options.Create(_alertSettings),
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("smtpSettings");
    }

    [Fact]
    public void Constructor_WithNullAlertSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new AlertService(
            Options.Create(_smtpSettings),
            null!,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("alertSettings");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new AlertService(
            Options.Create(_smtpSettings),
            Options.Create(_alertSettings),
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }
}
