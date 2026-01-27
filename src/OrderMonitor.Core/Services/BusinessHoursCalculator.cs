namespace OrderMonitor.Core.Services;

/// <summary>
/// Calculates business hours excluding weekends and holidays.
/// </summary>
public class BusinessHoursCalculator
{
    private readonly HashSet<DateTime> _holidays;

    /// <summary>
    /// Initializes the calculator with a list of holiday dates.
    /// </summary>
    public BusinessHoursCalculator(IEnumerable<DateTime>? holidays = null)
    {
        _holidays = holidays?.Select(h => h.Date).ToHashSet() ?? GetDefaultHolidays();
    }

    /// <summary>
    /// Calculates business hours between two dates, excluding weekends and holidays.
    /// </summary>
    /// <param name="startDate">Start date/time</param>
    /// <param name="endDate">End date/time (defaults to UTC now)</param>
    /// <returns>Number of business hours</returns>
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
                // Calculate hours for this day
                var dayStart = current.Date;
                var dayEnd = dayStart.AddDays(1);

                var effectiveStart = current > dayStart ? current : dayStart;
                var effectiveEnd = end < dayEnd ? end : dayEnd;

                if (effectiveEnd > effectiveStart)
                {
                    businessHours += (int)(effectiveEnd - effectiveStart).TotalHours;
                }
            }

            // Move to start of next day
            current = current.Date.AddDays(1);
        }

        return businessHours;
    }

    /// <summary>
    /// Checks if a date is a business day (not weekend, not holiday).
    /// </summary>
    public bool IsBusinessDay(DateTime date)
    {
        // Check weekend
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            return false;

        // Check holiday
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

    /// <summary>
    /// Default holidays for 2025-2026 (UK/EU focused for Printerpix).
    /// </summary>
    private static HashSet<DateTime> GetDefaultHolidays()
    {
        return new HashSet<DateTime>
        {
            // 2025 Holidays
            new DateTime(2025, 1, 1),   // New Year's Day
            new DateTime(2025, 4, 18),  // Good Friday
            new DateTime(2025, 4, 21),  // Easter Monday
            new DateTime(2025, 5, 5),   // Early May Bank Holiday
            new DateTime(2025, 5, 26),  // Spring Bank Holiday
            new DateTime(2025, 8, 25),  // Summer Bank Holiday
            new DateTime(2025, 12, 25), // Christmas Day
            new DateTime(2025, 12, 26), // Boxing Day

            // 2026 Holidays
            new DateTime(2026, 1, 1),   // New Year's Day
            new DateTime(2026, 4, 3),   // Good Friday
            new DateTime(2026, 4, 6),   // Easter Monday
            new DateTime(2026, 5, 4),   // Early May Bank Holiday
            new DateTime(2026, 5, 25),  // Spring Bank Holiday
            new DateTime(2026, 8, 31),  // Summer Bank Holiday
            new DateTime(2026, 12, 25), // Christmas Day
            new DateTime(2026, 12, 28), // Boxing Day (observed)

            // 2027 Holidays
            new DateTime(2027, 1, 1),   // New Year's Day
            new DateTime(2027, 3, 26),  // Good Friday
            new DateTime(2027, 3, 29),  // Easter Monday
            new DateTime(2027, 5, 3),   // Early May Bank Holiday
            new DateTime(2027, 5, 31),  // Spring Bank Holiday
            new DateTime(2027, 8, 30),  // Summer Bank Holiday
            new DateTime(2027, 12, 27), // Christmas Day (observed)
            new DateTime(2027, 12, 28), // Boxing Day (observed)
        };
    }
}
