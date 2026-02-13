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
    private readonly IDiagnosticsService _diagnosticsService;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(IDiagnosticsService diagnosticsService, ILogger<DiagnosticsController> logger)
    {
        _diagnosticsService = diagnosticsService;
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
            var tables = await _diagnosticsService.GetTablesAsync("Order");
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
            var columns = await _diagnosticsService.GetColumnsAsync(tableName);
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
            var results = await _diagnosticsService.ExecuteQueryAsync(request.Sql);
            return Ok(results);
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
