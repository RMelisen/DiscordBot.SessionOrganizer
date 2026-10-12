using System.Collections.Concurrent;

namespace ProjectSYNCS.Services;

/// <summary>
/// Where a forwarded mention came from, so a DM reply can be routed back to it.
/// </summary>
public sealed record PendingMention(
    ulong GuildId, ulong ChannelId, ulong MessageId, string AuthorName);

// Tracks whether the bot's owner has flagged themselves as absent. State is kept
// in memory and resets to "available" on restart, by design. While the owner is
// absent, ChatterService intercepts messages that ping them and replies, in a
// formal tone, that they are unavailable — then DMs the owner the mention. This
// service also remembers where each of those DMs came from, so the owner can
// answer simply by replying to the DM.
public sealed class AvailabilityService
{
    // Rodhengard, the owner. The single source of truth for this id.
    public const ulong OwnerId = 345917214966415362;

    // volatile: written from the slash-command path, read from gateway handlers.
    private volatile bool _ownerAbsent;

    public bool IsOwnerAbsent => _ownerAbsent;

    public void SetOwnerAbsent(bool absent) => _ownerAbsent = absent;

    // Keyed by the id of the DM the bot sent the owner. Bounded so a long absence
    // can't grow it without limit; the oldest entries fall off first. Like the
    // absent flag, this is deliberately in-memory: a restart drops the ability to
    // reply to older notices, which is acceptable.
    private const int MaxTrackedMentions = 200;

    private readonly ConcurrentDictionary<ulong, PendingMention> _pendingMentions = new();
    private readonly ConcurrentQueue<ulong> _pendingOrder = new();

    public void RememberMention(ulong dmMessageId, PendingMention mention)
    {
        _pendingMentions[dmMessageId] = mention;
        _pendingOrder.Enqueue(dmMessageId);

        while (_pendingOrder.Count > MaxTrackedMentions && _pendingOrder.TryDequeue(out var oldest))
            _pendingMentions.TryRemove(oldest, out _);
    }

    // Entries are kept after use, so the owner can send several replies to the
    // same notice.
    public bool TryGetMention(ulong dmMessageId, out PendingMention mention) =>
        _pendingMentions.TryGetValue(dmMessageId, out mention!);
}
