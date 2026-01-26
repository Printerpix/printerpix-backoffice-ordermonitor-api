namespace OrderMonitor.Core.Entities;

/// <summary>
/// Represents an item within an order.
/// </summary>
public class OrderItem
{
    public int OrderItemId { get; set; }
    public string CONumber { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? ProductCode { get; set; }
    public decimal? UnitPrice { get; set; }
}
