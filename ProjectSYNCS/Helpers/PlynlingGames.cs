namespace ProjectSYNCS.Helpers;

// /plynling play's three mini-games. Each is picked at random when a game starts.
public enum PlynlingGame { HideAndSeek, RockPaperScissors, HigherLower }

public enum GameStatus { Playing, Won, Lost }

public enum RpsThrow { Rock, Paper, Scissors }

public enum RpsResult { Win, Lose, Tie }

// What the Plynling answers a guess: the number it thinks of is higher, lower, or that.
public enum GuessHint { Higher, Lower, Correct }

/// <summary>
/// One game in progress, and every rule of all three games. Pure: no Discord and no clock,
/// and every draw takes the <see cref="Random"/> it should use, so each round and each outcome
/// is checkable. The live games are held by PlynlingPlayService — the secrets (the rock, the
/// number) never travel in a custom-id.
/// </summary>
public sealed class PlynlingGameState
{
    public const int Rocks = 3;
    public const int HideRounds = 2;       // found once in two rounds wins; a find ends it
    public const int RpsToWin = 2;         // first to two; a tie is replayed
    public const int GuessMin = 1;
    public const int GuessMax = 100;
    public const int GuessTries = 6;

    public PlynlingGame Game { get; }
    public GameStatus Status { get; private set; } = GameStatus.Playing;

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

    // Plus ou moins.
    public int Secret { get; private set; }
    public int TriesLeft { get; private set; } = GuessTries;
    public int? LastGuess { get; private set; }
    public GuessHint? LastHint { get; private set; }

    public PlynlingGameState(PlynlingGame game, Random rng)
    {
        Game = game;
        if (game == PlynlingGame.HideAndSeek) HiddenBehind = rng.Next(Rocks);
        if (game == PlynlingGame.HigherLower) Secret = rng.Next(GuessMin, GuessMax + 1);
    }

    /// <summary>Looks behind a rock. True when it was there — the game is then won.</summary>
    public bool Hide(int rock, Random rng)
    {
        Expect(PlynlingGame.HideAndSeek);
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

    /// <summary>A guess from 1 to 100 (the modal enforces the range).</summary>
    public GuessHint Guess(int n)
    {
        Expect(PlynlingGame.HigherLower);
        TriesLeft--;
        LastGuess = n;
        var hint = n < Secret ? GuessHint.Higher : n > Secret ? GuessHint.Lower : GuessHint.Correct;
        LastHint = hint;
        if (hint == GuessHint.Correct) Status = GameStatus.Won;
        else if (TriesLeft <= 0) Status = GameStatus.Lost;
        return hint;
    }

    private void Expect(PlynlingGame game)
    {
        if (Game != game) throw new InvalidOperationException($"This is a {Game} game, not {game}.");
        if (Status != GameStatus.Playing) throw new InvalidOperationException("This game is over.");
    }
}
