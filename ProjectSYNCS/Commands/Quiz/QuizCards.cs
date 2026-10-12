using System.Globalization;
using System.Text;
using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The pop quiz card and the /quiz leaderboard card. Static and Context-free, so their size
// is checkable without a gateway; QuizMasterService posts and closes the round card,
// QuizModule renders the board.
//
// Components V2 both, so: no content and no embeds, the flag re-asserted on every edit, and
// AllowedMentions.None on every send — the winner's <@id> is a TextDisplay, which genuinely
// pings otherwise.
public static class QuizCards
{
    /// <summary>How many names the board shows.</summary>
    public const int BoardSize = 10;

    private static readonly string[] Letters = { "A", "B", "C", "D" };

    /// <summary>
    /// The round's card, open or closed. <paramref name="intro"/> is her line under the
    /// heading while it is open; a closed card replaces it with the outcome and the answer.
    /// </summary>
    /// <remarks>
    /// Budget: container 1 + heading 1 + intro/outcome 1 + separator 1 + question 1 +
    /// footer 1, plus a row of 4 buttons on a multiple-choice card (5) = 11 of 40.
    /// </remarks>
    public static MessageComponent BuildRound(QuizRound round, QuizQuestion question, string? intro)
    {
        var open = !round.Closed;

        var container = new ContainerBuilder()
            .WithAccentColor(AccentFor(question.Difficulty))
            .AddComponent(new TextDisplayBuilder(
                $"### 🧠 Quiz · {QuizBank.Label(question.Category)}\n"
                + $"-# {Capitalize(QuizBank.Label(question.Difficulty))} · {PebbleEconomy.Cailloux(round.Reward)} au premier qui trouve"));

        // Discord rejects an empty TextDisplay, so an open card without an intro skips it.
        var lead = open ? intro : Outcome(round);
        if (!string.IsNullOrWhiteSpace(lead)) container.AddComponent(new TextDisplayBuilder(lead));

        container
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder(QuestionText(round, question, open)));

        container.AddComponent(new TextDisplayBuilder(open
            ? (question.IsChoice
                ? $"-# Une seule tentative par personne · fermeture <t:{round.ClosesAt.ToUnixTimeSeconds()}:R>"
                : $"-# Réponds directement dans le salon · fermeture <t:{round.ClosesAt.ToUnixTimeSeconds()}:R>")
            : Aside(question)));

        var builder = new ComponentBuilderV2().AddComponent(container);
        if (question.IsChoice) builder.AddComponent(Buttons(round, open));
        return builder.Build();
    }

    /// <summary>Position of the right answer on the card, from the stored shuffle.</summary>
    public static int CorrectPosition(QuizRound round) => round.ChoiceOrder.IndexOf('0');

    private static string QuestionText(QuizRound round, QuizQuestion question, bool open)
    {
        var sb = new StringBuilder($"**{question.Prompt}**");
        if (!question.IsChoice) return sb.ToString();

        var correct = CorrectPosition(round);
        for (var i = 0; i < round.ChoiceOrder.Length; i++)
        {
            var choice = question.Choices![round.ChoiceOrder[i] - '0'];
            var line = $"**{Letters[i]}.** {choice}";
            // Once closed, the right one stands out and the others step back.
            sb.Append('\n').Append(open ? line : i == correct ? $"✅ {line}" : $"-# {Letters[i]}. {choice}");
        }
        return sb.ToString();
    }

    private static string Outcome(QuizRound round)
    {
        if (round.WinnerId == 0) return "⌛ **Personne n'a trouvé.**";

        var seconds = round.WonAt is { } won
            ? (won - round.PostedAt).TotalSeconds.ToString("0.0", CultureInfo.GetCultureInfo("fr-FR"))
            : null;
        return $"🏆 <@{round.WinnerId}> a trouvé"
               + (seconds is null ? "" : $" en {seconds} s")
               + $" · +{PebbleEconomy.Cailloux(round.Reward)}";
    }

    private static string Aside(QuizQuestion question) =>
        $"Réponse : **{question.Display}**\n-# {question.Aside}";

    // One row, one verb, four distinct ids (quiz:pick:{round}:{position}). Closed, every
    // button is disabled and the right one turns green.
    private static ActionRowBuilder Buttons(QuizRound round, bool open)
    {
        var correct = CorrectPosition(round);
        var row = new ActionRowBuilder();
        for (var i = 0; i < round.ChoiceOrder.Length; i++)
        {
            var style = !open && i == correct ? ButtonStyle.Success : open ? ButtonStyle.Primary : ButtonStyle.Secondary;
            row.WithButton(Letters[i], $"quiz:pick:{round.Id}:{i}", style, disabled: !open);
        }
        return row;
    }

    /// <summary>
    /// The /quiz leaderboard card. Budget: container 1 + heading 1 + separator 1 + board 1
    /// + your line 1, then the period row 1 + 3 buttons = 9 of 40.
    /// </summary>
    public static MessageComponent BuildBoard(IReadOnlyList<QuizTally> board, StatsPeriod period, ulong viewerId)
    {
        var container = new ContainerBuilder()
            .WithAccentColor(Color.Gold)
            .AddComponent(new TextDisplayBuilder($"# 🧠 Les cerveaux du serveur\n-# {StatsPeriodUi.Label(period)}"))
            .AddComponent(new SeparatorBuilder());

        if (board.Count == 0)
        {
            container.AddComponent(new TextDisplayBuilder(
                "*Personne n'a encore gagné de quiz sur cette période. Le savoir se fait attendre (ᵕ • ᴗ •)*"));
        }
        else
        {
            container.AddComponent(new TextDisplayBuilder(string.Join("\n",
                board.Take(BoardSize).Select((t, i) => Row(t, i + 1, period)))));

            var mine = board.Select((t, i) => (Tally: t, Rank: i + 1)).FirstOrDefault(x => x.Tally.UserId == viewerId);
            if (mine.Rank > BoardSize)
                container.AddComponent(new TextDisplayBuilder($"-# Toi : {Row(mine.Tally, mine.Rank, period)}"));
        }

        return new ComponentBuilderV2()
            .AddComponent(container)
            .AddComponent(new ActionRowBuilder().AddFilterRow("quiz:win", period))
            .Build();
    }

    private static string Row(QuizTally tally, int rank, StatsPeriod period)
    {
        var line = $"{LevelCardUi.RankMarker(rank)} <@{tally.UserId}> — **{tally.Wins}** {(tally.Wins == 1 ? "victoire" : "victoires")}";
        if (period == StatsPeriod.AllTime && tally.BestMs > 0)
            line += $" · record {(tally.BestMs / 1000.0).ToString("0.0", CultureInfo.GetCultureInfo("fr-FR"))} s";
        return line;
    }

    private static Color AccentFor(QuizDifficulty difficulty) => difficulty switch
    {
        QuizDifficulty.Easy => Color.Green,
        QuizDifficulty.Medium => Color.Orange,
        _ => Color.Red,
    };

    private static string Capitalize(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
