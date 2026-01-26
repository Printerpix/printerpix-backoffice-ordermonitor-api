namespace OrderMonitor.Core.Entities;

/// <summary>
/// Represents an order status configuration.
/// </summary>
public class OrderStatus
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}
