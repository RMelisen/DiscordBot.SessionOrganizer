using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

// Which way a chat message reached her (ChatterService): a reply to one of her messages, an
// @mention, or her name said untagged.
public enum ChatPath
{
    Reply,
    Mention,
    Name,
}

/// <summary>
/// Every kind of chat answer she gives, as the string key `/debug answers` counts it by
/// (ChatAnswerStat). Pure: the keys live here and nowhere else.
/// </summary>
/// <remarks>
/// <b>Stored keys, so append-only</b> (root CLAUDE.md, Data): renaming one orphans its rows. The
/// builders put an enum's <i>name</i> in the key, so ChatIntent, ReplyContext and ChatFact may be
/// reordered but never renamed.
/// </remarks>
public static class AnswerKinds
{
    public const string Nice = "nice";
    public const string Greeting = "greeting";
    public const string Reference = "reference";
    public const string Tata = "tata";
    public const string Comeback = "comeback";
    public const string PapaComeback = "papa:comeback";
    public const string PapaMean = "papa:mean";
    public const string PapaGreeting = "papa:greeting";
    public const string Interrogation = "interrogation";
    public const string TalkedAbout = "talked-about";
    public const string PapaTalkedAbout = "papa:talked-about";
    public const string Backbite = "backbite";
    public const string Rescue = "rescue";
    public const string Shutdown = "shutdown";
    public const string Inabot = "inabot";
    public const string Breakdown = "breakdown";
    public const string StreakNudge = "streak:nudge";
    public const string StreakEnd = "streak:end";
    public const string StreakSulk = "streak:sulk";
    public const string PapaRaspberry = "papa:pet-raspberry";
    public const string PapaChild = "papa:pet-child";
    // Told « chut » in that channel: a sulk reaction instead of words (ChatHush).
    public const string Hushed = "hushed";

    internal static string Intent(ChatIntent intent) => "intent:" + intent;
    public static string Context(ReplyContext context) => "context:" + context;
    public static string Fact(ChatFact fact) => "fact:" + fact;

    public static string Key(ChatPath path) => path switch
    {
        ChatPath.Reply => "reply",
        ChatPath.Mention => "mention",
        _ => "name",
    };

    /// <summary>
    /// Whether this answer means she found nothing specific to say: the plain fallbacks, and the
    /// fact and nudge lines that stood in for one. These are the messages worth reading to find
    /// what she should learn next (UnansweredLog).
    /// </summary>
    public static bool IsUnrecognised(string kind) =>
        kind is Reference or Tata or Comeback or PapaComeback or Interrogation or TalkedAbout or PapaTalkedAbout
            or StreakNudge
        || kind.StartsWith("fact:", StringComparison.Ordinal);
}
