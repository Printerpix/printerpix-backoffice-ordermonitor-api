using Microsoft.AspNetCore.Mvc;
using OrderMonitor.Core.Interfaces;

namespace OrderMonitor.Api.Controllers;

/// <summary>
/// Controller for alert management endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(IAlertService alertService, ILogger<AlertsController> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a test alert email to verify SMTP configuration.
    /// </summary>
    /// <param name="request">Test alert request with recipient email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error message</returns>
    /// <response code="200">Test email sent successfully</response>
    /// <response code="400">Invalid request (missing email)</response>
    /// <response code="500">Failed to send email (SMTP error)</response>
    [HttpPost("test")]
    [ProducesResponseType(typeof(TestAlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TestAlertResponse>> SendTestAlert(
        [FromBody] TestAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request?.Email))
        {
            return BadRequest(new { error = "Email address is required" });
        }

        try
        {
            _logger.LogInformation("Sending test alert to {Email}", request.Email);

            await _alertService.SendTestAlertAsync(request.Email, cancellationToken);

            _logger.LogInformation("Test alert sent successfully to {Email}", request.Email);

            return Ok(new TestAlertResponse
            {
                Success = true,
                Message = $"Test alert sent to {request.Email}",
                SentAt = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("SMTP password"))
        {
            _logger.LogError(ex, "SMTP password not configured");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "SMTP password not configured. Set SMTP_PASSWORD environment variable."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test alert to {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = $"Failed to send test alert: {ex.Message}"
            });
        }
    }
}

/// <summary>
/// Request model for sending a test alert.
/// </summary>
public class TestAlertRequest
{
    /// <summary>
    /// Email address to send the test alert to.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Response model for test alert result.
/// </summary>
public class TestAlertResponse
{
    /// <summary>
    /// Whether the test alert was sent successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Status message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the alert was sent.
    /// </summary>
    public DateTime SentAt { get; set; }
}
