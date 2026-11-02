using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public sealed record CommandLine(string Command, long Uses, long Failures);

/// <summary>What /report commands shows. Built by <see cref="CommandReport.Compute"/>.</summary>
public sealed record CommandReportData(
    long Uses,
    long Failures,
    IReadOnlyList<CommandLine> Top,
    IReadOnlyList<CommandLine> Shortcuts,
    IReadOnlyList<string> NeverUsed,
    IReadOnlyDictionary<string, long> UsesByCommand,
    IReadOnlyList<CommandLine> Failing)
{
    /// <summary>The staff's commands nobody used, listed after the members' (<see cref="CommandReportData.NeverUsed"/>).</summary>
    public IReadOnlyList<string> NeverUsedStaff { get; init; } = Array.Empty<string>();

    /// <summary>/debug's uses, shown apart: the owner's testing isn't the server's use.</summary>
    public long DebugUses { get; init; }

    /// <summary>Everyone who used a command in the window; empty before the reach was counted.</summary>
    public IReadOnlySet<ulong> People { get; init; } = new HashSet<ulong>();

    /// <summary>Different people per command key.</summary>
    public IReadOnlyDictionary<string, int> PeopleByCommand { get; init; } = new Dictionary<string, int>();

    /// <summary>Different people per top-level command, largest first.</summary>
    public IReadOnlyList<(string Feature, int People)> Features { get; init; } = Array.Empty<(string, int)>();

    /// <summary>The first day the reach has a row at all (it started later than the uses).</summary>
    public int? PeopleSince { get; init; }
}

/// <summary>
/// Her commands over one window, from CommandDailyStat rows and the paths registered right now
/// (CommandPath.Of over InteractionService.SlashCommands). A key no longer registered (a renamed
/// command) still ranks under its old name. Shortcuts (« ; ») are listed apart. /debug stays out of the
/// totals, the ranking and the never-used lists, since it is the owner testing; its failures still reach
/// Failing, since a failing tool is worth knowing about. Pure.
/// </summary>
public static class CommandReport
{
    public const int TopCommands = 15;

    public static CommandReportData Compute(IReadOnlyList<CommandDailyStat> rows, IReadOnlyCollection<string> registered,
        IReadOnlyList<CommandMemberDailyStat>? people = null)
    {
        var all = rows.GroupBy(r => r.Command)
            .Select(g => new CommandLine(g.Key, g.Sum(r => r.Uses), g.Sum(r => r.Failures)))
            .ToList();
        var per = all.Where(c => !CommandPath.IsDebug(c.Command)).ToList();
        var slash = per.Where(c => !c.Command.StartsWith(';'))
            .OrderByDescending(c => c.Uses).ThenBy(c => c.Command, StringComparer.Ordinal).ToList();
        var shortcuts = per.Where(c => c.Command.StartsWith(';')).OrderByDescending(c => c.Uses).ToList();
        var used = per.Where(c => c.Uses > 0).Select(c => c.Command).ToHashSet();
        var never = registered.Where(c => !used.Contains(c) && !CommandPath.IsDebug(c)).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToList();

        var reach = (people ?? Array.Empty<CommandMemberDailyStat>()).Where(r => !CommandPath.IsDebug(r.Command)).ToList();
        return new CommandReportData(
            per.Sum(c => c.Uses), per.Sum(c => c.Failures),
            slash.Take(TopCommands).ToList(), shortcuts, never.Where(c => !CommandPath.IsStaff(c)).ToList(),
            per.ToDictionary(c => c.Command, c => c.Uses),
            // Every command that failed, top ten or not, for the digest's « À regarder ».
            all.Where(c => c.Failures > 0).OrderByDescending(c => c.Failures).ThenBy(c => c.Command, StringComparer.Ordinal).ToList())
        {
            NeverUsedStaff = never.Where(CommandPath.IsStaff).ToList(),
            DebugUses = all.Where(c => CommandPath.IsDebug(c.Command)).Sum(c => c.Uses),
            People = reach.Select(r => r.UserId).ToHashSet(),
            PeopleByCommand = reach.GroupBy(r => r.Command).ToDictionary(g => g.Key, g => g.Select(r => r.UserId).Distinct().Count()),
            Features = reach.GroupBy(r => CommandPath.Feature(r.Command))
                .Select(g => (g.Key, g.Select(r => r.UserId).Distinct().Count()))
                .OrderByDescending(x => x.Item2).ThenBy(x => x.Key, StringComparer.Ordinal)
                .ToList(),
        };
    }

    /// <summary>Of this window's people, how many also used a command in the window before; null when there is nothing to compare.</summary>
    public static int? Returning(Compared<CommandReportData> c) =>
        c.Previous is { People.Count: > 0 } p ? c.Current.People.Count(p.People.Contains) : null;
}
