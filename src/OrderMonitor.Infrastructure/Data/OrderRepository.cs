using System.Text;
using Dapper;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;

namespace OrderMonitor.Infrastructure.Data;

/// <summary>
/// Repository implementation for order data access using Dapper.
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
                o.CONumber AS OrderId,
                o.OrderNumber,
                o.StatusId,
                s.StatusName AS Status,
                o.ProductType,
                o.StatusUpdatedAt AS StuckSince,
                DATEDIFF(HOUR, o.StatusUpdatedAt, GETUTCDATE()) AS HoursStuck,
                CASE
                    WHEN o.StatusId BETWEEN 3001 AND 3910 THEN 6
                    WHEN o.StatusId BETWEEN 4001 AND 5830 THEN 48
                    ELSE 24
                END AS ThresholdHours,
                o.Region,
                o.CustomerEmail
            FROM Orders o
            INNER JOIN OrderStatuses s ON o.StatusId = s.StatusId
            WHERE
                (
                    (o.StatusId BETWEEN 3001 AND 3910
                     AND DATEDIFF(HOUR, o.StatusUpdatedAt, GETUTCDATE()) > 6)
                    OR
                    (o.StatusId BETWEEN 4001 AND 5830
                     AND DATEDIFF(HOUR, o.StatusUpdatedAt, GETUTCDATE()) > 48)
                )";

        var sqlBuilder = new StringBuilder(baseSql);
        var parameters = new DynamicParameters();

        // Apply optional filters
        if (queryParams.StatusId.HasValue)
        {
            sqlBuilder.Append(" AND o.StatusId = @StatusId");
            parameters.Add("StatusId", queryParams.StatusId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Status))
        {
            sqlBuilder.Append(" AND s.StatusName LIKE @Status");
            parameters.Add("Status", $"%{queryParams.Status}%");
        }

        if (queryParams.MinHours.HasValue)
        {
            sqlBuilder.Append(" AND DATEDIFF(HOUR, o.StatusUpdatedAt, GETUTCDATE()) >= @MinHours");
            parameters.Add("MinHours", queryParams.MinHours.Value);
        }

        if (queryParams.MaxHours.HasValue)
        {
            sqlBuilder.Append(" AND DATEDIFF(HOUR, o.StatusUpdatedAt, GETUTCDATE()) <= @MaxHours");
            parameters.Add("MaxHours", queryParams.MaxHours.Value);
        }

        // Order by hours stuck descending (oldest first)
        sqlBuilder.Append(" ORDER BY DATEDIFF(HOUR, o.StatusUpdatedAt, GETUTCDATE()) DESC");

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
            FROM Orders o
            WHERE
                (
                    (o.StatusId BETWEEN 3001 AND 3910
                     AND DATEDIFF(HOUR, o.StatusUpdatedAt, GETUTCDATE()) > 6)
                    OR
                    (o.StatusId BETWEEN 4001 AND 5830
                     AND DATEDIFF(HOUR, o.StatusUpdatedAt, GETUTCDATE()) > 48)
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
                    sh.StatusId,
                    s.StatusName AS Status,
                    sh.Timestamp,
                    LEAD(sh.Timestamp) OVER (ORDER BY sh.Timestamp) AS NextTimestamp
                FROM OrderStatusHistory sh
                INNER JOIN OrderStatuses s ON sh.StatusId = s.StatusId
                WHERE sh.OrderId = @OrderId
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
