namespace OrderMonitor.Core.Configuration;

/// <summary>
/// Business hours and holiday configuration settings.
/// </summary>
public class BusinessHoursSettings
{
    public const string SectionName = "BusinessHours";

    /// <summary>
    /// IANA timezone identifier (e.g., "Europe/London").
    /// </summary>
    public string Timezone { get; set; } = "Europe/London";

    /// <summary>
    /// Business day start hour (24h format).
    /// </summary>
    public int StartHour { get; set; } = 0;

    /// <summary>
    /// Business day end hour (24h format). 0 means full 24-hour days.
    /// </summary>
    public int EndHour { get; set; } = 0;

    /// <summary>
    /// Comma-separated list of holiday dates in yyyy-MM-dd format.
    /// </summary>
    public string Holidays { get; set; } = string.Empty;

    /// <summary>
    /// Parses the Holidays string into a list of DateTime values.
    /// </summary>
    public IEnumerable<DateTime> GetHolidayDates()
    {
        if (string.IsNullOrWhiteSpace(Holidays))
            return Enumerable.Empty<DateTime>();

        return Holidays
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => DateTime.TryParse(s, out var date) ? date : (DateTime?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value.Date);
    }
}
