namespace ProjectSYNCS.Helpers;

// /plynling play's six mini-games, all played with buttons. One is drawn at random when a game
// starts. In memory only (never stored), so this enum may change freely.
public enum PlynlingGame { HideAndSeek, RockPaperScissors, PebbleBag, Memory, LastPebble, TreasureHunt }

public enum GameStatus { Playing, Won, Lost }

public enum RpsThrow { Rock, Paper, Scissors }

public enum RpsResult { Win, Lose, Tie }

// What a dig in the treasure hunt says, by Manhattan distance to the gland: 0, 1, 2, 3+.
public enum DigHint { Found, Burning, Warm, Cold }

/// <summary>
/// One game in progress, and every rule of all six games. Pure: no Discord and no clock,
/// and every draw takes the <see cref="Random"/> it should use, so each round and each outcome
/// is checkable. The live games are held by PlynlingPlayService — the secrets (the rock, the
/// gland, the next pebble) never travel in a custom-id.
/// </summary>
public sealed class PlynlingGameState
{
    public const int Rocks = 3;
    public const int HideRounds = 2;       // found once in two rounds wins; a find ends it
    public const int RpsToWin = 2;         // first to two; a tie is replayed

    public const int BagMin = 1;
    public const int BagMax = 10;
    public const int BagToWin = 3;         // three right calls in a row; a tie is drawn again

    public const int MemoryLength = 4;     // mushrooms to remember
    public const int MemoryOptions = 6;    // buttons to pick them from: the four and two decoys

    public const int NimMin = 12;
    public const int NimMax = 16;
    public const int NimMaxTake = 3;
    public const double NimSlip = 0.25;    // how often the Plynling misses its winning move

    public const int GridSize = 4;
    public const int Digs = 3;

    public PlynlingGame Game { get; }
    public GameStatus Status { get; private set; } = GameStatus.Playing;
    // Moves made so far. Cards whose buttons depend on what was showing carry it in their ids, so a
    // second click from the same (stale) card is ignored instead of played blind.
    public int Step { get; private set; }

    // Cache-cache: the round (1 or 2), the rock it hides behind now, and the last rock picked.
    public int Round { get; private set; } = 1;
    public int HiddenBehind { get; private set; }
    public int? LastPick { get; private set; }

    // Pierre-papier-ciseaux.
    public int PlayerScore { get; private set; }
    public int PlynlingScore { get; private set; }
    public RpsThrow? LastPlayerThrow { get; private set; }
    public RpsThrow? LastPlynlingThrow { get; private set; }
    public RpsResult? LastResult { get; private set; }

    // Le sac de cailloux: the pebble showing, the streak, every pebble drawn (oldest first).
    public int Pebble { get; private set; }
    public int Streak { get; private set; }
    public bool? LastRight { get; private set; }
    private readonly List<int> _drawn = new();
    public IReadOnlyList<int> Drawn => _drawn;

    // Mémoire: item keys of the mushrooms to remember, and of the buttons to pick them from.
    public IReadOnlyList<string> Sequence { get; } = Array.Empty<string>();
    public IReadOnlyList<string> Options { get; } = Array.Empty<string>();
    public bool Memorised { get; private set; }
    public int Progress { get; private set; }
    public int? WrongSlot { get; private set; }

    // Le dernier caillou: the pile, the player's last take and the Plynling's answer to it.
    public int Pile { get; private set; }
    public int? LastTake { get; private set; }
    public int? PlynlingTake { get; private set; }

    // Chasse au trésor: the gland's cell (row × GridSize + column) and every dug cell's hint.
    public int Treasure { get; }
    private readonly Dictionary<int, DigHint> _dug = new();
    public IReadOnlyDictionary<int, DigHint> Dug => _dug;
    public int? LastDig { get; private set; }

    /// <param name="mushrooms">Mémoire only: the item keys it may draw from (at least
    /// <see cref="MemoryOptions"/>, each with its emoji loaded).</param>
    public PlynlingGameState(PlynlingGame game, Random rng, IReadOnlyList<string>? mushrooms = null)
    {
        Game = game;
        switch (game)
        {
            case PlynlingGame.HideAndSeek:
                HiddenBehind = rng.Next(Rocks);
                break;
            case PlynlingGame.PebbleBag:
                Pebble = rng.Next(BagMin, BagMax + 1);
                _drawn.Add(Pebble);
                break;
            case PlynlingGame.Memory:
                if (mushrooms is null || mushrooms.Distinct().Count() < MemoryOptions)
                    throw new ArgumentException($"Mémoire needs at least {MemoryOptions} distinct mushrooms.", nameof(mushrooms));
                var drawn = mushrooms.Distinct().OrderBy(_ => rng.Next()).Take(MemoryOptions).ToList();
                Sequence = drawn.Take(MemoryLength).ToList();
                Options = drawn.OrderBy(_ => rng.Next()).ToList();
                break;
            case PlynlingGame.LastPebble:
                Pile = rng.Next(NimMin, NimMax + 1);
                break;
            case PlynlingGame.TreasureHunt:
                Treasure = rng.Next(GridSize * GridSize);
                break;
        }
    }

