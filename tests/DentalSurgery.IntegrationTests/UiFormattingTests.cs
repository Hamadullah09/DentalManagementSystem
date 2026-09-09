using DentalSurgery.Web.Components.Shared;
using Xunit;

namespace DentalSurgery.IntegrationTests;

/// <summary>
/// The formatting helpers the pages render through.
/// <para>
/// Worth testing directly because a bad format string is not a compile error.
/// It throws when the component renders, and Blazor turns that into "this
/// screen could not be displayed" — a whole page that simply will not open,
/// with nothing to point at but a stack trace in the log. The appointment
/// book's booking panel was unopenable from the first commit for exactly this
/// reason.
/// </para>
/// </summary>
public class UiFormattingTests
{
    [Theory]
    [InlineData(9, 0, "09:00")]
    [InlineData(9, 30, "09:30")]
    [InlineData(14, 5, "14:05")]
    [InlineData(0, 0, "00:00")]
    [InlineData(23, 59, "23:59")]
    public void A_time_of_day_renders_as_hours_and_minutes(int hours, int minutes, string expected)
    {
        Assert.Equal(expected, Ui.TimeOfDay(new TimeSpan(hours, minutes, 0)));
    }

    [Fact]
    public void A_time_of_day_does_not_throw_on_any_time_in_the_day()
    {
        // TimeSpan format specifiers are case-sensitive and have no "HH". Using
        // a DateTime pattern compiles and then throws at render time, so the
        // whole range is walked rather than trusting one sample.
        for (var minute = 0; minute < 24 * 60; minute++)
        {
            var time = TimeSpan.FromMinutes(minute);
            var rendered = Ui.TimeOfDay(time);

            Assert.Equal(5, rendered.Length);
            Assert.Equal(':', rendered[2]);
        }
    }

    [Fact]
    public void An_absent_time_reads_as_a_dash_rather_than_empty()
    {
        Assert.Equal("-", Ui.TimeOfDay((TimeSpan?)null));
    }

    [Fact]
    public void The_rendered_time_round_trips_back_to_the_same_value()
    {
        // The booking panel writes this into an <input type="time"> and parses
        // whatever comes back, so the two directions have to agree.
        var original = new TimeSpan(16, 45, 0);

        Assert.True(TimeSpan.TryParse(Ui.TimeOfDay(original), out var parsed));
        Assert.Equal(original, parsed);
    }
}
