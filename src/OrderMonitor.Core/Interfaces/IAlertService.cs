using OrderMonitor.Core.Models;

namespace OrderMonitor.Core.Interfaces;

/// <summary>
/// Service interface for sending alerts.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Sends an alert email with stuck orders information.
    /// </summary>
    Task SendStuckOrdersAlertAsync(StuckOrdersSummary summary, IEnumerable<StuckOrderDto> topOrders, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a test alert email.
    /// </summary>
    Task SendTestAlertAsync(string recipientEmail, CancellationToken cancellationToken = default);
}
