using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderMonitor.Core.Configuration;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;

namespace OrderMonitor.Infrastructure.Services;

/// <summary>
/// Background service that periodically scans for stuck orders and sends alerts.
/// </summary>
public class BackgroundScannerService : BackgroundService, IBackgroundScanner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ScannerSettings _settings;
    private readonly ILogger<BackgroundScannerService> _logger;

    public BackgroundScannerService(
        IServiceScopeFactory scopeFactory,
        IOptions<ScannerSettings> settings,
        ILogger<BackgroundScannerService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> ExecuteScanAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogDebug("Scanner is disabled, skipping scan");
            return false;
        }

        try
        {
            _logger.LogInformation("Starting stuck orders scan");

            using var scope = _scopeFactory.CreateScope();
            var stuckOrderService = scope.ServiceProvider.GetRequiredService<IStuckOrderService>();
            var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();

            // Get summary to check if there are any stuck orders
            var summary = await stuckOrderService.GetStuckOrdersSummaryAsync(cancellationToken);

            if (summary.TotalStuckOrders == 0)
            {
                _logger.LogInformation("No stuck orders found");
                return false;
            }

            _logger.LogWarning("Found {Count} stuck orders", summary.TotalStuckOrders);

            // Get top orders for the alert
            var queryParams = new StuckOrderQueryParams
            {
                Limit = _settings.BatchSize,
                Offset = 0
            };

            var stuckOrdersResponse = await stuckOrderService.GetStuckOrdersAsync(queryParams, cancellationToken);

            // Send alert
            await alertService.SendStuckOrdersAlertAsync(summary, stuckOrdersResponse.Items, cancellationToken);

            _logger.LogInformation("Sent alert for {Count} stuck orders", summary.TotalStuckOrders);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during stuck orders scan");
            return false;
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background scanner starting with interval of {Interval} minutes", _settings.IntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecuteScanAsync(stoppingToken);

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_settings.IntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
        }

        _logger.LogInformation("Background scanner stopped");
    }
}
