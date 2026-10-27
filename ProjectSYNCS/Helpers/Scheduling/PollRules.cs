using ProjectSYNCS.Models;
using Poll = ProjectSYNCS.Models.Poll;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The rules of a poll or a vote that don't need Discord: reading a typed list of options, whether
/// it still takes votes, who leads, how far its deadline can be pushed. Pure, for the checks.
/// </summary>
public static class PollRules
{
    /// <summary>Options per poll or vote: two rows of five buttons.</summary>
    public const int MaxOptions = 10;

    /// <summary>A vote option's label: Discord's button label cap.</summary>
    public const int MaxLabelLength = 80;

    /// <summary>The longest a deadline may be pushed to, from now.</summary>
    public static readonly TimeSpan MaxLifetime = TimeSpan.FromDays(14);

    /// <summary>
    /// « une option par ligne »: trimmed, list markers dropped (« - », « • », « 1. »), each cut to
    /// <see cref="MaxLabelLength"/>, duplicates (any case) and empty lines skipped, at most
    /// <see cref="MaxOptions"/>. The caller checks there are at least two.
    /// </summary>
    public static List<string> ParseOptions(string text)
    {
        var options = new List<string>();
        foreach (var raw in text.Split('\n'))
        {
            var label = raw.Trim().TrimStart('-', '•', '*', '·').Trim();
            // « 1. Among Us » or « 2) Gartic »: the number is the list's, not the option's.
            var dot = label.IndexOfAny(new[] { '.', ')' });
            if (dot is > 0 and <= 2 && label[..dot].All(char.IsDigit)) label = label[(dot + 1)..].Trim();
            if (label.Length == 0) continue;
            if (label.Length > MaxLabelLength) label = label[..MaxLabelLength].TrimEnd();
            if (options.Contains(label, StringComparer.OrdinalIgnoreCase)) continue;
            options.Add(label);
            if (options.Count == MaxOptions) break;
        }
        return options;
    }

    /// <summary>
    /// The options typed on one line (`/vote create options:`), where a slash option has no line
    /// breaks: « Valorant, Lethal Company; Among Us », commas or semicolons, then as <see cref="ParseOptions"/>.
    /// </summary>
    public static List<string> ParseInlineOptions(string text) =>
        ParseOptions(text.Replace(',', '\n').Replace(';', '\n'));

    /// <summary>Open: not closed by hand, and its deadline not reached.</summary>
    public static bool IsOpen(Poll poll, DateTimeOffset now) => !poll.IsClosed && now < poll.ClosesAt;

    /// <summary>
    /// The deadline pushed by <paramref name="by"/> from the later of now and the current deadline,
    /// held under <see cref="MaxLifetime"/> from now.
    /// </summary>
    public static DateTimeOffset Extend(Poll poll, TimeSpan by, DateTimeOffset now)
    {
        var from = poll.ClosesAt > now ? poll.ClosesAt : now;
        var to = from + by;
        return to - now > MaxLifetime ? now + MaxLifetime : to;
    }

    /// <summary>Distinct people who voted on any option.</summary>
    public static int VoterCount(Poll poll) =>
        poll.Options.SelectMany(o => o.Votes).Select(v => v.UserId).Distinct().Count();

    /// <summary>The options with the most votes, in card order; none when nobody voted.</summary>
    public static List<PollOption> Leaders(Poll poll)
    {
        var best = poll.Options.Count == 0 ? 0 : poll.Options.Max(o => o.Votes.Count);
        return best == 0 ? new List<PollOption>() : Ordered(poll).Where(o => o.Votes.Count == best).ToList();
    }

    /// <summary>
    /// A date poll's one clear winner: the only future slot with the most votes, and at least
    /// <paramref name="minVotes"/> of them. Null on a tie, too few votes or no slot ahead.
    /// </summary>
    public static PollOption? ClearWinner(Poll poll, DateTimeOffset now, int minVotes)
    {
        var future = poll.Options.Where(o => o.ScheduledAt > now).ToList();
        if (future.Count == 0) return null;
        var best = future.Max(o => o.Votes.Count);
        if (best < minVotes) return null;
        var leaders = future.Where(o => o.Votes.Count == best).ToList();
        return leaders.Count == 1 ? leaders[0] : null;
    }

    /// <summary>Who voted for the slot at <paramref name="slot"/>, once each, in voting order.</summary>
    public static List<PollVote> VotersAt(Poll poll, DateTimeOffset slot) =>
        poll.Options.Where(o => o.ScheduledAt == slot)
            .SelectMany(o => o.Votes)
            .OrderBy(v => v.VotedAt)
            .DistinctBy(v => v.UserId)
            .ToList();

    /// <summary>The card's order: slots by date, text options as they were entered.</summary>
    public static List<PollOption> Ordered(Poll poll) =>
        poll.Kind == PollKind.Text
            ? poll.Options.OrderBy(o => o.Id).ToList()
            : poll.Options.OrderBy(o => o.ScheduledAt).ToList();
}
