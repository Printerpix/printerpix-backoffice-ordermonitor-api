namespace OrderMonitor.Core.Interfaces;

/// <summary>
/// Interface for the background scanner that periodically checks for stuck orders.
/// </summary>
public interface IBackgroundScanner
{
    /// <summary>
    /// Executes a single scan for stuck orders and sends alerts if needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if alerts were sent, false otherwise.</returns>
    Task<bool> ExecuteScanAsync(CancellationToken cancellationToken = default);
}
