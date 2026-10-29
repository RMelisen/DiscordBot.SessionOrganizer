namespace ProjectSYNCS.Helpers;

// Display only (the card's header): rounds store the question key, never its category, so a
// question can move between categories freely.
public enum QuizCategory { Tech, Games, Culture, Absurd, Syncs, Nature, Science, Geography, History, Food, Words, Mycology }

public enum QuizDifficulty { Easy, Medium, Hard }

/// <summary>One question of her pop quiz.</summary>
/// <param name="Answers">
/// The display answer first. For an open question, every other entry is an accepted
/// spelling (QuizAnswer compares them all). For multiple choice, just the right choice.
/// </param>
/// <param name="Choices">Exactly four, the right one first (shuffled at post time); null for an open question.</param>
/// <param name="Aside">Her comment under the revealed answer. Every question has one.</param>
public sealed record QuizQuestion(
    string Key,
    QuizCategory Category,
    QuizDifficulty Difficulty,
    string Prompt,
    IReadOnlyList<string> Answers,
    IReadOnlyList<string>? Choices,
    string Aside)
{
    public bool IsChoice => Choices is not null;
    public string Display => Answers[0];
}

// The questions of her pop quiz (QuizMasterService). Walked through by DailyRotation, keyed
// by how many rounds the guild has had, so nothing repeats until the whole bank is spent.
//
// Authoring rules:
//
// - **Keys are stored** (QuizRound.QuestionKey): append-only, never renamed. Adding or
//   removing a question reshuffles the walk from the next round on; that's fine. A key's
//   prefix is the category it was written under: moving it (culture.octopus is Nature now)
//   never renames it.
// - **Every fact is true**, as for MorningFunFacts: the joke is her aside, never an
//   invented answer. Many questions are adapted from those fun facts on purpose.
// - **An answer someone could find by spamming guesses is multiple choice**: years, small
//   numbers, yes/no, single letters. An open question needs an answer space too large to
//   brute-force: names, words, titles. List the spellings people will actually type.
// - Multiple choice: exactly four choices, the right one first, no two that could both be
//   defended as right. Keep each under ~70 characters (one card line).
// - **Every question has an aside**: her reaction once the answer is out, the part only she
//   could say. A bare fact restated is not an aside; give it a turn.
// - Prompts and asides follow docs/syncs-voice.md: `vous` to the server, never gender the
//   reader, at most one tag, at the end (dry lines carry none). SYNCS questions stay a small
//   share and keep Synthia and the breakdown out: the lore stays implicit everywhere else.
public static partial class QuizBank
{
    public static long RewardFor(QuizDifficulty difficulty) => difficulty switch
    {
        QuizDifficulty.Easy => 25,
        QuizDifficulty.Medium => 40,
        _ => 60,
    };

    public static string Label(QuizCategory category) => category switch
    {
        QuizCategory.Tech => "Tech",
        QuizCategory.Games => "Jeux vidéo",
        QuizCategory.Nature => "Nature & animaux",
        QuizCategory.Mycology => "Champignons",
        QuizCategory.Science => "Sciences & espace",
        QuizCategory.Geography => "Géographie",
        QuizCategory.History => "Histoire",
        QuizCategory.Food => "Cuisine",
        QuizCategory.Words => "Mots & langues",
        QuizCategory.Culture => "Arts & culture pop",
        QuizCategory.Absurd => "Absurde",
        QuizCategory.Syncs => "Spécial SYNCS",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
    };

    public static string Label(QuizDifficulty difficulty) => difficulty switch
    {
        QuizDifficulty.Easy => "facile",
        QuizDifficulty.Medium => "moyen",
        _ => "difficile",
    };

    public static QuizQuestion? ByKey(string key) =>
        All.FirstOrDefault(q => q.Key == key);

    private const QuizCategory Tech = QuizCategory.Tech;
    private const QuizCategory Games = QuizCategory.Games;
    private const QuizCategory Culture = QuizCategory.Culture;
    private const QuizCategory Absurd = QuizCategory.Absurd;
    private const QuizCategory Syncs = QuizCategory.Syncs;
    private const QuizCategory Nature = QuizCategory.Nature;
    private const QuizCategory Science = QuizCategory.Science;
    private const QuizCategory Geography = QuizCategory.Geography;
    private const QuizCategory History = QuizCategory.History;
    private const QuizCategory Food = QuizCategory.Food;
    private const QuizCategory Words = QuizCategory.Words;
    private const QuizCategory Mycology = QuizCategory.Mycology;
    private const QuizDifficulty Easy = QuizDifficulty.Easy;
    private const QuizDifficulty Medium = QuizDifficulty.Medium;
    private const QuizDifficulty Hard = QuizDifficulty.Hard;

    private static QuizQuestion Choice(string key, QuizCategory category, QuizDifficulty difficulty,
        string prompt, string[] choices, string aside) =>
        new(key, category, difficulty, prompt, new[] { choices[0] }, choices, aside);

    private static QuizQuestion Open(string key, QuizCategory category, QuizDifficulty difficulty,
        string prompt, string[] answers, string aside) =>
        new(key, category, difficulty, prompt, answers, null, aside);

    // One method per file, by category (QuizBank.TechGames.cs, .NatureScience.cs, .Culture.cs),
    // joined in this order. DailyRotation indexes into All, so the order is the rotation's.
    public static readonly IReadOnlyList<QuizQuestion> All = new[]
    {
        TechAndGames(), NatureAndScience(), CultureAndTheRest(),
    }.SelectMany(part => part).ToArray();
}
