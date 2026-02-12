using OrderMonitor.Core.Configuration;
using Xunit;

namespace OrderMonitor.UnitTests.Configuration;

public class BusinessHoursSettingsTests
{
    [Fact]
    public void GetHolidayDates_EmptyString_ReturnsEmpty()
    {
        var settings = new BusinessHoursSettings { Holidays = "" };

        var dates = settings.GetHolidayDates().ToList();

        Assert.Empty(dates);
    }

    [Fact]
    public void GetHolidayDates_NullString_ReturnsEmpty()
    {
        var settings = new BusinessHoursSettings { Holidays = null! };

        var dates = settings.GetHolidayDates().ToList();

        Assert.Empty(dates);
    }

    [Fact]
    public void GetHolidayDates_WhitespaceString_ReturnsEmpty()
    {
        var settings = new BusinessHoursSettings { Holidays = "   " };

        var dates = settings.GetHolidayDates().ToList();

        Assert.Empty(dates);
    }

    [Fact]
    public void GetHolidayDates_SingleDate_ReturnsSingleDate()
    {
        var settings = new BusinessHoursSettings { Holidays = "2026-12-25" };

        var dates = settings.GetHolidayDates().ToList();

        Assert.Single(dates);
        Assert.Equal(new DateTime(2026, 12, 25), dates[0]);
    }

    [Fact]
    public void GetHolidayDates_MultipleDates_ReturnsAllDates()
    {
        var settings = new BusinessHoursSettings
        {
            Holidays = "2026-01-01,2026-12-25,2026-12-26"
        };

        var dates = settings.GetHolidayDates().ToList();

        Assert.Equal(3, dates.Count);
        Assert.Contains(new DateTime(2026, 1, 1), dates);
        Assert.Contains(new DateTime(2026, 12, 25), dates);
        Assert.Contains(new DateTime(2026, 12, 26), dates);
    }

    [Fact]
    public void GetHolidayDates_WithSpaces_TrimsCorrectly()
    {
        var settings = new BusinessHoursSettings
        {
            Holidays = " 2026-01-01 , 2026-12-25 "
        };

        var dates = settings.GetHolidayDates().ToList();

        Assert.Equal(2, dates.Count);
    }

    [Fact]
    public void GetHolidayDates_InvalidDate_SkipsInvalid()
    {
        var settings = new BusinessHoursSettings
        {
            Holidays = "2026-01-01,not-a-date,2026-12-25"
        };

        var dates = settings.GetHolidayDates().ToList();

        Assert.Equal(2, dates.Count);
        Assert.Contains(new DateTime(2026, 1, 1), dates);
        Assert.Contains(new DateTime(2026, 12, 25), dates);
    }

    [Fact]
    public void GetHolidayDates_ReturnsDateOnly_NoTime()
    {
        var settings = new BusinessHoursSettings { Holidays = "2026-12-25" };

        var dates = settings.GetHolidayDates().ToList();

        Assert.Equal(TimeSpan.Zero, dates[0].TimeOfDay);
    }

    [Fact]
    public void Defaults_AreCorrect()
    {
        var settings = new BusinessHoursSettings();

        Assert.Equal("Europe/London", settings.Timezone);
        Assert.Equal(0, settings.StartHour);
        Assert.Equal(0, settings.EndHour);
        Assert.Equal(string.Empty, settings.Holidays);
    }
}
