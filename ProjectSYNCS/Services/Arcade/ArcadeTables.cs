using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// A duel in progress. Mutable, guarded by ArcadeTables' lock.
public sealed class DuelGame
{
    public required int Id { get; init; }
    public required ulong GuildId { get; init; }
    public required ulong ChannelId { get; init; }
    public required ulong ChallengerId { get; init; }
    public required ulong OpponentId { get; init; }
    public required long Stake { get; init; }

    // Against her: no accept step, and she always wins.
    public required bool AgainstHer { get; init; }

    public ulong MessageId { get; set; }
    public bool Accepted { get; set; }
    public int Round { get; set; } = 1;
    public DateTimeOffset PhaseStartedAt { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<ulong, DuelMove> Picks { get; } = new();

    // The moves of the last tied round, shown on the card.
    public (DuelMove A, DuelMove B)? LastTie { get; set; }

    // Her line for the current stage, kept so a repost of the card says the same thing.
    public string Line { get; set; } = string.Empty;

    // Every step reposts the card at the bottom of the channel (ArcadeRunner.RepostDuelAsync):
    // one repost at a time, or two quick clicks could delete the newer card and keep the older.
    public SemaphoreSlim CardGate { get; } = new(1, 1);

    public bool IsPlayer(ulong userId) => userId == ChallengerId || userId == OpponentId;
}

// A heist crew forming. Mutable, guarded by ArcadeTables' lock.
public sealed class HeistGame
{
    public required int Id { get; init; }
    public required ulong GuildId { get; init; }
    public required ulong ChannelId { get; init; }
    public required ulong OrganizerId { get; init; }
    public required DateTimeOffset EndsAt { get; init; }

    public ulong MessageId { get; set; }
    public List<ulong> Crew { get; } = new();

    // Her intro line, kept so every redraw of the lobby says the same thing.
    public string Line { get; set; } = string.Empty;

    // Set once, by whoever launches it: the timer or the organizer's button.
    public bool Launched { get; set; }
}

// What every newer /arcade game shares: where its card is. Each game's own class (RaceGame,
// CrashGame…) lives beside the code that runs it and adds its state. Mutable, guarded by
// ArcadeTables' lock: touch a game's state only inside ArcadeTables.With.
public abstract class ArcadeGame
{
    public int Id { get; set; }
    public required ulong GuildId { get; init; }
    public required ulong ChannelId { get; init; }
    public ulong MessageId { get; set; }

    // Set once, by whoever ends it (a click, a timer): the other one then does nothing.
    public bool Done { get; set; }
}

/// <summary>
/// The /arcade games running right now — in memory by design: a restart drops them, their buttons
/// then say so, and since settlement only happens at the end, no cailloux moved. Singleton; owns
/// its lock (the games are small and touched by a click at a time). Only state lives here: the
/// module and the handler run the games and their timers.
/// </summary>
public sealed class ArcadeTables
{
    private readonly object _lock = new();
    private int _nextId;

    private readonly Dictionary<int, DuelGame> _duels = new();
    private readonly Dictionary<int, HeistGame> _heists = new();
    private readonly Dictionary<ulong, DateTimeOffset> _heistCooldownUntil = new();
    private readonly Dictionary<int, ArcadeGame> _games = new();

    // ---- Every other game (race, crash, blackjack, bomb, quote, temperature, boards, Quokka) --------

    /// <summary>
    /// Opens <paramref name="game"/> unless <paramref name="conflict"/> finds a running game that
    /// stands in its way (one race per guild, one blackjack hand per person…), which is returned
    /// instead. The check and the add are one step under the lock.
    /// </summary>
    public (T? Opened, ArcadeGame? Blocking) TryOpen<T>(T game, Func<ArcadeGame, bool> conflict) where T : ArcadeGame
    {
        lock (_lock)
        {
            var blocking = _games.Values.FirstOrDefault(g => !g.Done && conflict(g));
            if (blocking is not null) return (null, blocking);
            game.Id = ++_nextId;
            _games[game.Id] = game;
            return (game, null);
        }
    }

    /// <summary>Runs <paramref name="action"/> on the game under the lock; null when it's gone or of another kind.</summary>
    public TR With<T, TR>(int id, Func<T?, TR> action) where T : ArcadeGame
    {
        lock (_lock) return action(_games.GetValueOrDefault(id) as T);
    }

