using System.Text;
using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

/// <summary>
/// The cards of the newer /arcade games, static and Context-free like ArcadeCards: the module,
/// the handlers and ArcadeRunner's timers all render through them. Every button carries its own
/// verb, and the ones that repeat on a card (stakes, columns, cells, choices) carry their value
/// too, so no id ever appears twice on one message.
/// </summary>
public static class PartyCards
{
    private static readonly Color Party = new(0x6A, 0x4C, 0x93);

    private static string Cailloux(long n) => PebbleEconomy.Cailloux(n);

    // ---- Race -------------------------------------------------------------------------------------

    public static Embed RaceLobby(RaceGame r)
    {
        var runners = string.Join("\n", r.Runners.Select((x, i) =>
            $"**{i + 1}. {x.Name}** · à <@{x.OwnerId}> · forme {x.Strength}"));
        var pool = r.Bets.Values.Sum(b => b.Stake);
        var perRunner = string.Join(" · ", r.Runners.Select((_, i) =>
            $"{i + 1} : {Cailloux(r.Bets.Values.Where(b => b.Runner == i).Sum(b => b.Stake))}"));
        return new EmbedBuilder()
            .WithTitle("🏁 Course de Plynlings")
            .WithDescription($"{r.Line}\n\nDépart <t:{r.StartsAt.ToUnixTimeSeconds()}:R>. Choisis ton champion et ta mise.")
            .AddField("Sur la ligne de départ", runners)
            .AddField($"Paris : {r.Bets.Count} · cagnotte {Cailloux(pool)}", perRunner)
            .WithFooter($"Les gagnants se partagent la cagnotte, moins {Race.Commission:P0}. Le maître du vainqueur reçoit {Race.OwnerBonus} cailloux.")
            .WithColor(Party)
            .Build();
    }

    public static MessageComponent RaceLobbyComponents(RaceGame r)
    {
        var menu = new SelectMenuBuilder()
            .WithCustomId($"race:pick:{r.Id}")
            .WithPlaceholder("Parier sur…");
        for (var i = 0; i < r.Runners.Count; i++)
            menu.AddOption($"{i + 1}. {Trim(r.Runners[i].Name, 90)}", i.ToString());
        return new ComponentBuilder().WithSelectMenu(menu).Build();
    }

    public static MessageComponent RaceStakeButtons(RaceGame r, int runner)
    {
        var b = new ComponentBuilder();
        foreach (var stake in Race.Stakes)
            b.WithButton($"{stake}", $"race:bet:{r.Id}:{runner}:{stake}", ButtonStyle.Primary);
        return b.Build();
    }

