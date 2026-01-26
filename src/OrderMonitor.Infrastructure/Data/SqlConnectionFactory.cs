using System.Data;
using Microsoft.Data.SqlClient;
using OrderMonitor.Core.Interfaces;

namespace OrderMonitor.Infrastructure.Data;

/// <summary>
/// SQL Server connection factory implementation.
/// </summary>
public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
