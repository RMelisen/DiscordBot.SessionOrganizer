using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

/// <summary>
/// The /arcade cards, static and Context-free like every card builder here: the module, the
/// component handler and ArcadeRunner's timers all render through them. Each button carries its
/// own verb (`duel:accept:`, `duel:decline:`, `duel:pick:`…), so no two ever share an id.
/// </summary>
public static class ArcadeCards
{
    private static readonly Color DuelColor = new(0xB0, 0x3A, 0x2E);
    private static readonly Color HeistColor = new(0x2B, 0x2D, 0x42);

    // ---- Duel -------------------------------------------------------------------------------------

    private static string Players(DuelGame d) => $"<@{d.ChallengerId}> contre <@{d.OpponentId}>";

    private static string StakeLine(DuelGame d) =>
        $"Mise : **{PebbleEconomy.Cailloux(d.Stake)}** chacun · commission de l'arbitre : {Duel.CommissionShare:P0}";

    /// <param name="line">Her line for this stage, already picked and formatted.</param>
    public static Embed DuelEmbed(DuelGame d, string line, string? footer = null)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(line).AppendLine();
        sb.AppendLine(Players(d));
        sb.AppendLine(d.AgainstHer ? $"Mise : **{PebbleEconomy.Cailloux(d.Stake)}**" : StakeLine(d));
        if (d.Accepted && d.Round > 1)
            sb.AppendLine($"Manche **{d.Round}** sur {Duel.MaxRounds}");
        if (d.LastTie is { } tie)
            sb.AppendLine($"Dernière manche : {Duel.Emoji(tie.A)} contre {Duel.Emoji(tie.B)}, égalité.");

        var embed = new EmbedBuilder()
            .WithTitle("🗿📄✂️ Duel")
            .WithDescription(sb.ToString().TrimEnd())
            .WithColor(DuelColor);
        if (footer is not null) embed.WithFooter(footer);
        return embed.Build();
    }

    // The challenge, before it's accepted.
    public static MessageComponent DuelChallengeButtons(DuelGame d) => new ComponentBuilder()
        .WithButton("Accepter", $"duel:accept:{d.Id}", ButtonStyle.Success)
        .WithButton("Refuser", $"duel:decline:{d.Id}", ButtonStyle.Danger)
        .Build();

    // Once accepted: the moves are picked privately, from an ephemeral row.
    public static MessageComponent DuelPickButton(DuelGame d) => new ComponentBuilder()
        .WithButton("Choisir mon coup", $"duel:pick:{d.Id}", ButtonStyle.Primary)
        .Build();

    // The private row. The round rides in the id, so a row from a tied round can't count twice.
    public static MessageComponent DuelMoveButtons(DuelGame d) => new ComponentBuilder()
        .WithButton(Duel.Name(DuelMove.Rock), $"duel:rps:{d.Id}:{d.Round}:{(int)DuelMove.Rock}", ButtonStyle.Secondary, new Emoji(Duel.Emoji(DuelMove.Rock)))
        .WithButton(Duel.Name(DuelMove.Paper), $"duel:rps:{d.Id}:{d.Round}:{(int)DuelMove.Paper}", ButtonStyle.Secondary, new Emoji(Duel.Emoji(DuelMove.Paper)))
        .WithButton(Duel.Name(DuelMove.Scissors), $"duel:rps:{d.Id}:{d.Round}:{(int)DuelMove.Scissors}", ButtonStyle.Secondary, new Emoji(Duel.Emoji(DuelMove.Scissors)))
        .Build();

    // The reveal: both moves, then her line.
    public static Embed DuelResultEmbed(DuelGame d, DuelMove challengerMove, DuelMove opponentMove, string line, string outcome)
    {
        var description =
            $"<@{d.ChallengerId}> {Duel.Emoji(challengerMove)} **{Duel.Name(challengerMove)}**\n" +
            $"<@{d.OpponentId}> {Duel.Emoji(opponentMove)} **{Duel.Name(opponentMove)}**\n\n" +
            $"{line}\n\n{outcome}";
        return new EmbedBuilder()
            .WithTitle("🗿📄✂️ Duel — résultat")
            .WithDescription(description)
            .WithColor(DuelColor)
            .Build();
    }

    // An ended card: just the line, no buttons.
    public static Embed DuelClosedEmbed(DuelGame d, string line) => new EmbedBuilder()
        .WithTitle("🗿📄✂️ Duel")
        .WithDescription($"{line}\n\n{Players(d)}")
        .WithColor(Color.DarkGrey)
        .Build();

    // ---- Heist ------------------------------------------------------------------------------------

    public static Embed HeistEmbed(HeistGame h)
    {
        var line = h.Line;
        var crew = string.Join("\n", h.Crew.Select((id, i) => $"{i + 1}. <@{id}>"));
        var n = h.Crew.Count;
        var odds =
            $"Chances : **{Heist.SuccessChance(n):P0}** · butin par tête : **{PebbleEconomy.Cailloux(Heist.Payout(n))}**";
        return new EmbedBuilder()
            .WithTitle("🦘 Le casse de Quokka")
            .WithDescription(
                $"{line}\n\n" +
                $"Mise : **{PebbleEconomy.Cailloux(Heist.Stake)}** par tête, payée au départ.\n" +
                $"Départ <t:{h.EndsAt.ToUnixTimeSeconds()}:R>, ou quand <@{h.OrganizerId}> donne le signal.")
            .AddField($"L'équipe ({n}/{Heist.MaxCrew})", crew)
            .AddField("Si on y allait maintenant", odds)
            .WithFooter("Plus on est nombreux, plus ça passe — mais plus le butin se partage.")
            .WithColor(HeistColor)
            .Build();
    }

    public static MessageComponent HeistButtons(HeistGame h) => new ComponentBuilder()
        .WithButton("Rejoindre l'équipe", $"heist:join:{h.Id}", ButtonStyle.Success, disabled: h.Crew.Count >= Heist.MaxCrew)
        .WithButton("Me retirer", $"heist:leave:{h.Id}", ButtonStyle.Secondary)
        .WithButton("Lancer le casse", $"heist:go:{h.Id}", ButtonStyle.Danger)
        .Build();

    // The narration and the result: a line, then the crew.
    public static Embed HeistStoryEmbed(string story, IReadOnlyList<ulong> crew, bool? success = null)
    {
        var embed = new EmbedBuilder()
            .WithTitle("🦘 Le casse de Quokka")
            .WithDescription(story)
            .WithColor(success switch { true => Color.Green, false => Color.DarkRed, null => HeistColor });
        if (crew.Count > 0)
            embed.AddField("L'équipe", string.Join(" ", crew.Select(id => $"<@{id}>")));
        return embed.Build();
    }
}
