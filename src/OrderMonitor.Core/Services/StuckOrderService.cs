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
    private readonly BusinessHoursCalculator _businessHoursCalculator;

    public StuckOrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _businessHoursCalculator = new BusinessHoursCalculator();
    }

    /// <inheritdoc />
    public async Task<StuckOrdersResponse> GetStuckOrdersAsync(
        StuckOrderQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var stuckOrders = await _orderRepository.GetStuckOrdersAsync(queryParams, cancellationToken);
        var ordersList = stuckOrders.ToList();

        // Recalculate hours using business hours (excluding weekends and holidays)
        foreach (var order in ordersList)
        {
            order.HoursStuck = _businessHoursCalculator.CalculateBusinessHours(order.StuckSince);
        }

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

        // Recalculate hours using business hours (excluding weekends and holidays)
        foreach (var order in ordersList)
        {
            order.HoursStuck = _businessHoursCalculator.CalculateBusinessHours(order.StuckSince);
        }

        // Group by threshold type
        var byThreshold = new Dictionary<string, int>
        {
            ["PrepStatuses (6h)"] = ordersList.Count(o => OrderStatusConfiguration.IsPrepStatus(o.StatusId)),
            ["FacilityStatuses (48h)"] = ordersList.Count(o => OrderStatusConfiguration.IsFacilityStatus(o.StatusId))
        };

        // Group FacilityStatuses by Facility/Partner
        var byFacility = ordersList
            .Where(o => OrderStatusConfiguration.IsFacilityStatus(o.StatusId))
            .GroupBy(o => string.IsNullOrEmpty(o.FacilityName) ? "Unknown" : o.FacilityName)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

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

        // Group by Partner/Facility and Status
        var byPartnerStatus = ordersList
            .GroupBy(o => new
            {
                Partner = string.IsNullOrEmpty(o.FacilityName) ? "Unknown" : o.FacilityName,
                o.Status
            })
            .Select(g => new PartnerStatusCount
            {
                Partner = g.Key.Partner,
                Status = g.Key.Status,
                Count = g.Count()
            })
            .OrderBy(p => p.Partner)
            .ThenByDescending(p => p.Count)
            .ToList();

        return new StuckOrdersSummary
        {
            TotalStuckOrders = totalCount,
            ByThreshold = byThreshold,
            ByFacility = byFacility,
            ByStatusCategory = byCategory,
            TopStatuses = topStatuses,
            ByPartnerStatus = byPartnerStatus,
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
        var businessHours = _businessHoursCalculator.CalculateBusinessHours(statusUpdatedAt);
        return businessHours > thresholdHours;
    }
}
