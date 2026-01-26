using OrderMonitor.Core.Models;

namespace OrderMonitor.Core.Interfaces;

/// <summary>
/// Repository interface for order data access.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Gets all orders that are stuck beyond their threshold.
    /// </summary>
    Task<IEnumerable<StuckOrderDto>> GetStuckOrdersAsync(StuckOrderQueryParams queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of stuck orders.
    /// </summary>
    Task<int> GetStuckOrdersCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status history for a specific order.
    /// </summary>
    Task<IEnumerable<OrderStatusHistoryDto>> GetOrderStatusHistoryAsync(string orderId, CancellationToken cancellationToken = default);
}
