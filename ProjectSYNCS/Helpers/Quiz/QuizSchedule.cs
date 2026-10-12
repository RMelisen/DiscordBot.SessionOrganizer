namespace ProjectSYNCS.Helpers;

// When QuizMasterService posts a question. Pure, so the rules can be checked without a
// gateway; every wall-clock time is Paris (AppTime), so DST never moves a window.
//
// Two slots a day, one per window, each kept at SlotChance: 0 to 2 quizzes a day, 1.5 on
// average. A due slot still waits until the channel has had someone in it recently
// (ActiveWithin), so a question never lands in an empty room; it waits at most until
// DayEnd, then the day is over. MinGap keeps a late first slot and the second apart.
public static class QuizSchedule
{
    public static readonly TimeSpan OpenFor = TimeSpan.FromHours(1);
    public static readonly TimeSpan MinGap = TimeSpan.FromHours(3);
    public static readonly TimeSpan ActiveWithin = TimeSpan.FromMinutes(45);
    public const int MaxPerDay = 2;
    public const double SlotChance = 0.75;

    private static readonly (TimeSpan Start, TimeSpan End)[] Windows =
    {
        (new TimeSpan(11, 0, 0), new TimeSpan(16, 0, 0)),
        (new TimeSpan(16, 30, 0), new TimeSpan(22, 0, 0)),
    };

    /// <summary>No slot is posted at or after this wall-clock time; a waiting one is dropped.</summary>
    public static readonly TimeSpan DayEnd = new(22, 0, 0);

    /// <summary>
    /// Today's slots as UTC instants, earliest first. A slot already in the past is
    /// dropped, so a restart mid-day doesn't fire a backlog: after an update she may skip a
    /// quiz, never double one (the daily count and MinGap guard that too).
    /// </summary>
    public static List<DateTimeOffset> DrawSlots(DateTimeOffset now, Random random)
    {
        var day = AppTime.ToZoned(now).Date;
        var slots = new List<DateTimeOffset>();
        foreach (var (start, end) in Windows)
        {
            if (random.NextDouble() >= SlotChance) continue;
            var minutes = random.Next((int)(end - start).TotalMinutes);
            var slot = AppTime.AtWallClock(day + start + TimeSpan.FromMinutes(minutes)).ToUniversalTime();
            if (slot > now) slots.Add(slot);
        }
        return slots;
    }

    /// <summary>Today's <see cref="DayEnd"/> as an instant.</summary>
    public static DateTimeOffset DayEndFor(DateTimeOffset now) =>
        AppTime.AtWallClock(AppTime.ToZoned(now).Date + DayEnd);

    /// <summary>
    /// Whether a due slot may become a question now. False means "not yet" — the caller keeps
    /// the slot — except past <see cref="DayEnd"/> or at <see cref="MaxPerDay"/>, where it
    /// should drop it (<see cref="DayIsOver"/>).
    /// </summary>
    public static bool CanPost(DateTimeOffset now, int postedToday, DateTimeOffset? lastPostedAt,
        DateTimeOffset? lastHumanAt)
    {
        if (DayIsOver(now, postedToday)) return false;
        if (lastPostedAt is { } last && now - last < MinGap) return false;
        return lastHumanAt is { } human && now - human <= ActiveWithin;
    }

    public static bool DayIsOver(DateTimeOffset now, int postedToday) =>
        postedToday >= MaxPerDay || now >= DayEndFor(now);
}
