using System.Text.RegularExpressions;
using Discord;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

// When MorningGreetingService says hello: once a day, at a random moment inside a
// fixed wall-clock window in the app's zone. Pure, so the schedule can be checked
// without a gateway.
public static class MorningGreeting
{
    public static readonly TimeSpan WindowStart = new(8, 0, 0);
    public static readonly TimeSpan WindowEnd = new(10, 0, 0);

    // Her birthday: the repo's first commit. On that day each year the hello is her age
    // instead of a pool line.
    public const int BirthYear = 2026;
    public const int BirthMonth = 6;
    public const int BirthDay = 16;

    /// <summary>Whether <paramref name="now"/> falls on her birthday, in the app's zone.</summary>
    public static bool IsBirthday(DateTimeOffset now)
    {
        var day = AppTime.ToZoned(now);
        return day.Month == BirthMonth && day.Day == BirthDay;
    }

    /// <summary>Her age in whole years on <paramref name="now"/>'s year (0 on the first birthday itself).</summary>
    public static int Age(DateTimeOffset now) => AppTime.ToZoned(now).Year - BirthYear;

    /// <summary>How many days she has been running on <paramref name="now"/>, in the app's zone (0 on her first day).</summary>
    public static int DaysOld(DateTimeOffset now) =>
        DateOnly.FromDateTime(AppTime.ToZoned(now).DateTime).DayNumber - new DateOnly(BirthYear, BirthMonth, BirthDay).DayNumber;

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

    // The pools go through DailyRotation, each with its own salt so their orders are unrelated;
    // changing a salt reshuffles that pool's rotation.
    public const ulong GreetingSalt = 0x5359_4E43_5348_454C;   // "SYNCSHEL"
    public const ulong FunFactSalt = 0x5359_4E43_5346_4143;    // "SYNCSFAC"
    public const ulong DedicationSalt = 0x5359_4E43_5344_4544; // "SYNCSDED"
    public const ulong CreditSalt = 0x5359_4E43_5343_5244;     // "SYNCSCRD"
    public const ulong StyleSalt = 0x5359_4E43_5353_5459;      // "SYNCSSTY"

    // On a dedicated day, the share of mornings that keep the classic hello and end on a credit line
    // for the winner instead of replacing the hello (the owner's call: 60 %).
    public const int CreditPercent = 60;

    /// <summary>
    /// Whether a dedication on <paramref name="dayNumber"/> keeps the classic hello and ends on a
    /// DedicationCredits line, rather than a DedicationLines line in its place. From the date alone,
    /// so a restart or an early hello says what the slot would have; over every 100 days, exactly
    /// <see cref="CreditPercent"/> of them.
    /// </summary>
    public static bool UsesCredit(int dayNumber) => DailyRotation.IndexFor(100, dayNumber, StyleSalt) < CreditPercent;

    /// <summary>
    /// What the morning hello says at <paramref name="now"/>. Her birthday: its own line alone, and a
    /// dedication waits for the next morning (UsesDedication false). A /shoutout winner, pinged alone:
    /// on a <see cref="UsesCredit"/> day today's hello, the fun fact, then a DedicationCredits line
    /// naming them; otherwise a DedicationLines line naming them, then the fun fact. Otherwise today's
    /// hello and fun fact. The hello is always the first line, alone: that is how it is recognised.
    /// </summary>
    public static (string Text, AllowedMentions Mentions, bool UsesDedication) ComposeHello(DateTimeOffset now, ulong? dedicatedTo)
    {
        if (IsBirthday(now)) return (BotResponses.BirthdayGreeting(Age(now)), AllowedMentions.None, false);

        var day = AppTime.DayNumber(now);
        var hello = DailyRotation.Pick(BotResponses.MorningGreetings, day, GreetingSalt);
        var fact = DailyRotation.Pick(BotResponses.MorningFunFacts, day, FunFactSalt);
        if (dedicatedTo is not { } winner)
            return (hello + "\n" + fact, AllowedMentions.None, false);

        var mention = $"<@{winner}>";
        var pings = new AllowedMentions { UserIds = new List<ulong> { winner } };
        return UsesCredit(day)
            ? (hello + "\n" + fact + "\n" + string.Format(DailyRotation.Pick(BotResponses.DedicationCredits, day, CreditSalt), mention), pings, true)
            : (string.Format(DailyRotation.Pick(BotResponses.DedicationLines, day, DedicationSalt), mention) + "\n" + fact, pings, true);
    }

    /// <summary>Whether a message's first line is a dedication hello: a DedicationLines line with a mention for {0}.</summary>
    public static bool IsDedicationLine(string firstLine) => DedicationPatterns.Value.Any(p => p.IsMatch(firstLine));

    private static readonly Lazy<Regex[]> DedicationPatterns = new(() => BotResponses.DedicationLines
        .Select(l => l.Split("{0}"))
        .Select(parts => new Regex("^" + Regex.Escape(parts[0]) + @"<@!?\d+>" + Regex.Escape(parts[1]) + "$", RegexOptions.CultureInvariant))
        .ToArray());

    private static DateTimeOffset At(DateTime wallClock) =>
        new(wallClock, AppTime.Zone.GetUtcOffset(wallClock));
}
