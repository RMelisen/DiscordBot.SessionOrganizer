namespace ProjectSYNCS.Helpers;

// When and what her weekend poll proposes (WeekendPollService), pure and in Paris wall-clock time
// so DST never moves it. Posted on Wednesday evening: the poll auto-closes two days later
// (ReminderService.PollLifetime), on Friday evening, before the first slot.
public static class WeekendPoll
{
    public const DayOfWeek PostingDay = DayOfWeek.Wednesday;

    // The posting slot falls somewhere in [WindowStartHour, WindowStartHour + WindowMinutes),
    // drawn from the week so a restart lands on the same minute. Past WindowEndHour the week is
    // skipped: a poll posted on Thursday would close on Saturday, after the Friday slot.
    public const int WindowStartHour = 18;
    public const int WindowMinutes = 120;
    public const int WindowEndHour = 22;

    // The slots it proposes: Friday, Saturday and Sunday at 21:00.
    public const int SlotHour = 21;
    private static readonly int[] SlotDayOffsets = { 2, 3, 4 };

    private const int SlotSalt = 0x5745_454B; // "WEEK"

    /// <summary>The posting slot for the Wednesday <paramref name="wednesday"/> (a date, midnight).</summary>
    public static DateTimeOffset PostingSlot(DateTime wednesday)
    {
        var dayNumber = DateOnly.FromDateTime(wednesday).DayNumber;
        var minutes = (int)(StableRoll.Unit(dayNumber, SlotSalt, 0) * WindowMinutes);
        return AppTime.AtWallClock(wednesday.Date.AddHours(WindowStartHour).AddMinutes(minutes));
    }

    /// <summary>Past the posting slot and before the window closes, on a Wednesday.</summary>
    public static bool IsDue(DateTimeOffset now, out DateTime wednesday)
    {
        var zoned = AppTime.ToZoned(now);
        wednesday = zoned.Date;
        if (zoned.DayOfWeek != PostingDay) return false;
        return now >= PostingSlot(wednesday) && zoned.Hour < WindowEndHour;
    }

    /// <summary>The three slots the poll proposes, for the week of <paramref name="wednesday"/>.</summary>
    public static IReadOnlyList<DateTimeOffset> Slots(DateTime wednesday) =>
        SlotDayOffsets.Select(d => AppTime.AtWallClock(wednesday.Date.AddDays(d).AddHours(SlotHour))).ToList();

    /// <summary>The weekend it covers: Friday 00:00 to Monday 00:00.</summary>
    public static (DateTimeOffset From, DateTimeOffset To) Weekend(DateTime wednesday) =>
        (AppTime.AtWallClock(wednesday.Date.AddDays(2)), AppTime.AtWallClock(wednesday.Date.AddDays(5)));
}
