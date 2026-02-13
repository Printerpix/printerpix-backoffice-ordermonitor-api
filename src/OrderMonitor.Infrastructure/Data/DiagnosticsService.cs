using Microsoft.EntityFrameworkCore;
using OrderMonitor.Core.Interfaces;

namespace OrderMonitor.Infrastructure.Data;

/// <summary>
/// EF Core implementation of database diagnostics.
/// Uses raw SQL for schema introspection queries.
/// </summary>
public class DiagnosticsService : IDiagnosticsService
{
    private readonly OrderMonitorDbContext _dbContext;

    public DiagnosticsService(OrderMonitorDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetTablesAsync(
        string pattern,
        CancellationToken cancellationToken = default)
    {
        // Use INFORMATION_SCHEMA which is supported by SQL Server, MySQL, and PostgreSQL
        var tables = await _dbContext.Database
            .SqlQueryRaw<string>(
                "SELECT TABLE_NAME AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE {0} ORDER BY TABLE_NAME",
                $"%{pattern}%")
            .ToListAsync(cancellationToken);

        return tables;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ColumnInfo>> GetColumnsAsync(
        string tableName,
        CancellationToken cancellationToken = default)
    {
        var columns = await _dbContext.Database
            .SqlQueryRaw<ColumnInfo>(
                @"SELECT COLUMN_NAME AS ColumnName, DATA_TYPE AS DataType,
                         IS_NULLABLE AS IsNullable, CHARACTER_MAXIMUM_LENGTH AS CharacterMaximumLength
                  FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_NAME = {0}
                  ORDER BY ORDINAL_POSITION",
                tableName)
            .ToListAsync(cancellationToken);

        return columns;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IDictionary<string, object?>>> ExecuteQueryAsync(
        string sql,
        CancellationToken cancellationToken = default)
    {
        var results = new List<IDictionary<string, object?>>();

        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var columnNames = Enumerable.Range(0, reader.FieldCount)
                .Select(reader.GetName)
                .ToList();

            var count = 0;
            while (await reader.ReadAsync(cancellationToken) && count < 100)
            {
                var row = new Dictionary<string, object?>();
                foreach (var col in columnNames)
                {
                    var value = reader[col];
                    row[col] = value == DBNull.Value ? null : value;
                }
                results.Add(row);
                count++;
            }
        }
        finally
        {
            await connection.CloseAsync();
        }

        return results;
    }
}
