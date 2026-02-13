namespace OrderMonitor.Core.Interfaces;

/// <summary>
/// Service interface for database diagnostics and schema discovery.
/// </summary>
public interface IDiagnosticsService
{
    /// <summary>
    /// Gets table names matching a pattern.
    /// </summary>
    Task<IEnumerable<string>> GetTablesAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets column information for a specific table.
    /// </summary>
    Task<IEnumerable<ColumnInfo>> GetColumnsAsync(string tableName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a read-only query and returns results.
    /// </summary>
    Task<IEnumerable<IDictionary<string, object?>>> ExecuteQueryAsync(string sql, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents column metadata for diagnostics.
/// </summary>
public class ColumnInfo
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string IsNullable { get; set; } = string.Empty;
    public int? CharacterMaximumLength { get; set; }
}
