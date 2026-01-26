using Microsoft.Extensions.Logging;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;

namespace OrderMonitor.Infrastructure.Services;

/// <summary>
/// Service for sending stuck order alerts.
/// Currently logs alerts; email functionality to be added in future ticket.
/// </summary>
public class AlertService : IAlertService
{
    private readonly ILogger<AlertService> _logger;

    public AlertService(ILogger<AlertService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task SendStuckOrdersAlertAsync(
        StuckOrdersSummary summary,
        IEnumerable<StuckOrderDto> topOrders,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "ALERT: {TotalStuck} stuck orders detected. PrepStatuses: {Prep}, FacilityStatuses: {Facility}",
            summary.TotalStuckOrders,
            summary.ByThreshold.GetValueOrDefault("PrepStatuses (6h)", 0),
            summary.ByThreshold.GetValueOrDefault("FacilityStatuses (48h)", 0));

        var ordersList = topOrders.ToList();
        if (ordersList.Any())
        {
            _logger.LogWarning("Top stuck orders:");
            foreach (var order in ordersList.Take(10))
            {
                _logger.LogWarning(
                    "  - Order {OrderId}: Status {Status} ({StatusId}), Stuck for {Hours:F1} hours",
                    order.OrderId,
                    order.Status,
                    order.StatusId,
                    order.HoursStuck);
            }
        }

        // TODO: Implement actual email sending in future ticket
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendTestAlertAsync(string recipientEmail, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("TEST ALERT: Would send test email to {Recipient}", recipientEmail);

        // TODO: Implement actual email sending in future ticket
        return Task.CompletedTask;
    }
}
