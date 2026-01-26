namespace OrderMonitor.Core.Configuration;

/// <summary>
/// Configuration settings for the background scanner.
/// </summary>
public class ScannerSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Scanner";

    /// <summary>
    /// Whether the scanner is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval between scans in minutes.
    /// </summary>
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Maximum number of orders to process per scan.
    /// </summary>
    public int BatchSize { get; set; } = 1000;
}
