namespace OrderMonitor.Core.Models;

/// <summary>
/// Data transfer object for order status history entry.
/// </summary>
public class OrderStatusHistoryDto
{
    public int StatusId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Duration { get; set; } = string.Empty;
    public bool IsStuck { get; set; }
}

/// <summary>
/// Response model for order status history.
/// </summary>
public class OrderStatusHistoryResponse
{
    public string OrderId { get; set; } = string.Empty;
    public IEnumerable<OrderStatusHistoryDto> History { get; set; } = [];
}
