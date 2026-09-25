using System.Collections.Concurrent;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// One /plynling play game in progress. Gate serialises the moves: a double click must not
// play twice, and exactly one move may be the one that finishes the game.
public sealed class PlynlingGameSession
{
    public PlynlingGameSession(string id, ulong guildId, ulong ownerId, int plynlingId, PlynlingGameState state, DateTimeOffset startedAt)
    {
        Id = id;
        GuildId = guildId;
        OwnerId = ownerId;
        PlynlingId = plynlingId;
        State = state;
        StartedAt = startedAt;
    }

    public string Id { get; }
    public ulong GuildId { get; }
    public ulong OwnerId { get; }
    public int PlynlingId { get; }
    public PlynlingGameState State { get; }
    public DateTimeOffset StartedAt { get; }
    public object Gate { get; } = new();
}

// The live games, by id — a singleton, so the secrets (the rock, the number) stay here and
// never travel in a custom-id. In memory like every other gate here: a restart ends games in
// progress, which costs nothing (the hour was already spent), and a game left alone expires.
public sealed class PlynlingPlayService
{
    public static readonly TimeSpan Expiry = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, PlynlingGameSession> _games = new();

    // Picks one of the three games at random and starts it.
    public PlynlingGameSession Start(ulong guildId, ulong ownerId, int plynlingId, DateTimeOffset now, Random rng)
    {
        foreach (var (id, game) in _games)
            if (now - game.StartedAt > Expiry) _games.TryRemove(id, out _);

        var kind = (PlynlingGame)rng.Next(3);
        var session = new PlynlingGameSession(Guid.NewGuid().ToString("N")[..12], guildId, ownerId, plynlingId,
            new PlynlingGameState(kind, rng), now);
        _games[session.Id] = session;
        return session;
    }

    // The game, or null when it is over, expired or never existed.
    public PlynlingGameSession? Get(string id, DateTimeOffset now)
    {
        if (!_games.TryGetValue(id, out var session)) return null;
        if (now - session.StartedAt <= Expiry) return session;
        _games.TryRemove(id, out _);
        return null;
    }

    public void End(string id) => _games.TryRemove(id, out _);
}