    public static Embed RaceFrame(RaceGame r, double[] positions, string line, bool finished = false, int? winner = null)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < r.Runners.Count; i++)
        {
            var cell = (int)Math.Round(positions[i] * Race.Track);
            var bar = new string('·', cell) + "🍄" + new string('·', Race.Track - cell);
            sb.AppendLine($"`{bar}`🏁 **{i + 1}. {r.Runners[i].Name}**{(winner == i ? " 🏆" : "")}");
        }
        return new EmbedBuilder()
            .WithTitle(finished ? "🏁 Course de Plynlings — arrivée" : "🏁 Course de Plynlings — en cours")
            .WithDescription($"{line}\n\n{sb}")
            .WithColor(finished ? Color.Gold : Party)
            .Build();
    }

    // ---- Crash ------------------------------------------------------------------------------------

    public static Embed CrashLobby(CrashGame c) => new EmbedBuilder()
        .WithTitle("🚀 Crash")
        .WithDescription($"{c.Line}\n\nDécollage <t:{c.LobbyEndsAt.ToUnixTimeSeconds()}:R>. Mise, puis clique sur **Encaisser** " +
                         "avant que la fusée explose. Plus tu attends, plus ça rapporte.")
        .AddField($"À bord ({c.Bets.Count})", Bets(c.Bets))
        .WithColor(Party)
        .Build();

    public static MessageComponent CrashLobbyComponents(CrashGame c)
    {
        var b = new ComponentBuilder();
        foreach (var stake in Crash.Stakes)
            b.WithButton($"Miser {stake}", $"crash:bet:{c.Id}:{stake}", ButtonStyle.Primary, row: 0);
        b.WithButton("Décoller maintenant", $"crash:go:{c.Id}", ButtonStyle.Secondary, row: 1);
        return b.Build();
    }

    public static Embed CrashFlight(CrashGame c, double multiplier) => new EmbedBuilder()
        .WithTitle($"🚀 ×{multiplier:0.00}")
        .WithDescription("La fusée monte… **Encaisse** avant qu'elle explose !")
        .AddField("Encaissé", Cashed(c), inline: true)
        .AddField("Encore à bord", Aboard(c), inline: true)
        .WithColor(Color.Orange)
        .Build();

    public static MessageComponent CrashFlightComponents(CrashGame c) => new ComponentBuilder()
        .WithButton("Encaisser", $"crash:cash:{c.Id}", ButtonStyle.Success)
        .Build();

    public static Embed CrashBoom(CrashGame c, string line) => new EmbedBuilder()
        .WithTitle($"💥 Explosée à ×{c.CrashPoint:0.00}")
        .WithDescription(line)
        .AddField("Encaissé à temps", Cashed(c), inline: true)
        .AddField("Restés dans la fusée", Aboard(c), inline: true)
        .WithColor(Color.DarkRed)
        .Build();

    private static string Bets(IReadOnlyDictionary<ulong, long> bets) =>
        bets.Count == 0 ? "—" : string.Join("\n", bets.Take(20).Select(b => $"<@{b.Key}> · {Cailloux(b.Value)}"));

    private static string Cashed(CrashGame c) => c.CashedOut.Count == 0 ? "—"
        : string.Join("\n", c.CashedOut.Take(20).Select(x => $"<@{x.Key}> ×{x.Value:0.00} → {Cailloux(Crash.Payout(c.Bets[x.Key], x.Value))}"));

    private static string Aboard(CrashGame c)
    {
        var aboard = c.Bets.Keys.Where(k => !c.CashedOut.ContainsKey(k)).ToList();
        return aboard.Count == 0 ? "—" : string.Join("\n", aboard.Take(20).Select(k => $"<@{k}>"));
    }

    // ---- Blackjack --------------------------------------------------------------------------------

    public static Embed BlackjackTable(BlackjackGame g, bool reveal, string? result = null)
    {
        var dealer = reveal
            ? $"{Blackjack.Show(g.Dealer)} · **{Blackjack.Total(g.Dealer)}**"
            : $"`{g.Dealer[0]}` `🂠`";
        var embed = new EmbedBuilder()
            .WithTitle("🃏 Blackjack")
            .WithDescription($"{g.Line}\n\n**Moi** : {dealer}\n**<@{g.PlayerId}>** : {Blackjack.Show(g.Player)} · **{Blackjack.Total(g.Player)}**" +
                             (result is null ? "" : $"\n\n{result}"))
            .WithFooter($"Mise : {Cailloux(g.Stake)}{(g.Doubled ? " (doublée)" : "")} · je m'arrête à 17 · un blackjack paie 3 contre 2")
            .WithColor(result is null ? Party : Color.DarkGrey);
        return embed.Build();
    }

    public static MessageComponent BlackjackButtons(BlackjackGame g, bool over = false) => over
        ? new ComponentBuilder().Build()
        : new ComponentBuilder()
            .WithButton("Tirer", $"bj:hit:{g.Id}", ButtonStyle.Primary)
            .WithButton("Rester", $"bj:stand:{g.Id}", ButtonStyle.Secondary)
            .WithButton("Doubler", $"bj:double:{g.Id}", ButtonStyle.Success, disabled: g.Player.Count != 2 || g.Doubled)
            .Build();

    // ---- Bomb -------------------------------------------------------------------------------------

    public static Embed BombLobby(BombGame b) => new EmbedBuilder()
        .WithTitle("💣 Patate chaude")
        .WithDescription($"{b.Line}\n\nMise : **{Cailloux(Bomb.Stake)}** chacun. La bombe passe de main en main ; " +
                         $"celui ou celle qui la tient quand elle explose perd sa mise, les autres se la partagent.\n" +
                         $"Allumage <t:{b.LobbyEndsAt.ToUnixTimeSeconds()}:R>, à partir de {Bomb.MinPlayers} joueurs.")
        .AddField($"Joueurs ({b.Players.Count}/{Bomb.MaxPlayers})", b.Players.Count == 0 ? "—" : string.Join("\n", b.Players.Select(p => $"<@{p}>")))
        .WithColor(Party)
        .Build();

    public static MessageComponent BombLobbyComponents(BombGame b) => new ComponentBuilder()
        .WithButton("Jouer", $"bomb:join:{b.Id}", ButtonStyle.Success, disabled: b.Players.Count >= Bomb.MaxPlayers)
        .WithButton("Me retirer", $"bomb:leave:{b.Id}", ButtonStyle.Secondary)
        .WithButton("Allumer la mèche", $"bomb:go:{b.Id}", ButtonStyle.Danger)
        .Build();

    public static Embed BombLive(BombGame b, string line) => new EmbedBuilder()
        .WithTitle("💣 Patate chaude — la mèche brûle")
        .WithDescription($"{line}\n\nLa bombe est chez **<@{b.Holder}>**. Vite, passe-la !")
        .AddField("Joueurs", string.Join(" ", b.Players.Select(p => $"<@{p}>")))
        .WithFooter($"Passes : {b.Passes}")
        .WithColor(Color.Orange)
        .Build();

    public static MessageComponent BombLiveComponents(BombGame b) => new ComponentBuilder()
        .WithButton("Passer la bombe", $"bomb:pass:{b.Id}", ButtonStyle.Danger, new Emoji("💣"))
        .Build();

    public static Embed BombOver(string line, string details, bool exploded) => new EmbedBuilder()
        .WithTitle(exploded ? "💥 Patate chaude — BOUM" : "💣 Patate chaude")
        .WithDescription($"{line}\n\n{details}")
        .WithColor(exploded ? Color.DarkRed : Color.DarkGrey)
        .Build();

    // ---- Quote ------------------------------------------------------------------------------------

    public static Embed QuoteCard(QuoteRound q, string? result = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine(q.Line).AppendLine();
        sb.AppendLine(MessageFormat.Quote(q.Content, QuoteRules.MaxLength));
        sb.AppendLine($"-# Écrit <t:{q.WrittenAt.ToUnixTimeSeconds()}:D>");
        if (result is not null) sb.AppendLine().AppendLine(result).Append($"-# [Le message d'origine]({q.SourceLink})");
        var embed = new EmbedBuilder()
            .WithTitle("🗨️ Qui a dit ça ?")
            .WithDescription(sb.ToString().TrimEnd())
            .WithColor(result is null ? Party : Color.DarkGrey);
        // Never an empty footer: Discord refuses one.
        if (result is null) embed.WithFooter($"Premier qui trouve : {Cailloux(QuoteRules.Reward)}. Un seul essai par personne.");
        return embed.Build();
    }

    public static MessageComponent QuoteButtons(QuoteRound q, bool over = false)
    {
        var b = new ComponentBuilder();
        foreach (var (id, name) in q.Choices)
            b.WithButton(Trim(name, 80), $"quote:guess:{q.Id}:{id}",
                over && id == q.AuthorId ? ButtonStyle.Success : ButtonStyle.Secondary, disabled: over);
        return b.Build();
    }

    // ---- Temperature ------------------------------------------------------------------------------

    public static Embed TemperatureCard(TemperatureRound t) => new EmbedBuilder()
        .WithTitle("🌡️ Devine ma température")
        .WithDescription($"{t.Line}\n\nDevine la température de mon processeur, en °C. Mise : **{Cailloux(TemperatureRules.Stake)}**. " +
                         $"Le plus proche rafle la cagnotte (moins {TemperatureRules.Commission:P0}).\n" +
                         $"Résultat <t:{t.EndsAt.ToUnixTimeSeconds()}:R>.")
        .AddField($"Paris ({t.Guesses.Count})", t.Guesses.Count == 0 ? "—" : string.Join(" ", t.Guesses.Keys.Select(k => $"<@{k}>")))
        .WithFooter("Les réponses restent secrètes jusqu'au résultat.")
        .WithColor(Party)
        .Build();

    public static MessageComponent TemperatureButtons(TemperatureRound t) => new ComponentBuilder()
        .WithButton("Deviner", $"temp:open:{t.Id}", ButtonStyle.Primary, new Emoji("🌡️"))
        .Build();

    public static Embed TemperatureResult(TemperatureRound t, string line, double? actual, IReadOnlyCollection<ulong> winners)
    {
        var guesses = actual is { } a
            ? string.Join("\n", t.Guesses.OrderBy(g => Math.Abs(g.Value - a)).Take(20)
                .Select(g => $"{(winners.Contains(g.Key) ? "🏆 " : "")}<@{g.Key}> · {g.Value:0.#} °C"))
            : "—";
        return new EmbedBuilder()
            .WithTitle(actual is { } v ? $"🌡️ {v:0.#} °C" : "🌡️ Devine ma température")
            .WithDescription(line)
            .AddField("Les paris", guesses)
            .WithColor(Color.DarkGrey)
            .Build();
    }

    // ---- Boards -----------------------------------------------------------------------------------

    public static string BoardName(BoardKind kind) => kind == BoardKind.Connect4 ? "Puissance 4" : "Morpion";

    public static Embed BoardCard(BoardGame g, string? result = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine(g.Line).AppendLine();
        var (s1, s2) = g.Kind == BoardKind.Connect4 ? ("🔴", "🟡") : ("❌", "⭕");
        sb.AppendLine($"{s1} <@{g.Player1}> contre {s2} <@{g.Player2}>");
        if (g.Stake > 0) sb.AppendLine($"Mise : **{Cailloux(g.Stake)}**");
        if (g.Accepted && result is null) sb.AppendLine($"Au tour de <@{g.PlayerFor(g.Turn)}> {(g.Turn == 1 ? s1 : s2)}");
        if (g.Kind == BoardKind.Connect4 && g.Accepted) sb.AppendLine().AppendLine(Connect4.Render(g.Board));
        if (result is not null) sb.AppendLine().AppendLine(result);
        var embed = new EmbedBuilder()
            .WithTitle(g.Kind == BoardKind.Connect4 ? "🔴 Puissance 4" : "❌ Morpion")
            .WithDescription(sb.ToString().TrimEnd())
            .WithColor(result is null ? Party : Color.DarkGrey);
        if (g.Accepted && result is null) embed.WithFooter("5 minutes par coup, sinon c'est perdu.");
        return embed.Build();
    }

    public static MessageComponent BoardChallengeButtons(BoardGame g) => new ComponentBuilder()
        .WithButton("Accepter", $"board:accept:{g.Id}", ButtonStyle.Success)
        .WithButton("Refuser", $"board:decline:{g.Id}", ButtonStyle.Danger)
        .Build();

    public static MessageComponent BoardButtons(BoardGame g, bool over = false)
    {
        var b = new ComponentBuilder();
        if (g.Kind == BoardKind.Connect4)
        {
            for (var c = 0; c < Connect4.Cols; c++)
                b.WithButton($"{c + 1}", $"c4:drop:{g.Id}:{c}", ButtonStyle.Secondary,
                    disabled: over || !Connect4.CanDrop(g.Board, c), row: c < 5 ? 0 : 1);
            if (!over) b.WithButton("Abandonner", $"board:quit:{g.Id}", ButtonStyle.Danger, row: 1);
        }
        else
        {
            for (var i = 0; i < 9; i++)
                b.WithButton(null, $"ttt:play:{g.Id}:{i}", g.Board[i] == 0 ? ButtonStyle.Secondary : ButtonStyle.Primary,
                    new Emoji(TicTacToe.Symbol(g.Board[i])), disabled: over || g.Board[i] != 0, row: i / 3);
            if (!over) b.WithButton("Abandonner", $"board:quit:{g.Id}", ButtonStyle.Danger, row: 3);
        }
        return b.Build();
    }

    // ---- Quokka -----------------------------------------------------------------------------------

    public static Embed QuokkaCard(QuokkaVisit v, string? result = null) => new EmbedBuilder()
        .WithTitle("🦘 Quokka !")
        .WithDescription(result ?? $"{v.Line}\n\nLe premier qui le frappe gagne **{Cailloux(v.Reward)}**. Il repart dans deux minutes.")
        .WithColor(result is null ? Color.Orange : Color.DarkGrey)
        .Build();

    public static MessageComponent QuokkaButtons(QuokkaVisit v, bool over = false) => new ComponentBuilder()
        .WithButton("Frapper", $"quokka:hit:{v.Id}", ButtonStyle.Danger, new Emoji("🔨"), disabled: over)
        .Build();

    private static string Trim(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";
}
