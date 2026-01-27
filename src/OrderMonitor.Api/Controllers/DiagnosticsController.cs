using Dapper;
using Microsoft.AspNetCore.Mvc;
using OrderMonitor.Core.Interfaces;

namespace OrderMonitor.Api.Controllers;

/// <summary>
/// Diagnostic endpoints for debugging and schema discovery.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DiagnosticsController : ControllerBase
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(IDbConnectionFactory connectionFactory, ILogger<DiagnosticsController> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets tables containing 'Order' in their name.
    /// </summary>
    [HttpGet("tables")]
    public async Task<IActionResult> GetOrderTables()
    {
        try
        {
            using var conn = _connectionFactory.CreateConnection();
            var tables = await conn.QueryAsync<string>(
                "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE '%Order%' ORDER BY TABLE_NAME");
            return Ok(tables);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets columns for a specific table.
    /// </summary>
    [HttpGet("columns/{tableName}")]
    public async Task<IActionResult> GetTableColumns(string tableName)
    {
        try
        {
            using var conn = _connectionFactory.CreateConnection();
            var columns = await conn.QueryAsync<dynamic>(
                @"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
                  FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_NAME = @TableName
                  ORDER BY ORDINAL_POSITION",
                new { TableName = tableName });
            return Ok(columns);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Runs a custom SQL query (SELECT only).
    /// </summary>
    [HttpPost("query")]
    public async Task<IActionResult> RunQuery([FromBody] QueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Sql))
            return BadRequest(new { error = "SQL query is required" });

        // Safety check - only allow SELECT
        if (!request.Sql.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only SELECT queries are allowed" });

        try
        {
            using var conn = _connectionFactory.CreateConnection();
            var results = await conn.QueryAsync<dynamic>(request.Sql);
            return Ok(results.Take(100)); // Limit to 100 rows
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class QueryRequest
{
    public string Sql { get; set; } = string.Empty;
}
