using Microsoft.Extensions.Options;
using OrderMonitor.Core.Configuration;

namespace OrderMonitor.Core.Services;

/// <summary>
/// Calculates business hours excluding weekends and holidays.
/// Holidays are loaded from configuration (BusinessHours:Holidays) instead of being hardcoded.
/// </summary>
public class BusinessHoursCalculator
{
    private readonly HashSet<DateTime> _holidays;

    /// <summary>
    /// Initializes the calculator with holidays from configuration.
    /// </summary>
    public BusinessHoursCalculator(IOptions<BusinessHoursSettings> settings)
        : this(settings.Value.GetHolidayDates())
    {
    }

    /// <summary>
    /// Initializes the calculator with an explicit list of holiday dates.
    /// </summary>
    public BusinessHoursCalculator(IEnumerable<DateTime>? holidays = null)
    {
        _holidays = holidays?.Select(h => h.Date).ToHashSet() ?? new HashSet<DateTime>();
    }

    /// <summary>
    /// Calculates business hours between two dates, excluding weekends and holidays.
    /// </summary>
    public int CalculateBusinessHours(DateTime startDate, DateTime? endDate = null)
    {
        var end = endDate ?? DateTime.UtcNow;

        if (startDate >= end)
            return 0;

        int businessHours = 0;
        var current = startDate;

        while (current < end)
        {
            if (IsBusinessDay(current))
            {
                var dayStart = current.Date;
                var dayEnd = dayStart.AddDays(1);

                var effectiveStart = current > dayStart ? current : dayStart;
                var effectiveEnd = end < dayEnd ? end : dayEnd;

                if (effectiveEnd > effectiveStart)
                {
                    businessHours += (int)(effectiveEnd - effectiveStart).TotalHours;
                }
            }

            current = current.Date.AddDays(1);
        }

        return businessHours;
    }

    /// <summary>
    /// Checks if a date is a business day (not weekend, not holiday).
    /// </summary>
    public bool IsBusinessDay(DateTime date)
    {
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            return false;

        if (_holidays.Contains(date.Date))
            return false;

        return true;
    }

    /// <summary>
    /// Gets the number of weekend days between two dates.
    /// </summary>
    public int GetWeekendDays(DateTime startDate, DateTime endDate)
    {
        int weekendDays = 0;
        var current = startDate.Date;

        while (current <= endDate.Date)
        {
            if (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday)
                weekendDays++;
            current = current.AddDays(1);
        }

        return weekendDays;
    }

    /// <summary>
    /// Gets the number of holidays between two dates (excluding weekends).
    /// </summary>
    public int GetHolidayDays(DateTime startDate, DateTime endDate)
    {
        return _holidays.Count(h =>
            h >= startDate.Date &&
            h <= endDate.Date &&
            h.DayOfWeek != DayOfWeek.Saturday &&
            h.DayOfWeek != DayOfWeek.Sunday);
    }
}
