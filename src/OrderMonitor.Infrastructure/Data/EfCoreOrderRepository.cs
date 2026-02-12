using Microsoft.EntityFrameworkCore;
using OrderMonitor.Core.Configuration;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;

namespace OrderMonitor.Infrastructure.Data;

/// <summary>
/// Repository implementation for order data access using Entity Framework Core.
/// All date/time calculations are performed in C# for database-agnostic operation.
/// </summary>
public class EfCoreOrderRepository : IOrderRepository
{
    private readonly OrderMonitorDbContext _dbContext;

    public EfCoreOrderRepository(OrderMonitorDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StuckOrderDto>> GetStuckOrdersAsync(
        StuckOrderQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var twoYearsAgo = utcNow.AddYears(-2);

        // Fetch candidate orders with all related data using LINQ joins
        var query = from opt in _dbContext.OrderProductTrackings
                    join co in _dbContext.ConsolidationOrders on opt.CONumber equals co.CONumber
                    join st in _dbContext.TrackingStatuses on opt.Status equals st.TrackingStatusId
                    join sn in _dbContext.SnSpecifications on opt.OptSnSpId equals sn.SnId
                    join mt in _dbContext.MajorProductTypes on sn.MasterProductTypeId equals mt.MProductTypeId
                    join pm in _dbContext.Partners on opt.TPartnerCode equals pm.PartnerId into partners
                    from pm in partners.Where(p => p.IsActive).DefaultIfEmpty()
                    where opt.IsPrimaryComponent
                        && opt.OrderDate > twoYearsAgo
                        && opt.Status < 6400
                        && opt.LastUpdatedDate != null
                    select new
                    {
                        co.CONumber,
                        co.OrderNumber,
                        opt.Status,
                        StatusName = st.TrackingStatusName,
                        ProductType = mt.MajorProductTypeName,
                        opt.LastUpdatedDate,
                        co.WebsiteCode,
                        FacilityCode = opt.TPartnerCode,
                        FacilityName = pm != null ? pm.PartnerDisplayName : null
                    };

        // Materialize to apply C# date logic
        var rawOrders = await query.ToListAsync(cancellationToken);

        // Apply stuck threshold filtering in C# (replaces SQL DATEDIFF)
        var stuckOrders = rawOrders
            .Where(o =>
            {
                var hoursStuck = (int)(utcNow - o.LastUpdatedDate!.Value).TotalHours;
                return (o.Status >= OrderStatusConfiguration.PrepMinStatusId
                        && o.Status <= OrderStatusConfiguration.PrepMaxStatusId
                        && hoursStuck > OrderStatusConfiguration.PrepThresholdHours)
                    || (o.Status >= OrderStatusConfiguration.FacilityMinStatusId
                        && o.Status <= OrderStatusConfiguration.FacilityMaxStatusId
                        && hoursStuck > OrderStatusConfiguration.FacilityThresholdHours);
            })
            // Deduplicate by CONumber (keep latest per order) - replaces ROW_NUMBER()
            .GroupBy(o => o.CONumber)
            .Select(g => g.OrderByDescending(o => o.LastUpdatedDate).First())
            .Select(o => new StuckOrderDto
            {
                OrderId = o.CONumber,
                OrderNumber = o.OrderNumber ?? string.Empty,
                StatusId = o.Status,
                Status = o.StatusName ?? string.Empty,
                ProductType = o.ProductType ?? string.Empty,
                StuckSince = o.LastUpdatedDate!.Value,
                HoursStuck = (int)(utcNow - o.LastUpdatedDate!.Value).TotalHours,
                ThresholdHours = OrderStatusConfiguration.GetThresholdHours(o.Status),
                Region = o.WebsiteCode,
                CustomerEmail = null,
                FacilityCode = o.FacilityCode?.ToString(),
                FacilityName = o.FacilityName ?? "Unknown"
            });

        // Apply optional filters
        if (queryParams.StatusId.HasValue)
            stuckOrders = stuckOrders.Where(o => o.StatusId == queryParams.StatusId.Value);

        if (!string.IsNullOrWhiteSpace(queryParams.Status))
            stuckOrders = stuckOrders.Where(o =>
                o.Status.Contains(queryParams.Status, StringComparison.OrdinalIgnoreCase));

        if (queryParams.MinHours.HasValue)
            stuckOrders = stuckOrders.Where(o => o.HoursStuck >= queryParams.MinHours.Value);

        if (queryParams.MaxHours.HasValue)
            stuckOrders = stuckOrders.Where(o => o.HoursStuck <= queryParams.MaxHours.Value);

        // Order and paginate
        return stuckOrders
            .OrderByDescending(o => o.HoursStuck)
            .Skip(queryParams.Offset)
            .Take(queryParams.Limit)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetStuckOrdersCountAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var twoYearsAgo = utcNow.AddYears(-2);

        // Fetch candidate records
        var query = _dbContext.OrderProductTrackings
            .Where(opt => opt.IsPrimaryComponent
                && opt.OrderDate > twoYearsAgo
                && opt.Status < 6400
                && opt.LastUpdatedDate != null);

        var candidates = await query
            .Select(opt => new { opt.CONumber, opt.Status, opt.LastUpdatedDate })
            .ToListAsync(cancellationToken);

        // Apply stuck threshold filtering in C# and count distinct CONumbers
        return candidates
            .Where(o =>
            {
                var hoursStuck = (int)(utcNow - o.LastUpdatedDate!.Value).TotalHours;
                return (o.Status >= OrderStatusConfiguration.PrepMinStatusId
                        && o.Status <= OrderStatusConfiguration.PrepMaxStatusId
                        && hoursStuck > OrderStatusConfiguration.PrepThresholdHours)
                    || (o.Status >= OrderStatusConfiguration.FacilityMinStatusId
                        && o.Status <= OrderStatusConfiguration.FacilityMaxStatusId
                        && hoursStuck > OrderStatusConfiguration.FacilityThresholdHours);
            })
            .Select(o => o.CONumber)
            .Distinct()
            .Count();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrderStatusHistoryDto>> GetOrderStatusHistoryAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        // Fetch status history ordered by timestamp
        var statusEntries = await _dbContext.OrderProductTrackings
            .Where(opt => opt.CONumber == orderId && opt.IsPrimaryComponent && opt.LastUpdatedDate != null)
            .Join(_dbContext.TrackingStatuses,
                opt => opt.Status,
                st => st.TrackingStatusId,
                (opt, st) => new
                {
                    StatusId = opt.Status,
                    Status = st.TrackingStatusName ?? string.Empty,
                    Timestamp = opt.LastUpdatedDate!.Value
                })
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);

        // Apply LEAD() equivalent in C# - calculate duration between consecutive statuses
        var result = new List<OrderStatusHistoryDto>();
        for (var i = 0; i < statusEntries.Count; i++)
        {
            var entry = statusEntries[i];
            var nextTimestamp = i < statusEntries.Count - 1
                ? statusEntries[i + 1].Timestamp
                : (DateTime?)null;

            var isStuck = false;
            string duration;

            if (nextTimestamp == null)
            {
                // Last/current status - calculate from timestamp to now
                var hoursFromNow = (int)(utcNow - entry.Timestamp).TotalHours;
                var isThresholdExceeded =
                    (entry.StatusId >= OrderStatusConfiguration.PrepMinStatusId
                     && entry.StatusId <= OrderStatusConfiguration.PrepMaxStatusId
                     && hoursFromNow > OrderStatusConfiguration.PrepThresholdHours)
                    || (entry.StatusId >= OrderStatusConfiguration.FacilityMinStatusId
                        && entry.StatusId <= OrderStatusConfiguration.FacilityMaxStatusId
                        && hoursFromNow > OrderStatusConfiguration.FacilityThresholdHours);

                isStuck = isThresholdExceeded;
                duration = isThresholdExceeded
                    ? $"{hoursFromNow}h+ (STUCK)"
                    : $"{hoursFromNow}h (Current)";
            }
            else
            {
                // Calculate duration between this status and the next
                var totalMinutes = (int)(nextTimestamp.Value - entry.Timestamp).TotalMinutes;
                if (totalMinutes < 60)
                {
                    duration = $"{totalMinutes}m";
                }
                else
                {
                    var hours = totalMinutes / 60;
                    var minutes = totalMinutes % 60;
                    duration = $"{hours}h {minutes}m";
                }
            }

            result.Add(new OrderStatusHistoryDto
            {
                StatusId = entry.StatusId,
                Status = entry.Status,
                Timestamp = entry.Timestamp,
                Duration = duration,
                IsStuck = isStuck
            });
        }

        return result;
    }
}
