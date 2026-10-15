using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The state of each newer /arcade game, held by ArcadeTables. Touch it only inside
// ArcadeTables.With (one lock for all of them: they are small and change a click at a time).

public sealed record RaceRunner(int PlynlingId, string Name, ulong OwnerId, int Strength);

public sealed class RaceGame : ArcadeGame
{
    public required ulong OrganizerId { get; init; }
    public required DateTimeOffset StartsAt { get; init; }
    public required IReadOnlyList<RaceRunner> Runners { get; init; }
    public Dictionary<ulong, (int Runner, long Stake)> Bets { get; } = new();
    public string Line { get; set; } = string.Empty;
    public bool Started { get; set; }
}

public enum CrashPhase { Lobby, Flying }

public sealed class CrashGame : ArcadeGame
{
    public required ulong OrganizerId { get; init; }
    public required DateTimeOffset LobbyEndsAt { get; init; }
    public required double CrashPoint { get; init; }
    public Dictionary<ulong, long> Bets { get; } = new();
    public Dictionary<ulong, double> CashedOut { get; } = new();
    public CrashPhase Phase { get; set; }
    public DateTimeOffset LaunchedAt { get; set; }
    public string Line { get; set; } = string.Empty;
}

public sealed class BlackjackGame : ArcadeGame
{
    public required ulong PlayerId { get; init; }
    public long Stake { get; set; }
    public List<Card> Player { get; } = new();
    public List<Card> Dealer { get; } = new();
    public bool Doubled { get; set; }
    public DateTimeOffset LastActionAt { get; set; } = DateTimeOffset.UtcNow;
    public string Line { get; set; } = string.Empty;
}

public sealed class BombGame : ArcadeGame
{
    public required ulong OrganizerId { get; init; }
    public required DateTimeOffset LobbyEndsAt { get; init; }
    public List<ulong> Players { get; } = new();
    public bool Lit { get; set; }
    public ulong Holder { get; set; }
    public int Passes { get; set; }
    public string Line { get; set; } = string.Empty;
}

public sealed class QuoteRound : ArcadeGame
{
    public required ulong AuthorId { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset WrittenAt { get; init; }
    public required string SourceLink { get; init; }
    public required IReadOnlyList<(ulong Id, string Name)> Choices { get; init; }
    public HashSet<ulong> Out { get; } = new();
    public string Line { get; set; } = string.Empty;
}

public sealed class TemperatureRound : ArcadeGame
{
    public required ulong OrganizerId { get; init; }
    public required DateTimeOffset EndsAt { get; init; }
    public Dictionary<ulong, double> Guesses { get; } = new();
    public string Line { get; set; } = string.Empty;
}

public sealed class BoardGame : ArcadeGame
{
    public required BoardKind Kind { get; init; }
    public required ulong Player1 { get; init; }
    public required ulong Player2 { get; init; }
    public required bool AgainstHer { get; init; }
    public required long Stake { get; init; }
    public required int[] Board { get; init; }
    public bool Accepted { get; set; }
    public int Turn { get; set; } = 1;
    public int Moves { get; set; }
    public DateTimeOffset LastMoveAt { get; set; } = DateTimeOffset.UtcNow;

    // Puissance 4 against her: one game in five, she drops two pieces once. She won't mention it.
    public bool CheatPending { get; set; }
    public string Line { get; set; } = string.Empty;

    public ulong PlayerFor(int turn) => turn == 1 ? Player1 : Player2;
    public bool IsPlayer(ulong id) => id == Player1 || id == Player2;
}

public sealed class QuokkaVisit : ArcadeGame
{
    public required long Reward { get; init; }
    public string Line { get; set; } = string.Empty;
}
