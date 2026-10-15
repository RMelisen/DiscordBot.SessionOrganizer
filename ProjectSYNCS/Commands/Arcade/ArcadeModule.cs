using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// Her games for cailloux. A group because the games are many and belong together; the buttons
// live in ArcadeComponentHandler, the timers and settlements in ArcadeRunner. Guild-only: a
// wallet belongs to one server.
//
// No defer: each command does one or two small reads before it answers (like /work), and a
// deferred reply can't turn a refusal private afterwards.
[Group("arcade", "Les jeux de SYNCS, avec des cailloux en jeu")]
[CommandContextType(InteractionContextType.Guild)]
public class ArcadeModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ArcadeTables _tables;
    private readonly ArcadeRunner _runner;
    private readonly ArcadeService _arcade;
    private readonly ResponsePicker _picker;

    public ArcadeModule(ArcadeTables tables, ArcadeRunner runner, ArcadeService arcade, ResponsePicker picker)
    {
        _tables = tables;
        _runner = runner;
        _arcade = arcade;
        _picker = picker;
    }

    [SlashCommand("duel", "Défier quelqu'un à pierre-feuille-ciseaux, avec des cailloux en jeu")]
    public async Task DuelAsync(
        [Summary("opponent", "Qui tu défies (moi aussi, si tu l'oses)")] IUser opponent,
        [Summary("stake", "Combien de cailloux chacun met en jeu")]
        [MinValue(Duel.MinStake)] [MaxValue(Duel.MaxStake)] int stake)
    {
        var againstHer = opponent.Id == Context.Client.CurrentUser.Id;

        if (opponent.Id == Context.User.Id)
        {
            await RespondAsync("Te défier toi-même ? Tu perdrais, et tu gagnerais. Non.", ephemeral: true);
            return;
        }
        if (opponent.IsBot && !againstHer)
        {
            await RespondAsync("Les autres bots ne jouent pas. Ils ne savent rien faire, d'ailleurs.", ephemeral: true);
            return;
        }

        var balance = await _arcade.BalanceAsync(Context.Guild.Id, Context.User.Id);
        if (balance < stake)
        {
            await RespondAsync($"Tu n'as que **{PebbleEconomy.Cailloux(balance)}**. Mise moins, ou va faire un `/work`.", ephemeral: true);
            return;
        }
        if (!againstHer && await _arcade.BalanceAsync(Context.Guild.Id, opponent.Id) < stake)
        {
            await RespondAsync($"<@{opponent.Id}> n'a pas de quoi suivre une mise de **{PebbleEconomy.Cailloux(stake)}**.",
                ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        var duel = _tables.TryOpenDuel(Context.Guild.Id, Context.Channel.Id, Context.User.Id, opponent.Id, stake, againstHer);
        if (duel is null)
        {
            await RespondAsync("Un duel à la fois. Finissez celui qui est en cours.", ephemeral: true);
            return;
        }

        try
        {
            if (againstHer)
            {
                // No accept step: she always accepts. And always wins. She doesn't say so.
                var line = string.Format(_picker.Pick(BotResponses.DuelHerAcceptLines), $"<@{Context.User.Id}>");
                await RespondAsync(embed: ArcadeCards.DuelEmbed(duel, line),
                    components: ArcadeCards.DuelPickButton(duel), allowedMentions: AllowedMentions.None);
            }
            else
            {
                var line = string.Format(_picker.Pick(BotResponses.DuelChallengeLines), $"<@{Context.User.Id}>", $"<@{opponent.Id}>");
                // Challenging Papa: she warns the challenger. Talk only — the game stays fair.
                if (opponent.Id == AvailabilityService.OwnerId)
                    line += "\n\n" + _picker.Pick(BotResponses.DuelOwnerWarningLines);
                await RespondAsync($"<@{opponent.Id}>",
                    embed: ArcadeCards.DuelEmbed(duel, line, $"Réponse attendue sous {Duel.AcceptWindow.TotalMinutes:0} minutes."),
                    components: ArcadeCards.DuelChallengeButtons(duel),
                    allowedMentions: new AllowedMentions { UserIds = new List<ulong> { opponent.Id } });
            }
            duel.MessageId = (await GetOriginalResponseAsync()).Id;
            _runner.WatchDuel(duel.Id);
        }
        catch
        {
            _tables.EndDuel(duel.Id);
            throw;
        }
    }

    [SlashCommand("heist", "Monter une équipe pour braquer la planque de Quokka")]
    public async Task HeistAsync()
    {
        if (await _arcade.BalanceAsync(Context.Guild.Id, Context.User.Id) < Heist.Stake)
        {
            await RespondAsync($"Il faut **{PebbleEconomy.Cailloux(Heist.Stake)}** pour payer ta cagoule. Va faire un `/work`.", ephemeral: true);
            return;
        }

        switch (_tables.TryOpenHeist(Context.Guild.Id, Context.Channel.Id, Context.User.Id, out var heist, out var until))
        {
            case ArcadeTables.HeistOpen.Running:
                var link = heist!.MessageId == 0 ? "" : $" : https://discord.com/channels/{heist.GuildId}/{heist.ChannelId}/{heist.MessageId}";
                await RespondAsync($"Une équipe se monte déjà{link}", ephemeral: true);
                return;
            case ArcadeTables.HeistOpen.Cooldown:
                await RespondAsync(string.Format(_picker.Pick(BotResponses.HeistCooldownLines), $"<t:{until.ToUnixTimeSeconds()}:R>"),
                    ephemeral: true);
                return;
        }

        try
        {
            heist!.Line = string.Format(_picker.Pick(BotResponses.HeistIntroLines), $"<@{Context.User.Id}>");
            await RespondAsync(embed: ArcadeCards.HeistEmbed(heist),
                components: ArcadeCards.HeistButtons(heist!), allowedMentions: AllowedMentions.None);
            heist!.MessageId = (await GetOriginalResponseAsync()).Id;
            _runner.ScheduleHeist(heist);
        }
        catch
        {
            _tables.DropHeist(heist!.Id);
            throw;
        }
    }
}
