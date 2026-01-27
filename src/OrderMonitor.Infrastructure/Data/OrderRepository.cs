using System.Text;
using Dapper;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;

namespace OrderMonitor.Infrastructure.Data;

/// <summary>
/// Repository implementation for order data access using Dapper.
/// Queries ConsolidationOrder and OrderProductTracking tables.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrderRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StuckOrderDto>> GetStuckOrdersAsync(
        StuckOrderQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        const string baseSql = @"
            SELECT
                co.CONumber AS OrderId,
                co.orderNumber AS OrderNumber,
                opt.Status AS StatusId,
                st.Tracking_Status_Name AS Status,
                mt.MajorProductTypeName AS ProductType,
                opt.lastUpdatedDate AS StuckSince,
                DATEDIFF(HOUR, opt.lastUpdatedDate, GETUTCDATE()) AS HoursStuck,
                CASE
                    WHEN opt.Status BETWEEN 3001 AND 3910 THEN 6
                    WHEN opt.Status BETWEEN 4001 AND 5830 THEN 48
                    ELSE 24
                END AS ThresholdHours,
                co.websiteCode AS Region,
                NULL AS CustomerEmail
            FROM ConsolidationOrder co (NOLOCK)
            INNER JOIN OrderProductTracking opt (NOLOCK)
                ON opt.CONumber = co.CONumber
            INNER JOIN luk_Tracking_Status st (NOLOCK)
                ON st.Tracking_Status_id = opt.Status
            INNER JOIN mas_SnSpecification sn (NOLOCK)
                ON sn.SnID = opt.OPT_SnSpId
            INNER JOIN luk_MajorProductType mt (NOLOCK)
                ON mt.MProductTypeID = sn.MasterProductTypeID
            WHERE opt.isPrimaryComponent = 1
                AND opt.OrderDate > DATEADD(YEAR, -2, GETUTCDATE())
                AND opt.Status < 6400
                AND (
                    (opt.Status BETWEEN 3001 AND 3910
                     AND DATEDIFF(HOUR, opt.lastUpdatedDate, GETUTCDATE()) > 6)
                    OR
                    (opt.Status BETWEEN 4001 AND 5830
                     AND DATEDIFF(HOUR, opt.lastUpdatedDate, GETUTCDATE()) > 48)
                )";

        var sqlBuilder = new StringBuilder(baseSql);
        var parameters = new DynamicParameters();

        // Apply optional filters
        if (queryParams.StatusId.HasValue)
        {
            sqlBuilder.Append(" AND opt.Status = @StatusId");
            parameters.Add("StatusId", queryParams.StatusId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Status))
        {
            sqlBuilder.Append(" AND st.Tracking_Status_Name LIKE @Status");
            parameters.Add("Status", $"%{queryParams.Status}%");
        }

        if (queryParams.MinHours.HasValue)
        {
            sqlBuilder.Append(" AND DATEDIFF(HOUR, opt.lastUpdatedDate, GETUTCDATE()) >= @MinHours");
            parameters.Add("MinHours", queryParams.MinHours.Value);
        }

        if (queryParams.MaxHours.HasValue)
        {
            sqlBuilder.Append(" AND DATEDIFF(HOUR, opt.lastUpdatedDate, GETUTCDATE()) <= @MaxHours");
            parameters.Add("MaxHours", queryParams.MaxHours.Value);
        }

        // Order by hours stuck descending (oldest first)
        sqlBuilder.Append(" ORDER BY DATEDIFF(HOUR, opt.lastUpdatedDate, GETUTCDATE()) DESC");

        // Apply pagination
        sqlBuilder.Append(" OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY");
        parameters.Add("Offset", queryParams.Offset);
        parameters.Add("Limit", queryParams.Limit);

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<StuckOrderDto>(
            new CommandDefinition(sqlBuilder.ToString(), parameters, cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<int> GetStuckOrdersCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM ConsolidationOrder co (NOLOCK)
            INNER JOIN OrderProductTracking opt (NOLOCK)
                ON opt.CONumber = co.CONumber
            WHERE opt.isPrimaryComponent = 1
                AND opt.OrderDate > DATEADD(YEAR, -2, GETUTCDATE())
                AND opt.Status < 6400
                AND (
                    (opt.Status BETWEEN 3001 AND 3910
                     AND DATEDIFF(HOUR, opt.lastUpdatedDate, GETUTCDATE()) > 6)
                    OR
                    (opt.Status BETWEEN 4001 AND 5830
                     AND DATEDIFF(HOUR, opt.lastUpdatedDate, GETUTCDATE()) > 48)
                )";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrderStatusHistoryDto>> GetOrderStatusHistoryAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            WITH StatusDurations AS (
                SELECT
                    opt.Status AS StatusId,
                    st.Tracking_Status_Name AS Status,
                    opt.lastUpdatedDate AS Timestamp,
                    LEAD(opt.lastUpdatedDate) OVER (ORDER BY opt.lastUpdatedDate) AS NextTimestamp
                FROM OrderProductTracking opt (NOLOCK)
                INNER JOIN luk_Tracking_Status st (NOLOCK)
                    ON st.Tracking_Status_id = opt.Status
                WHERE opt.CONumber = @OrderId
                    AND opt.isPrimaryComponent = 1
            )
            SELECT
                StatusId,
                Status,
                Timestamp,
                CASE
                    WHEN NextTimestamp IS NULL THEN
                        CASE
                            WHEN (StatusId BETWEEN 3001 AND 3910 AND DATEDIFF(HOUR, Timestamp, GETUTCDATE()) > 6)
                                 OR (StatusId BETWEEN 4001 AND 5830 AND DATEDIFF(HOUR, Timestamp, GETUTCDATE()) > 48)
                            THEN CONCAT(DATEDIFF(HOUR, Timestamp, GETUTCDATE()), 'h+ (STUCK)')
                            ELSE CONCAT(DATEDIFF(HOUR, Timestamp, GETUTCDATE()), 'h (Current)')
                        END
                    ELSE
                        CASE
                            WHEN DATEDIFF(MINUTE, Timestamp, NextTimestamp) < 60
                            THEN CONCAT(DATEDIFF(MINUTE, Timestamp, NextTimestamp), 'm')
                            ELSE CONCAT(DATEDIFF(HOUR, Timestamp, NextTimestamp), 'h ',
                                        DATEDIFF(MINUTE, Timestamp, NextTimestamp) % 60, 'm')
                        END
                END AS Duration,
                CASE
                    WHEN NextTimestamp IS NULL AND
                        ((StatusId BETWEEN 3001 AND 3910 AND DATEDIFF(HOUR, Timestamp, GETUTCDATE()) > 6)
                         OR (StatusId BETWEEN 4001 AND 5830 AND DATEDIFF(HOUR, Timestamp, GETUTCDATE()) > 48))
                    THEN 1
                    ELSE 0
                END AS IsStuck
            FROM StatusDurations
            ORDER BY Timestamp ASC";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<OrderStatusHistoryDto>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken));
    }
}
