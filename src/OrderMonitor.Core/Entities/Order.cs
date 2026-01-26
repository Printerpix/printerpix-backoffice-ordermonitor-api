namespace OrderMonitor.Core.Entities;

/// <summary>
/// Represents an order in the Backoffice system.
/// </summary>
public class Order
{
    public string CONumber { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public DateTime StatusUpdatedAt { get; set; }
    public string? Region { get; set; }
    public string? CustomerEmail { get; set; }
}
