using Discord.Interactions;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>Who a monthly digest is for. Not stored: report-state.json keys each by its own field.</summary>
public enum DigestRecipient
{
    [ChoiceDisplay("Moi")] Owner,
    [ChoiceDisplay("Tata")] Tata,
}

public enum DigestAlertKind
{
    FailingCommand,
    PowerCuts,
    Crashes,
    HeatRecord,
    UnansweredRising,
    QuizTooEasy,
    QuizTooHard,
    ActiveDrop,
    MoreLeaves,
    ManySleepers,
}

/// <summary>
/// One line of « À regarder ». <paramref name="Subject"/> is a command key for FailingCommand;
/// <paramref name="Value"/> and <paramref name="Of"/> are the kind's two figures (see the card).
/// </summary>
public sealed record DigestAlert(DigestAlertKind Kind, long Value, long Of = 0, string Subject = "");

/// <summary>
/// The « À regarder » part of the monthly digest's first message: thresholds over the reports'
/// figures, pure so the checks can feed it cases. A comparison with the month before is only made
/// when that month has data, so the first digest after a new counter (or a quiet month) raises
/// nothing it can't back up. Papa's version is about her running well (failing commands, the Pi,
/// what she can't answer, the quiz's calibration); Tata's about the server's life.
/// </summary>
public static class DigestBrief
{
    // A command is failing when it failed at least this many times and more than this share of its uses.
    public const int FailingMin = 2;
    public const double FailingRate = 0.05;
    public const int FailingListed = 5;

    // « Sans réponse précise » rising: both months need this many answers, and the share must rise
    // by at least this many points.
    public const int UnansweredMinAnswers = 50;
    public const int UnansweredRisePoints = 5;

    // A difficulty is miscalibrated when, over at least QuizMinPosted questions, « difficile » is
    // found this often or « facile » this rarely.
    public const int QuizMinPosted = 5;
    public const double QuizHardTooEasy = 0.80;
    public const double QuizEasyTooHard = 0.30;

    // Active members dropping: by at least this share, from at least ActiveMin the month before.
    public const double ActiveDropRate = 0.20;
    public const int ActiveMin = 5;

    // Sleepers: more than this share of at least SleepersMinMembers members.
    public const double SleepersRate = 0.10;
    public const int SleepersMinMembers = 10;

    public static IReadOnlyList<DigestAlert> Owner(
        Compared<CommandReportData> commands, Compared<SyncsReportData> syncs, (int Day, int MaxMilli)? heatRecord,
        Compared<QuizReportData> quiz, ReportWindow window)
    {
        var alerts = new List<DigestAlert>();

        alerts.AddRange(commands.Current.Failing
            .Where(c => c.Failures >= FailingMin && c.Failures > c.Uses * FailingRate)
            .Take(FailingListed)
            .Select(c => new DigestAlert(DigestAlertKind.FailingCommand, c.Failures, c.Uses, c.Command)));

        var s = syncs.Current;
        var cuts = s.Outages.Where(o => o.Kind == UptimeEventKind.PowerCut).Sum(o => o.Count);
        var crashes = s.Outages.Where(o => o.Kind == UptimeEventKind.Crash).Sum(o => o.Count);
        if (cuts > 0) alerts.Add(new DigestAlert(DigestAlertKind.PowerCuts, cuts));
        if (crashes > 0) alerts.Add(new DigestAlert(DigestAlertKind.Crashes, crashes));
        // A record only means something against months already measured.
        if (heatRecord is { } record && s.MaxMilli == record.MaxMilli && window.Contains(record.Day)
            && syncs.Previous is { DaysMeasured: > 0 })
            alerts.Add(new DigestAlert(DigestAlertKind.HeatRecord, record.MaxMilli));

        if (syncs.Previous is { } p && p.Answers >= UnansweredMinAnswers && s.Answers >= UnansweredMinAnswers)
        {
            var now = Percent(s.Unrecognised, s.Answers);
            var before = Percent(p.Unrecognised, p.Answers);
            if (now - before >= UnansweredRisePoints)
                alerts.Add(new DigestAlert(DigestAlertKind.UnansweredRising, now, before));
        }

        var q = quiz.Current;
        var hard = q.ByDifficulty.FirstOrDefault(l => l.Label == QuizBank.Label(QuizDifficulty.Hard));
        if (hard is { Posted: >= QuizMinPosted } && hard.Found >= hard.Posted * QuizHardTooEasy)
            alerts.Add(new DigestAlert(DigestAlertKind.QuizTooEasy, Percent(hard.Found, hard.Posted)));
        var easy = q.ByDifficulty.FirstOrDefault(l => l.Label == QuizBank.Label(QuizDifficulty.Easy));
        if (easy is { Posted: >= QuizMinPosted } && easy.Found < easy.Posted * QuizEasyTooHard)
            alerts.Add(new DigestAlert(DigestAlertKind.QuizTooHard, Percent(easy.Found, easy.Posted)));

        return alerts;
    }

    public static IReadOnlyList<DigestAlert> Tata(Compared<CommunityReportData> community, SleepingReportData sleeping)
    {
        var alerts = new List<DigestAlert>();
        var c = community.Current;
        if (community.Previous is { ActiveMembers: >= ActiveMin } p && c.ActiveMembers <= p.ActiveMembers * (1 - ActiveDropRate))
            alerts.Add(new DigestAlert(DigestAlertKind.ActiveDrop, c.ActiveMembers, p.ActiveMembers));
        if (c.Leaves > c.Joins)
            alerts.Add(new DigestAlert(DigestAlertKind.MoreLeaves, c.Leaves, c.Joins));
        if (sleeping.Members >= SleepersMinMembers && sleeping.Sleepers.Count > sleeping.Members * SleepersRate)
            alerts.Add(new DigestAlert(DigestAlertKind.ManySleepers, sleeping.Sleepers.Count, sleeping.Members));
        return alerts;
    }

    /// <summary>The weekday whose sessions were best attended (at least two), or null.</summary>
    public static SlotTally? BestSessionDay(SessionReportData sessions) =>
        sessions.Weekdays
            .Where(s => s.Sessions >= 2 && s.Expected > 0)
            .OrderByDescending(s => s.Came / (double)s.Expected).ThenByDescending(s => s.Sessions)
            .FirstOrDefault();

    private static long Percent(long part, long total) => total <= 0 ? 0 : (long)Math.Round(part * 100.0 / total);
}
