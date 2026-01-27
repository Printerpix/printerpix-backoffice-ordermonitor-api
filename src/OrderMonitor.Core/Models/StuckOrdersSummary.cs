namespace OrderMonitor.Core.Models;

/// <summary>
/// Summary statistics for stuck orders.
/// </summary>
public class StuckOrdersSummary
{
    public int TotalStuckOrders { get; set; }
    public Dictionary<string, int> ByThreshold { get; set; } = new();
    public Dictionary<string, int> ByFacility { get; set; } = new();
    public Dictionary<string, int> ByStatusCategory { get; set; } = new();
    public IEnumerable<StatusCount> TopStatuses { get; set; } = [];
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Count of orders by status.
/// </summary>
public class StatusCount
{
    public int StatusId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}
