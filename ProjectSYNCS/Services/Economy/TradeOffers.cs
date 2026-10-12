using System.Collections.Concurrent;

namespace ProjectSYNCS.Services;

// One /inventory trade offer: `FromId` gives GiveQty × GiveKey to `ToId` for WantQty × WantKey.
public sealed record TradeOffer(
    string Id, ulong GuildId, ulong FromId, ulong ToId,
    string GiveKey, int GiveQty, string WantKey, int WantQty, DateTimeOffset ExpiresAt);

// The open trade offers — a singleton, in memory like every other pending thing here (visit
// invitations, games): a restart drops them, and their buttons then say the offer is gone.
// Nothing moves until the swap, which re-checks both sides, so a lost offer costs nothing.
//
// One open offer per proposer: a new one replaces the old, whose buttons then find nothing.
// Taking an offer removes it atomically, so two clicks on « Accepter » cannot both swap.
public sealed class TradeOffers
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    private const string Alphabet = "abcdefghijkmnpqrstuvwxyz23456789";   // no 0/o, 1/l
    private readonly ConcurrentDictionary<string, TradeOffer> _offers = new();

    public TradeOffer Open(ulong guildId, ulong fromId, ulong toId, string giveKey, int giveQty, string wantKey, int wantQty,
        DateTimeOffset now, Random rng)
    {
        Sweep(now);
        foreach (var old in _offers.Values.Where(o => o.GuildId == guildId && o.FromId == fromId))
            _offers.TryRemove(old.Id, out _);

        while (true)
        {
            var id = new string(Enumerable.Range(0, 6).Select(_ => Alphabet[rng.Next(Alphabet.Length)]).ToArray());
            var offer = new TradeOffer(id, guildId, fromId, toId, giveKey, giveQty, wantKey, wantQty, now + Lifetime);
            if (_offers.TryAdd(id, offer)) return offer;
        }
    }

    // The offer if it is still open; an expired one is dropped and reads as gone.
    public TradeOffer? Get(string id, DateTimeOffset now)
    {
        if (!_offers.TryGetValue(id, out var offer)) return null;
        if (offer.ExpiresAt > now) return offer;
        _offers.TryRemove(id, out _);
        return null;
    }

    // Removes and returns it — only one caller ever gets a given offer.
    public TradeOffer? Take(string id, DateTimeOffset now) =>
        _offers.TryRemove(id, out var offer) && offer.ExpiresAt > now ? offer : null;

    // Puts back an offer taken for a swap that could not happen yet (the recipient lacked the
    // items and may still get them) — unless its proposer opened another one meanwhile.
    public void Restore(TradeOffer offer)
    {
        if (_offers.Values.Any(o => o.GuildId == offer.GuildId && o.FromId == offer.FromId)) return;
        _offers.TryAdd(offer.Id, offer);
    }

    private void Sweep(DateTimeOffset now)
    {
        foreach (var offer in _offers.Values.Where(o => o.ExpiresAt <= now))
            _offers.TryRemove(offer.Id, out _);
    }
}
