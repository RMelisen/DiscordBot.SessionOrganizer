using Microsoft.Data.Sqlite;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The kinds of incident she counts (IncidentDailyStat.Kind, stored: append-only), and what each is
/// counted under. Slash is a command whose handler reported a failure, Button and Modal the same for a
/// click or a form, Late an interaction still unanswered after Discord's 3 s, which the person saw fail
/// even if the work was done.
/// </summary>
public static class IncidentKinds
{
    public const string Slash = "slash";
    public const string Button = "button";
    public const string Modal = "modal";
    public const string Late = "late";
    public const string LogWarning = "log.warning";
    public const string LogError = "log.error";
    public const string SqliteBusy = "sqlite.busy";
    public const string Disconnect = "gateway.disconnect";
    public const string Restart = "restart";

    // EF names one logger per concern; they are one thing in a report.
    private const string EfPrefix = "Microsoft.EntityFrameworkCore";

    // Where EF logs a failed command, once per failure; its Update logger says it again for a save.
    private const string EfDatabasePrefix = EfPrefix + ".Database.";

    /// <summary>A logger's name as a report shows it: the class alone, or « EF Core » for EF's own.</summary>
    public static string LogKey(string category)
    {
        if (category.StartsWith(EfPrefix, StringComparison.Ordinal)) return "EF Core";
        var dot = category.LastIndexOf('.');
        return dot < 0 ? category : category[(dot + 1)..];
    }

    /// <summary>A failed database command because the file was locked (SQLITE_BUSY, SQLITE_LOCKED).</summary>
    public static bool IsBusy(string category, Exception? exception)
    {
        if (!category.StartsWith(EfDatabasePrefix, StringComparison.Ordinal)) return false;
        for (var e = exception; e is not null; e = e.InnerException)
            if (e is SqliteException { SqliteErrorCode: 5 or 6 }) return true;
        return false;
    }
}

/// <summary>One thing that went wrong, and how often. <paramref name="Kind"/> is an IncidentKinds key.</summary>
public sealed record IncidentLine(string Kind, string Key, long Count);

/// <summary>What /report incidents shows. Built by <see cref="IncidentReport.Compute"/>.</summary>
public sealed record IncidentReportData(
    long Slash,
    long Buttons,
    long Modals,
    long Late,
    long LogErrors,
    long LogWarnings,
    long SqliteBusy,
    long Disconnects,
    IReadOnlyList<(string Stop, long Count)> Restarts,
    IReadOnlyList<IncidentLine> TopFailed,
    IReadOnlyList<IncidentLine> TopLate,
    IReadOnlyList<IncidentLine> TopLogs)
{
    /// <summary>What someone clicked or typed and saw fail.</summary>
    public long Failed => Slash + Buttons + Modals;
}

/// <summary>
/// What went wrong over one window, from IncidentDailyStat rows (filtered on Day in SQL by the caller).
/// Log lines are lines, not faults: one failure can be logged by EF and again by the service that
/// caught it. Pure.
/// </summary>
public static class IncidentReport
{
    public const int Listed = 5;

    public static IncidentReportData Compute(IReadOnlyList<IncidentDailyStat> rows)
    {
        long Sum(string kind) => rows.Where(r => r.Kind == kind).Sum(r => r.Count);
        IReadOnlyList<IncidentLine> Top(params string[] kinds) => rows
            .Where(r => kinds.Contains(r.Kind))
            .GroupBy(r => (r.Kind, r.Key))
            .Select(g => new IncidentLine(g.Key.Kind, g.Key.Key, g.Sum(r => r.Count)))
            .OrderByDescending(l => l.Count).ThenBy(l => l.Key, StringComparer.Ordinal)
            .Take(Listed)
            .ToList();

        return new IncidentReportData(
            Sum(IncidentKinds.Slash), Sum(IncidentKinds.Button), Sum(IncidentKinds.Modal), Sum(IncidentKinds.Late),
            Sum(IncidentKinds.LogError), Sum(IncidentKinds.LogWarning), Sum(IncidentKinds.SqliteBusy), Sum(IncidentKinds.Disconnect),
            rows.Where(r => r.Kind == IncidentKinds.Restart)
                .GroupBy(r => r.Key)
                .Select(g => (g.Key, g.Sum(r => r.Count)))
                .OrderByDescending(x => x.Item2).ThenBy(x => x.Key, StringComparer.Ordinal)
                .ToList(),
            Top(IncidentKinds.Slash, IncidentKinds.Button, IncidentKinds.Modal),
            Top(IncidentKinds.Late),
            Top(IncidentKinds.LogError, IncidentKinds.LogWarning));
    }
}
