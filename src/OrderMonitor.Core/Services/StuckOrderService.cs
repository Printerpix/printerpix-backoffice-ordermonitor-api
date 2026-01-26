using OrderMonitor.Core.Configuration;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;

namespace OrderMonitor.Core.Services;

/// <summary>
/// Service for detecting and managing stuck orders.
/// </summary>
public class StuckOrderService : IStuckOrderService
{
    private readonly IOrderRepository _orderRepository;

    public StuckOrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    /// <inheritdoc />
    public async Task<StuckOrdersResponse> GetStuckOrdersAsync(
        StuckOrderQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var stuckOrders = await _orderRepository.GetStuckOrdersAsync(queryParams, cancellationToken);
        var ordersList = stuckOrders.ToList();

        return new StuckOrdersResponse
        {
            Total = ordersList.Count,
            Items = ordersList,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<StuckOrdersSummary> GetStuckOrdersSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalCount = await _orderRepository.GetStuckOrdersCountAsync(cancellationToken);

        // Get all stuck orders for grouping (with high limit for summary)
        var allStuckOrders = await _orderRepository.GetStuckOrdersAsync(
            new StuckOrderQueryParams { Limit = 10000, Offset = 0 },
            cancellationToken);

        var ordersList = allStuckOrders.ToList();

        // Group by threshold type
        var byThreshold = new Dictionary<string, int>
        {
            ["PrepStatuses (6h)"] = ordersList.Count(o => OrderStatusConfiguration.IsPrepStatus(o.StatusId)),
            ["FacilityStatuses (48h)"] = ordersList.Count(o => OrderStatusConfiguration.IsFacilityStatus(o.StatusId))
        };

        // Group by status category
        var categories = OrderStatusConfiguration.GetStatusCategories();
        var byCategory = new Dictionary<string, int>();
        foreach (var category in categories)
        {
            var categoryStatusIds = category.Value.Select(s => s.StatusId).ToHashSet();
            var count = ordersList.Count(o => categoryStatusIds.Contains(o.StatusId));
            if (count > 0)
            {
                byCategory[category.Key] = count;
            }
        }

        // Top statuses
        var topStatuses = ordersList
            .GroupBy(o => new { o.StatusId, o.Status })
            .Select(g => new StatusCount
            {
                StatusId = g.Key.StatusId,
                Status = g.Key.Status,
                Count = g.Count()
            })
            .OrderByDescending(s => s.Count)
            .Take(10)
            .ToList();

        return new StuckOrdersSummary
        {
            TotalStuckOrders = totalCount,
            ByThreshold = byThreshold,
            ByStatusCategory = byCategory,
            TopStatuses = topStatuses,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<OrderStatusHistoryResponse> GetOrderStatusHistoryAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        var history = await _orderRepository.GetOrderStatusHistoryAsync(orderId, cancellationToken);

        return new OrderStatusHistoryResponse
        {
            OrderId = orderId,
            History = history
        };
    }

    /// <inheritdoc />
    public bool IsOrderStuck(int statusId, DateTime statusUpdatedAt)
    {
        var thresholdHours = OrderStatusConfiguration.GetThresholdHours(statusId);
        var hoursInStatus = (DateTime.UtcNow - statusUpdatedAt).TotalHours;
        return hoursInStatus > thresholdHours;
    }
}
