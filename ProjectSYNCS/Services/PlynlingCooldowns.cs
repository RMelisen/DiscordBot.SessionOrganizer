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

    // One game an hour per Plynling, claimed when the game *starts*, so abandoning a game
    // never rolls a new one.
    public CooldownGate<int> Play { get; } = new(PlynlingLife.PlayCooldown, forget: TimeSpan.FromHours(2));

    // One visit a (Paris) day per pair of owners, whichever of the two knocked.
    private readonly ConcurrentDictionary<(ulong Low, ulong High, int Day), byte> _visits = new();

    public bool TryClaimVisit(ulong a, ulong b, int day)
    {
        foreach (var key in _visits.Keys)
            if (key.Day < day) _visits.TryRemove(key, out _);            // yesterday's are done with
        return _visits.TryAdd(Pair(a, b, day), 0);
    }

    public void ReleaseVisit(ulong a, ulong b, int day) => _visits.TryRemove(Pair(a, b, day), out _);

    public bool VisitedToday(ulong a, ulong b, int day) => _visits.ContainsKey(Pair(a, b, day));

    private static (ulong, ulong, int) Pair(ulong a, ulong b, int day) => (Math.Min(a, b), Math.Max(a, b), day);

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
