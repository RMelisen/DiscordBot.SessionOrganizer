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
    IReadOnlyDictionary<string, long> UsesByCommand);

/// <summary>
/// Her commands over one window, from CommandDailyStat rows and the paths registered right now
/// (CommandPath.Of over InteractionService.SlashCommands). A key no longer registered (a renamed
/// command) still ranks under its old name. Shortcuts (« ; ») are listed apart. Pure.
/// </summary>
public static class CommandReport
{
    public const int TopCommands = 15;

    public static CommandReportData Compute(IReadOnlyList<CommandDailyStat> rows, IReadOnlyCollection<string> registered)
    {
        var per = rows.GroupBy(r => r.Command)
            .Select(g => new CommandLine(g.Key, g.Sum(r => r.Uses), g.Sum(r => r.Failures)))
            .ToList();
        var slash = per.Where(c => !c.Command.StartsWith(';'))
            .OrderByDescending(c => c.Uses).ThenBy(c => c.Command, StringComparer.Ordinal).ToList();
        var shortcuts = per.Where(c => c.Command.StartsWith(';')).OrderByDescending(c => c.Uses).ToList();
        var used = per.Where(c => c.Uses > 0).Select(c => c.Command).ToHashSet();
        var never = registered.Where(c => !used.Contains(c)).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToList();
        return new CommandReportData(
            per.Sum(c => c.Uses), per.Sum(c => c.Failures),
            slash.Take(TopCommands).ToList(), shortcuts, never,
            per.ToDictionary(c => c.Command, c => c.Uses));
    }
}
