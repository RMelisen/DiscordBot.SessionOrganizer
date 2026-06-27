namespace ProjectSYNCS.Models;

// Per-guild usage counter for a single emote. Both custom emotes (from any
// server) and standard unicode emojis are tracked; counts are scoped to the
// guild they were used in.
//   - Custom emote: EmoteId != 0, Unicode == "" (rendered as <:name:id>).
//   - Unicode emoji: EmoteId == 0, Unicode holds the emoji (e.g. "❤️").
public class EmoteStat
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong EmoteId { get; set; }

    // The raw unicode emoji, empty for custom emotes.
    public string Unicode { get; set; } = string.Empty;

    // Last-seen name and animated flag, used to re-render custom emote markup.
    public string Name { get; set; } = string.Empty;
    public bool IsAnimated { get; set; }

    // Times the emote was written in a message, vs used as a reaction.
    public long WrittenCount { get; set; }
    public long ReactedCount { get; set; }
}
