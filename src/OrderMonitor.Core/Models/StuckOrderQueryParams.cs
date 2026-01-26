namespace OrderMonitor.Core.Models;

/// <summary>
/// Query parameters for stuck orders endpoint.
/// </summary>
public class StuckOrderQueryParams
{
    public int? StatusId { get; set; }
    public string? Status { get; set; }
    public int? MinHours { get; set; }
    public int? MaxHours { get; set; }
    public int Limit { get; set; } = 100;
    public int Offset { get; set; } = 0;
}
