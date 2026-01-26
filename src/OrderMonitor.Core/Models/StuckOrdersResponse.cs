namespace OrderMonitor.Core.Models;

/// <summary>
/// Response model for stuck orders query.
/// </summary>
public class StuckOrdersResponse
{
    public int Total { get; set; }
    public IEnumerable<StuckOrderDto> Items { get; set; } = [];
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
