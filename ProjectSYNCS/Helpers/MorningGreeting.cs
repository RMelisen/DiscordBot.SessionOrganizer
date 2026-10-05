namespace ProjectSYNCS.Helpers;

// When MorningGreetingService says hello: once a day, at a random moment inside a
// fixed wall-clock window in the app's zone. Pure, so the schedule can be checked
// without a gateway.
public static class MorningGreeting
{
    public static readonly TimeSpan WindowStart = new(8, 0, 0);
    public static readonly TimeSpan WindowEnd = new(10, 0, 0);

    // Someone else's greeting can draw her hello out from this earlier time on, up to
    // WindowEnd. Earlier than her own window on purpose: an early riser gets an answer.
    public static readonly TimeSpan ReplyWindowStart = new(7, 0, 0);

    /// <summary>
    /// The next moment to greet, as a UTC instant. Today's window is used while it is
    /// still open and today hasn't been greeted yet — a start inside the window draws
    /// from what is left of it rather than skipping the day — otherwise tomorrow's.
    /// </summary>
    public static DateTimeOffset NextSlot(DateTimeOffset now, bool greetedToday, Random random)
    {
        var today = AppTime.ToZoned(now).Date;
        var (start, end) = Window(today);

        if (greetedToday || now >= end)
            (start, end) = Window(today.AddDays(1));
        else if (now > start)
            start = now;

        var span = end - start;
        return (start + TimeSpan.FromTicks((long)(random.NextDouble() * span.Ticks))).ToUniversalTime();
    }

    /// <summary>
    /// Whether someone's greeting at <paramref name="now"/> may bring the hello forward:
    /// from <see cref="ReplyWindowStart"/> to the end of today's window.
    /// </summary>
    public static bool AcceptsReply(DateTimeOffset now)
    {
        var day = AppTime.ToZoned(now).Date;
        return now >= At(day + ReplyWindowStart) && now < Window(day).End;
    }

    /// <summary>The end of the window the given slot falls in — the deadline for a late send.</summary>
    public static DateTimeOffset WindowEndFor(DateTimeOffset slot) =>
        Window(AppTime.ToZoned(slot).Date).End;

    // The offset is resolved per wall-clock moment, so the window keeps its Paris
    // wall-clock times across DST. Both ends sit well clear of the 2–3 am changeover.
    private static (DateTimeOffset Start, DateTimeOffset End) Window(DateTime day) =>
        (At(day + WindowStart), At(day + WindowEnd));

    private static DateTimeOffset At(DateTime wallClock) =>
        new(wallClock, AppTime.Zone.GetUtcOffset(wallClock));
}
