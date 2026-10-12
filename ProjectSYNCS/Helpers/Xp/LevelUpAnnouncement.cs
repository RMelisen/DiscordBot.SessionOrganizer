using System.Text.RegularExpressions;
using Discord;

namespace ProjectSYNCS.Helpers;

// The level-up bot's "passage de niveau" announcement, recognised in one place
// because two services now disagree about that bot on purpose.
//
// `ChatterService` answers these with a grudging congratulation — the "bravo" is aimed
// at the *person* who levelled, while the sulking is about the level having been earned
// on a rival's system rather than hers. `RivalryService` therefore has to skip exactly
// these messages and nothing else: the same bot's other traffic is fair game, but here
// she has already had her say, and adding a reaction and a muttered line on top would
// be three responses to one announcement.
public static class LevelUpAnnouncement
{
    public const ulong BotId = 437808476106784770;

    private const string Phrase = "tu viens de passer au niveau";

    // Pulls the level number out of the announcement: the first run of digits that
    // follows the phrase (skips any markdown like ** in between).
    private static readonly Regex _levelNumber =
        new(Phrase + @"\D*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// True when this is the level-up bot announcing that someone gained a level.
    /// Both halves matter: the bot posts plenty of other things.
    /// </summary>
    public static bool Matches(IUser author, string? content) =>
        author.Id == BotId && TryReadLevel(content, out _);

    /// <summary>The announced level, as written (kept as text — "67" is an easter egg).</summary>
    public static bool TryReadLevel(string? content, out string level)
    {
        var match = _levelNumber.Match(content ?? string.Empty);
        level = match.Success ? match.Groups[1].Value : string.Empty;
        return match.Success;
    }
}
