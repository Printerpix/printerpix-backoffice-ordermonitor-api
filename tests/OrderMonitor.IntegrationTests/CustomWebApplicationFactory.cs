using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;

namespace OrderMonitor.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Replaces real services with mocks for isolated testing.
/// Provides minimal required configuration to satisfy startup validation.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IStuckOrderService> MockStuckOrderService { get; } = new();
    public Mock<IAlertService> MockAlertService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Provide minimal configuration required by ConfigurationValidator
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "sqlserver",
                ["Database:ConnectionString"] = "Server=localhost;Database=TestDb;Trusted_Connection=True;",
                ["SmtpSettings:Host"] = "localhost",
                ["SmtpSettings:Port"] = "25",
                ["Alerts:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove real service registrations
            services.RemoveAll<IStuckOrderService>();
            services.RemoveAll<IAlertService>();

            // Replace config validator with no-op for testing
            services.RemoveAll<IConfigurationValidator>();
            services.AddSingleton<IConfigurationValidator>(sp =>
                new NoOpConfigurationValidator());

            // Add mocked services
            services.AddSingleton(MockStuckOrderService.Object);
            services.AddSingleton(MockAlertService.Object);
        });
    }

    /// <summary>
    /// Resets all mocks and sets up default mock responses.
    /// Call this at the start of each test to ensure clean state.
    /// </summary>
    public void SetupDefaultMocks()
    {
        // Reset mocks for test isolation
        MockStuckOrderService.Reset();
        MockAlertService.Reset();

        // Default stuck orders response
        MockStuckOrderService
            .Setup(s => s.GetStuckOrdersAsync(It.IsAny<StuckOrderQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StuckOrdersResponse
            {
                Total = 5,
                Items = new List<StuckOrderDto>
                {
                    new() { OrderId = "CO12345", OrderNumber = "12345", StatusId = 15, Status = "Submitted", HoursStuck = 24, ThresholdHours = 6, ProductType = "Photo Book" },
                    new() { OrderId = "CO12346", OrderNumber = "12346", StatusId = 17, Status = "Pending Print", HoursStuck = 72, ThresholdHours = 48, ProductType = "Calendar" },
                    new() { OrderId = "CO12347", OrderNumber = "12347", StatusId = 15, Status = "Submitted", HoursStuck = 12, ThresholdHours = 6, ProductType = "Canvas" },
                    new() { OrderId = "CO12348", OrderNumber = "12348", StatusId = 20, Status = "In Production", HoursStuck = 96, ThresholdHours = 48, ProductType = "Mug" },
                    new() { OrderId = "CO12349", OrderNumber = "12349", StatusId = 15, Status = "Submitted", HoursStuck = 8, ThresholdHours = 6, ProductType = "Photo Book" }
                },
                GeneratedAt = DateTime.UtcNow
            });

        // Default summary response
        MockStuckOrderService
            .Setup(s => s.GetStuckOrdersSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StuckOrdersSummary
            {
                TotalStuckOrders = 150,
                ByThreshold = new Dictionary<string, int>
                {
                    { "PrepStatuses (6h)", 80 },
                    { "FacilityStatuses (48h)", 70 }
                },
                ByStatusCategory = new Dictionary<string, int>
                {
                    { "PrepStatuses", 80 },
                    { "FacilityStatuses", 70 }
                },
                TopStatuses = new List<StatusCount>
                {
                    new() { StatusId = 15, Status = "Submitted", Count = 50 },
                    new() { StatusId = 17, Status = "Pending Print", Count = 40 },
                    new() { StatusId = 20, Status = "In Production", Count = 30 }
                },
                GeneratedAt = DateTime.UtcNow
            });

        // Default status history response
        MockStuckOrderService
            .Setup(s => s.GetOrderStatusHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string orderId, CancellationToken _) => new OrderStatusHistoryResponse
            {
                OrderId = orderId,
                History = new List<OrderStatusHistoryDto>
                {
                    new() { StatusId = 10, Status = "Created", Timestamp = DateTime.UtcNow.AddDays(-3), Duration = "2 hours", IsStuck = false },
                    new() { StatusId = 15, Status = "Submitted", Timestamp = DateTime.UtcNow.AddDays(-2), Duration = "48 hours", IsStuck = true },
                    new() { StatusId = 17, Status = "Pending Print", Timestamp = DateTime.UtcNow, Duration = "Ongoing", IsStuck = false }
                }
            });

        // Default test alert - succeeds
        MockAlertService
            .Setup(s => s.SendTestAlertAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}

/// <summary>
/// No-op configuration validator for integration testing.
/// </summary>
internal class NoOpConfigurationValidator : IConfigurationValidator
{
    public void Validate() { }
}
