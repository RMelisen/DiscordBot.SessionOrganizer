using System.Collections.Concurrent;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The in-memory Plynling cooldowns — a singleton, like every gate here. Only petting is
// rationed this way: a restart lets people pet once more early, which costs nothing,
// since petting can never keep a Plynling alive. Anything that touches money or death
// (/work, self-freeze) is stored in the database instead.
public sealed class PlynlingCooldowns
{
    public CooldownGate<(ulong Petter, int PlynlingId)> Pet { get; } =
        new(PlynlingLife.PetCooldown, forget: TimeSpan.FromHours(8));

    // After /plynling abandon, no adoption for PlynlingLife.AbandonCooldown. In memory like
    // the pet gate: a restart clears it, which costs nothing at 30 minutes.
    private readonly ConcurrentDictionary<(ulong Guild, ulong User), DateTimeOffset> _abandoned = new();

    public void MarkAbandoned(ulong guildId, ulong userId, DateTimeOffset now)
    {
        foreach (var (key, until) in _abandoned)
            if (until <= now) _abandoned.TryRemove(key, out _);         // keep it from growing
        _abandoned[(guildId, userId)] = now + PlynlingLife.AbandonCooldown;
    }

    // When they may adopt again, or null when they already may.
    public DateTimeOffset? AdoptBlockedUntil(ulong guildId, ulong userId, DateTimeOffset now) =>
        _abandoned.TryGetValue((guildId, userId), out var until) && until > now ? until : null;
}