    /// <summary>Marks the game done and drops it; true for the one caller who did.</summary>
    public bool End(int id)
    {
        lock (_lock)
        {
            if (!_games.Remove(id, out var game) || game.Done) return false;
            game.Done = true;
            return true;
        }
    }

    public IReadOnlyList<T> All<T>() where T : ArcadeGame
    {
        lock (_lock) return _games.Values.OfType<T>().Where(g => !g.Done).ToList();
    }

    // ---- Duels ------------------------------------------------------------------------------------

    /// <summary>Opens a duel unless either player already has one running.</summary>
    public DuelGame? TryOpenDuel(ulong guildId, ulong channelId, ulong challengerId, ulong opponentId, long stake, bool againstHer)
    {
        lock (_lock)
        {
            if (_duels.Values.Any(d => d.IsPlayer(challengerId) || (!againstHer && d.IsPlayer(opponentId))))
                return null;
            var duel = new DuelGame
            {
                Id = ++_nextId,
                GuildId = guildId,
                ChannelId = channelId,
                ChallengerId = challengerId,
                OpponentId = opponentId,
                Stake = stake,
                AgainstHer = againstHer,
                Accepted = againstHer,
            };
            _duels[duel.Id] = duel;
            return duel;
        }
    }

    public DuelGame? GetDuel(int id)
    {
        lock (_lock) return _duels.GetValueOrDefault(id);
    }

    // Runs `action` on the duel under the lock, so a check and its change are one step.
    public T WithDuel<T>(int id, Func<DuelGame?, T> action)
    {
        lock (_lock) return action(_duels.GetValueOrDefault(id));
    }

    /// <summary>Removes the duel; true for the one caller who removed it.</summary>
    public bool EndDuel(int id)
    {
        lock (_lock) return _duels.Remove(id);
    }

    // ---- Heists -----------------------------------------------------------------------------------

    public enum HeistOpen { Opened, Running, Cooldown }

    /// <summary>
    /// Opens the guild's heist, with the organizer as its first member, unless one is running or
    /// Quokka is still on alert. <paramref name="until"/> is when the cooldown ends.
    /// </summary>
    public HeistOpen TryOpenHeist(ulong guildId, ulong channelId, ulong organizerId, out HeistGame? heist, out DateTimeOffset until)
    {
        lock (_lock)
        {
            heist = _heists.Values.FirstOrDefault(h => h.GuildId == guildId);
            until = _heistCooldownUntil.GetValueOrDefault(guildId);
            if (heist is not null) return HeistOpen.Running;
            if (until > DateTimeOffset.UtcNow) return HeistOpen.Cooldown;

            heist = new HeistGame
            {
                Id = ++_nextId,
                GuildId = guildId,
                ChannelId = channelId,
                OrganizerId = organizerId,
                EndsAt = DateTimeOffset.UtcNow + Heist.LobbyTime,
            };
            heist.Crew.Add(organizerId);
            _heists[heist.Id] = heist;
            return HeistOpen.Opened;
        }
    }

    public T WithHeist<T>(int id, Func<HeistGame?, T> action)
    {
        lock (_lock) return action(_heists.GetValueOrDefault(id));
    }

    /// <summary>
    /// Marks the heist launched and returns its crew; null if it's gone or already launched (the
    /// organizer's button and the timer can race — one of them wins).
    /// </summary>
    public IReadOnlyList<ulong>? TryLaunchHeist(int id)
    {
        lock (_lock)
        {
            if (!_heists.TryGetValue(id, out var heist) || heist.Launched) return null;
            heist.Launched = true;
            return heist.Crew.ToList();
        }
    }

    // The heist is over: it leaves the table and Quokka goes on alert.
    public void EndHeist(int id)
    {
        lock (_lock)
        {
            if (!_heists.Remove(id, out var heist)) return;
            _heistCooldownUntil[heist.GuildId] = DateTimeOffset.UtcNow + Heist.Cooldown;
        }
    }

    // A heist that never went ahead (its card couldn't be posted): no cooldown.
    public void DropHeist(int id)
    {
        lock (_lock) _heists.Remove(id);
    }
}
