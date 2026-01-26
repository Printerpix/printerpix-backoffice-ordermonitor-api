using Microsoft.AspNetCore.Mvc;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;

namespace OrderMonitor.Api.Controllers;

/// <summary>
/// Controller for order monitoring endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IStuckOrderService _stuckOrderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IStuckOrderService stuckOrderService, ILogger<OrdersController> logger)
    {
        _stuckOrderService = stuckOrderService ?? throw new ArgumentNullException(nameof(stuckOrderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all orders currently stuck beyond their threshold.
    /// </summary>
    /// <param name="statusId">Filter by specific status ID</param>
    /// <param name="status">Filter by status name (partial match)</param>
    /// <param name="minHours">Minimum hours stuck</param>
    /// <param name="maxHours">Maximum hours stuck</param>
    /// <param name="limit">Maximum results to return (default: 100)</param>
    /// <param name="offset">Pagination offset (default: 0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of stuck orders with pagination info</returns>
    /// <response code="200">Returns the list of stuck orders</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("stuck")]
    [ProducesResponseType(typeof(StuckOrdersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StuckOrdersResponse>> GetStuckOrders(
        [FromQuery] int? statusId = null,
        [FromQuery] string? status = null,
        [FromQuery] int? minHours = null,
        [FromQuery] int? maxHours = null,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Getting stuck orders with filters: StatusId={StatusId}, Status={Status}, MinHours={MinHours}, MaxHours={MaxHours}, Limit={Limit}, Offset={Offset}",
                statusId, status, minHours, maxHours, limit, offset);

            var queryParams = new StuckOrderQueryParams
            {
                StatusId = statusId,
                Status = status,
                MinHours = minHours,
                MaxHours = maxHours,
                Limit = limit,
                Offset = offset
            };

            var response = await _stuckOrderService.GetStuckOrdersAsync(queryParams, cancellationToken);

            _logger.LogInformation("Found {Total} stuck orders", response.Total);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stuck orders");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while retrieving stuck orders" });
        }
    }
}
