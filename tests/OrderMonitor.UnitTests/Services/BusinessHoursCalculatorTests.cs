using OrderMonitor.Core.Configuration;
using OrderMonitor.Core.Services;
using Xunit;

namespace OrderMonitor.UnitTests.Services;

public class BusinessHoursCalculatorTests
{
    private readonly BusinessHoursCalculator _calculator;

    public BusinessHoursCalculatorTests()
    {
        // Use a fixed set of holidays for testing
        var holidays = new[]
        {
            new DateTime(2026, 1, 1),  // New Year's Day (Thursday)
            new DateTime(2026, 12, 25) // Christmas Day (Friday)
        };
        _calculator = new BusinessHoursCalculator(holidays);
    }

    [Fact]
    public void CalculateBusinessHours_WeekdayToWeekday_ReturnsCorrectHours()
    {
        // Monday 9am to Tuesday 9am = 24 hours
        var start = new DateTime(2026, 1, 5, 9, 0, 0); // Monday
        var end = new DateTime(2026, 1, 6, 9, 0, 0);   // Tuesday

        var hours = _calculator.CalculateBusinessHours(start, end);

        Assert.Equal(24, hours);
    }

    [Fact]
    public void CalculateBusinessHours_IncludesWeekend_ExcludesWeekendHours()
    {
        // Friday 9am to Monday 9am = should be 24 hours (only Friday counts)
        var start = new DateTime(2026, 1, 9, 9, 0, 0);  // Friday
        var end = new DateTime(2026, 1, 12, 9, 0, 0);   // Monday

        var hours = _calculator.CalculateBusinessHours(start, end);

        // Friday 9am to midnight = 15 hours
        // Saturday = 0 hours (weekend)
        // Sunday = 0 hours (weekend)
        // Monday midnight to 9am = 9 hours
        Assert.Equal(24, hours); // 15 + 0 + 0 + 9 = 24
    }

    [Fact]
    public void CalculateBusinessHours_EntireWeekend_ReturnsZero()
    {
        // Saturday to Sunday = 0 hours
        var start = new DateTime(2026, 1, 10, 9, 0, 0); // Saturday
        var end = new DateTime(2026, 1, 11, 9, 0, 0);   // Sunday

        var hours = _calculator.CalculateBusinessHours(start, end);

        Assert.Equal(0, hours);
    }

    [Fact]
    public void CalculateBusinessHours_IncludesHoliday_ExcludesHolidayHours()
    {
        // Dec 24 to Dec 26 (Christmas on 25th)
        var start = new DateTime(2026, 12, 24, 9, 0, 0); // Thursday
        var end = new DateTime(2026, 12, 26, 9, 0, 0);   // Saturday

        var hours = _calculator.CalculateBusinessHours(start, end);

        // Dec 24 (Thu) 9am to midnight = 15 hours
        // Dec 25 (Fri - Christmas holiday) = 0 hours
        // Dec 26 (Sat) = 0 hours (weekend)
        Assert.Equal(15, hours);
    }

    [Fact]
    public void CalculateBusinessHours_SameDay_ReturnsHoursDifference()
    {
        // Same weekday, 8 hours apart
        var start = new DateTime(2026, 1, 5, 9, 0, 0);  // Monday 9am
        var end = new DateTime(2026, 1, 5, 17, 0, 0);   // Monday 5pm

        var hours = _calculator.CalculateBusinessHours(start, end);

        Assert.Equal(8, hours);
    }

    [Fact]
    public void CalculateBusinessHours_StartAfterEnd_ReturnsZero()
    {
        var start = new DateTime(2026, 1, 6, 9, 0, 0);
        var end = new DateTime(2026, 1, 5, 9, 0, 0);

        var hours = _calculator.CalculateBusinessHours(start, end);

        Assert.Equal(0, hours);
    }

    [Fact]
    public void CalculateBusinessHours_FullWeek_Returns120Hours()
    {
        // Monday to Saturday (5 business days = 120 hours)
        var start = new DateTime(2026, 1, 5, 0, 0, 0);  // Monday midnight
        var end = new DateTime(2026, 1, 10, 0, 0, 0);   // Saturday midnight

        var hours = _calculator.CalculateBusinessHours(start, end);

        Assert.Equal(120, hours); // 5 days * 24 hours
    }

    [Fact]
    public void IsBusinessDay_Weekday_ReturnsTrue()
    {
        var monday = new DateTime(2026, 1, 5);
        Assert.True(_calculator.IsBusinessDay(monday));
    }

    [Fact]
    public void IsBusinessDay_Saturday_ReturnsFalse()
    {
        var saturday = new DateTime(2026, 1, 10);
        Assert.False(_calculator.IsBusinessDay(saturday));
    }

    [Fact]
    public void IsBusinessDay_Sunday_ReturnsFalse()
    {
        var sunday = new DateTime(2026, 1, 11);
        Assert.False(_calculator.IsBusinessDay(sunday));
    }

    [Fact]
    public void IsBusinessDay_Holiday_ReturnsFalse()
    {
        var christmas = new DateTime(2026, 12, 25);
        Assert.False(_calculator.IsBusinessDay(christmas));
    }

    [Fact]
    public void GetWeekendDays_OneWeek_ReturnsTwo()
    {
        var start = new DateTime(2026, 1, 5);  // Monday
        var end = new DateTime(2026, 1, 11);   // Sunday

        var weekendDays = _calculator.GetWeekendDays(start, end);

        Assert.Equal(2, weekendDays);
    }

    [Fact]
    public void GetWeekendDays_TwoWeeks_ReturnsFour()
    {
        var start = new DateTime(2026, 1, 5);  // Monday
        var end = new DateTime(2026, 1, 18);   // Sunday

        var weekendDays = _calculator.GetWeekendDays(start, end);

        Assert.Equal(4, weekendDays);
    }

    [Fact]
    public void DefaultConstructor_NoHolidays_AllWeekdaysAreBusinessDays()
    {
        // Create calculator with default constructor (no hardcoded holidays)
        var defaultCalculator = new BusinessHoursCalculator();

        // Christmas 2026 is a regular weekday when no holidays configured
        var christmas2026 = new DateTime(2026, 12, 25); // Friday
        Assert.True(defaultCalculator.IsBusinessDay(christmas2026));

        // Regular Wednesday is still a business day
        var regularDay = new DateTime(2026, 1, 7); // Wednesday
        Assert.True(defaultCalculator.IsBusinessDay(regularDay));

        // Weekends are still non-business days
        var saturday = new DateTime(2026, 1, 10);
        Assert.False(defaultCalculator.IsBusinessDay(saturday));
    }

    [Fact]
    public void IOptionsConstructor_LoadsHolidaysFromSettings()
    {
        // Arrange
        var settings = Microsoft.Extensions.Options.Options.Create(new BusinessHoursSettings
        {
            Holidays = "2026-12-25,2026-01-01"
        });

        // Act
        var calculator = new BusinessHoursCalculator(settings);

        // Assert
        Assert.False(calculator.IsBusinessDay(new DateTime(2026, 12, 25)));
        Assert.False(calculator.IsBusinessDay(new DateTime(2026, 1, 1)));
        Assert.True(calculator.IsBusinessDay(new DateTime(2026, 1, 7))); // Wednesday
    }
}
