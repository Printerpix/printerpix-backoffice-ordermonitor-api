using OrderMonitor.Core.Models;

namespace OrderMonitor.Core.Interfaces;

/// <summary>
/// Service interface for stuck order detection.
/// </summary>
public interface IStuckOrderService
{
    /// <summary>
    /// Gets all stuck orders based on query parameters.
    /// </summary>
    Task<StuckOrdersResponse> GetStuckOrdersAsync(StuckOrderQueryParams queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets summary statistics of stuck orders.
    /// </summary>
    Task<StuckOrdersSummary> GetStuckOrdersSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status history for a specific order.
    /// </summary>
    Task<OrderStatusHistoryResponse> GetOrderStatusHistoryAsync(string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an order is stuck based on its status and time in status.
    /// </summary>
    bool IsOrderStuck(int statusId, DateTime statusUpdatedAt);
}
