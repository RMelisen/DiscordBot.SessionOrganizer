namespace ProjectSYNCS.Models;

// One /confess. Only the author is kept, not the text: the text lives in the posted message, and
// the row exists so the owner alone can answer « who wrote #12 ? » (/debug confession) when a
// confession is abused. Never shown to anyone else.
public class AnonymousConfession
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }

    /// <summary>The number shown on the post, counted per guild from 1.</summary>
    public int Number { get; set; }

    public ulong AuthorId { get; set; }

    /// <summary>Where it was posted. Zero until the post went out.</summary>
    public ulong ChannelId { get; set; }

    /// <summary>The posted message. Zero until the post went out.</summary>
    public ulong MessageId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