    /// <summary>Looks behind a rock. True when it was there — the game is then won.</summary>
    public bool Hide(int rock, Random rng)
    {
        Expect(PlynlingGame.HideAndSeek);
        Step++;
        LastPick = rock;
        if (rock == HiddenBehind)
        {
            Status = GameStatus.Won;
            return true;
        }
        if (Round >= HideRounds)
        {
            Status = GameStatus.Lost;                  // HiddenBehind stays: the reveal says where
        }
        else
        {
            Round++;
            HiddenBehind = rng.Next(Rocks);            // it hides again, anywhere
        }
        return false;
    }

    public RpsResult Throw(RpsThrow player, Random rng)
    {
        Expect(PlynlingGame.RockPaperScissors);
        Step++;
        var mine = (RpsThrow)rng.Next(3);
        var result = Beats(player, mine) ? RpsResult.Win : player == mine ? RpsResult.Tie : RpsResult.Lose;
        if (result == RpsResult.Win) PlayerScore++;
        if (result == RpsResult.Lose) PlynlingScore++;
        LastPlayerThrow = player;
        LastPlynlingThrow = mine;
        LastResult = result;
        if (PlayerScore >= RpsToWin) Status = GameStatus.Won;
        else if (PlynlingScore >= RpsToWin) Status = GameStatus.Lost;
        return result;
    }

    public static bool Beats(RpsThrow a, RpsThrow b) =>
        (a, b) is (RpsThrow.Rock, RpsThrow.Scissors) or (RpsThrow.Paper, RpsThrow.Rock) or (RpsThrow.Scissors, RpsThrow.Paper);

    /// <summary>
    /// Calls the next pebble higher or lower, then draws it — again until it differs, so a tie
    /// never counts. True when the call was right.
    /// </summary>
    public bool Bag(bool higher, Random rng)
    {
        Expect(PlynlingGame.PebbleBag);
        Step++;
        int next;
        do next = rng.Next(BagMin, BagMax + 1); while (next == Pebble);
        var right = higher ? next > Pebble : next < Pebble;
        Pebble = next;
        _drawn.Add(next);
        LastRight = right;
        if (!right) Status = GameStatus.Lost;
        else if (++Streak >= BagToWin) Status = GameStatus.Won;
        return right;
    }

    /// <summary>« J'ai retenu ! »: the sequence hides and the picks open.</summary>
    public void Memorise()
    {
        Expect(PlynlingGame.Memory);
        Step++;
        Memorised = true;
    }

    /// <summary>Picks the option in <paramref name="slot"/>. True when it is the next of the sequence.</summary>
    public bool Pick(int slot)
    {
        Expect(PlynlingGame.Memory);
        if (!Memorised) throw new InvalidOperationException("The sequence is still showing.");
        if (slot < 0 || slot >= Options.Count) throw new ArgumentOutOfRangeException(nameof(slot));
        // A mushroom already found: a double click from the card not yet redrawn. Nothing happens.
        if (Sequence.Take(Progress).Contains(Options[slot])) return true;
        Step++;
        if (Options[slot] != Sequence[Progress])
        {
            WrongSlot = slot;
            Status = GameStatus.Lost;
            return false;
        }
        if (++Progress >= MemoryLength) Status = GameStatus.Won;
        return true;
    }

    /// <summary>The take that leaves a multiple of 4 (the winning move), or 0 when there is none.</summary>
    public static int NimBest(int pile) => pile % (NimMaxTake + 1);

    /// <summary>
    /// The player takes <paramref name="n"/> pebbles; unless that was the last, the Plynling
    /// answers in the same move. Whoever takes the last pebble wins.
    /// </summary>
    public void Take(int n, Random rng)
    {
        Expect(PlynlingGame.LastPebble);
        if (n < 1 || n > Math.Min(NimMaxTake, Pile)) throw new ArgumentOutOfRangeException(nameof(n));
        Step++;
        Pile -= n;
        LastTake = n;
        PlynlingTake = null;
        if (Pile == 0)
        {
            Status = GameStatus.Won;
            return;
        }
        var best = NimBest(Pile);
        var take = best > 0 && rng.NextDouble() >= NimSlip ? best : rng.Next(1, Math.Min(NimMaxTake, Pile) + 1);
        Pile -= take;
        PlynlingTake = take;
        if (Pile == 0) Status = GameStatus.Lost;
    }

    public static DigHint HintFor(int cell, int treasure)
    {
        var distance = Math.Abs(cell / GridSize - treasure / GridSize) + Math.Abs(cell % GridSize - treasure % GridSize);
        return distance switch
        {
            0 => DigHint.Found,
            1 => DigHint.Burning,
            2 => DigHint.Warm,
            _ => DigHint.Cold,
        };
    }

    /// <summary>
    /// Digs a cell. True when the gland was there. A cell already dug changes nothing (a double
    /// click, an old card); three misses lose.
    /// </summary>
    public bool Dig(int cell)
    {
        Expect(PlynlingGame.TreasureHunt);
        if (cell < 0 || cell >= GridSize * GridSize) throw new ArgumentOutOfRangeException(nameof(cell));
        if (_dug.ContainsKey(cell)) return false;
        Step++;
        var hint = HintFor(cell, Treasure);
        _dug[cell] = hint;
        LastDig = cell;
        if (hint == DigHint.Found)
        {
            Status = GameStatus.Won;
            return true;
        }
        if (_dug.Count >= Digs) Status = GameStatus.Lost;
        return false;
    }

    private void Expect(PlynlingGame game)
    {
        if (Game != game) throw new InvalidOperationException($"This is a {Game} game, not {game}.");
        if (Status != GameStatus.Playing) throw new InvalidOperationException("This game is over.");
    }
}
