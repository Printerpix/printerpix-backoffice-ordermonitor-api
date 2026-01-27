namespace OrderMonitor.Core.Models;

/// <summary>
/// Data transfer object for a stuck order.
/// </summary>
public class StuckOrderDto
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public DateTime StuckSince { get; set; }
    public int HoursStuck { get; set; }
    public int ThresholdHours { get; set; }
    public string? Region { get; set; }
    public string? CustomerEmail { get; set; }
    public string? FacilityCode { get; set; }
    public string? FacilityName { get; set; }
}
