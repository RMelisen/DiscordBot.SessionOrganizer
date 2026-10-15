using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /teams: splits a voice channel into teams. Stateless — the « Remélanger » button carries the
// channel, the count and the mode, and re-reads who is in voice on every click
// (TeamsComponentHandler), so a restart breaks nothing.
[CommandContextType(InteractionContextType.Guild)]
public class TeamsModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly XpService _xp;
    private readonly ResponsePicker _picker;

    public TeamsModule(XpService xp, ResponsePicker picker)
    {
        _xp = xp;
        _picker = picker;
    }

    [SlashCommand("teams", "Répartir les gens d'un salon vocal en équipes")]
    public async Task TeamsAsync(
        [Summary("teams", "Combien d'équipes (2 par défaut)")]
        [MinValue(TeamDraw.MinTeams)] [MaxValue(TeamDraw.MaxTeams)] int teams = TeamDraw.MinTeams,
        [Summary("channel", "Le salon vocal à répartir (par défaut, le tien)")]
        [ChannelTypes(ChannelType.Voice, ChannelType.Stage)] IGuildChannel? channel = null,
        [Summary("mode", "Au hasard, ou équilibré selon les niveaux")]
        [Choice("Au hasard", (int)TeamMode.Random)]
        [Choice("Équilibré (niveaux)", (int)TeamMode.Balanced)] int mode = (int)TeamMode.Random)
    {
        var voice = channel as SocketVoiceChannel ?? (Context.User as SocketGuildUser)?.VoiceChannel;
        if (voice is null)
        {
            await RespondAsync("Tu n'es dans aucun salon vocal. Rejoins-en un, ou choisis le salon (option `channel`).", ephemeral: true);
            return;
        }

        var (embed, components, refusal) = await DrawAsync(voice, teams, (TeamMode)mode, _xp, _picker);
        if (refusal is not null)
        {
            await RespondAsync(refusal, ephemeral: true);
            return;
        }
        await RespondAsync(embed: embed, components: components, allowedMentions: AllowedMentions.None);
    }

    /// <summary>
    /// One draw, shared by the command and the reroll button: who is in voice now, the split, the
    /// card. Returns a refusal instead when there aren't enough people.
    /// </summary>
    public static async Task<(Embed? Embed, MessageComponent? Components, string? Refusal)> DrawAsync(
        SocketVoiceChannel voice, int teams, TeamMode mode, XpService xp, ResponsePicker picker)
    {
        // Humans only: other bots, and her own seat in voice, don't play.
        var people = voice.ConnectedUsers.Where(u => !u.IsBot).ToList();
        if (people.Count < teams)
            return (null, null, $"Il faut au moins {teams} personnes dans <#{voice.Id}> pour faire {teams} équipes. " +
                                $"Là, il y en a {people.Count}.");

        var levels = mode == TeamMode.Balanced
            ? await xp.GetLevelsAsync(voice.Guild.Id, people.Select(p => p.Id).ToList())
            : new Dictionary<ulong, int>();
        var split = TeamDraw.Split(people, teams, mode, p => levels.GetValueOrDefault(p.Id), Random.Shared);

        var embed = new EmbedBuilder()
            .WithTitle($"Équipes — {voice.Name}")
            .WithDescription(picker.Pick(BotResponses.TeamsDrawLines))
            .WithColor(Color.Teal);
        foreach (var team in split)
        {
            var name = picker.Pick(BotResponses.TeamNames);
            var members = string.Join("\n", team.Select(p => mode == TeamMode.Balanced
                ? $"<@{p.Id}> · niv. {levels.GetValueOrDefault(p.Id)}"
                : $"<@{p.Id}>"));
            var total = mode == TeamMode.Balanced ? $" · {team.Sum(p => levels.GetValueOrDefault(p.Id))} niv." : "";
            embed.AddField($"{name}{total}", members, inline: true);
        }
        embed.WithFooter(mode == TeamMode.Balanced ? "Équilibré selon les niveaux" : "Au hasard");

        var components = new ComponentBuilder()
            .WithButton("Remélanger", $"teams:roll:{voice.Id}:{teams}:{(int)mode}", ButtonStyle.Secondary, new Emoji("🔀"))
            .Build();
        return (embed.Build(), components, null);
    }
}
