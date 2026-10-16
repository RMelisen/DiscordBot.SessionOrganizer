using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>One message she found nothing specific to answer. No author: only what was said.</summary>
public readonly record struct UnansweredMessage(DateTimeOffset At, ulong GuildId, ChatPath Path, string Kind, string Text);

/// <summary>
/// The last <see cref="Capacity"/> messages aimed at her that got no specific answer
/// (AnswerKinds.IsUnrecognised), for `/debug answers view: Examples`: the raw material for the next
/// intents. Singleton, in memory, lost on restart by design.
/// </summary>
/// <remarks>
/// Deliberately keeps no author id or name, at the owner's request: the point is what people say to
/// her, not who said it. The text is kept as written (mentions included), newlines collapsed and
/// capped at <see cref="MaxTextLength"/>.
/// </remarks>
public sealed class UnansweredLog
{
    public const int Capacity = 100;
    public const int MaxTextLength = 200;

    private readonly object _lock = new();
    private readonly Queue<UnansweredMessage> _messages = new();

    public void Add(ulong guildId, ChatPath path, string kind, string text, DateTimeOffset at)
    {
        var clean = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (clean.Length == 0) return;
        if (clean.Length > MaxTextLength) clean = clean[..(MaxTextLength - 1)] + "…";

        lock (_lock)
        {
            _messages.Enqueue(new UnansweredMessage(at, guildId, path, kind, clean));
            while (_messages.Count > Capacity) _messages.Dequeue();
        }
    }

    /// <summary>This guild's messages, newest first.</summary>
    public List<UnansweredMessage> For(ulong guildId)
    {
        lock (_lock)
            return _messages.Where(m => m.GuildId == guildId).Reverse().ToList();
    }
}
