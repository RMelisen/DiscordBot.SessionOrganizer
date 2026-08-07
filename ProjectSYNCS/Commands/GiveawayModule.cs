using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;
using System.Text;

namespace ProjectSYNCS.Commands;

[Group("giveaway", "Tirages au sort")]
// Guild-only: this module reads Context.Guild, which is null in a DM, and
// config.yaml ships register_globally: true (a global command is DM-enabled by
// default). Without this it is reachable somewhere it can only throw.
[CommandContextType(InteractionContextType.Guild)]
public class GiveawayModule : InteractionModuleBase<SocketInteractionContext>
{
    private const int MaxWinners = 10;

    // How many entrants the card lists before summarising the rest. Bounded by the
    // 1024-character embed field limit, and by not wanting the card to be a wall.
    private const int MaxEntrantsShown = 20;

    private readonly GiveawayService _giveaways;

    public GiveawayModule(GiveawayService giveaways)
    {
        _giveaways = giveaways;
    }

    // ---- Creating ---------------------------------------------------------

    // One shot rather than a wizard: unlike /poll and /vote there is no
    // variable-length list to assemble, so there is no draft to keep anywhere.
    //
    // The duration is a fixed choice list rather than free text — nothing to parse,
    // nothing to reject, and no error message to write. Discord allows 25 choices, so
    // there is room to add more.
    [SlashCommand("create", "Lancer un tirage au sort")]
    public async Task CreateAsync(
        [Summary("lot", "Ce qu'il y a à gagner")] string prize,
        [Summary("duree", "Combien de temps le tirage reste ouvert")]
        [Choice("10 minutes", 10)]
        [Choice("30 minutes", 30)]
        [Choice("1 heure", 60)]
        [Choice("6 heures", 360)]
        [Choice("12 heures", 720)]
        [Choice("24 heures", 1440)]
        [Choice("48 heures", 2880)]
        [Choice("7 jours", 10080)]
        int durationMinutes,
        [Summary("description", "Détails, conditions, ce que tu veux")] string? description = null,
        [Summary("gagnants", "Combien de gagnants tirer (1 par défaut)")]
        [MinValue(1)] [MaxValue(MaxWinners)] int winners = 1)
    {
        await DeferAsync();

        var giveaway = await _giveaways.CreateAsync(
            Context.Guild.Id,
            Context.Channel.Id,
            Context.User.Id,
            prize.Trim(),
            description?.Trim() ?? string.Empty,
            winners,
            DateTimeOffset.UtcNow.AddMinutes(durationMinutes));

        var message = await FollowupAsync(
            embed: BuildEmbed(giveaway),
            components: BuildComponents(giveaway));

        // The card has to know its own message so the sweep can edit it in place.
        await _giveaways.SetMessageLocationAsync(giveaway.Id, Context.Channel.Id, message.Id);
    }

    // ---- Listing + republishing -------------------------------------------

    [SlashCommand("list", "Lister les tirages en cours du serveur")]
    public async Task ListAsync()
    {
        await DeferAsync(ephemeral: true);

        var giveaways = await _giveaways.GetActiveAsync(Context.Guild.Id);
        if (giveaways.Count == 0)
        {
            await FollowupAsync("Aucun tirage en cours pour le moment.", ephemeral: true);
            return;
        }

        var sb = new StringBuilder();
        foreach (var g in giveaways)
        {
            sb.AppendLine(
                $"**#{g.Id}** 🎉 {g.Prize} — 🎟️ {g.Entries.Count} participant(s) — fin {Relative(g.EndsAt)}");
        }

        var embed = new EmbedBuilder()
            .WithTitle($"Tirages en cours ({giveaways.Count})")
            .WithDescription(sb.ToString())
            .WithColor(Color.Gold)
            .Build();

        // Up to 25 options in a select menu; the list embed still shows all.
        var menu = new SelectMenuBuilder()
            .WithCustomId("giveaway:republish")
            .WithPlaceholder("Republier un tirage dans ce salon");

        foreach (var g in giveaways.Take(25))
        {
            var label = $"#{g.Id} — {g.Prize}";
            if (label.Length > 100) label = label[..100];
            menu.AddOption(label, g.Id.ToString());
        }

        var components = new ComponentBuilder().WithSelectMenu(menu).Build();
        await FollowupAsync(embed: embed, components: components, ephemeral: true);
    }

    [ComponentInteraction("giveaway:republish", ignoreGroupNames: true)]
    public async Task OnRepublishAsync(string[] values)
    {
        await DeferAsync(ephemeral: true);

        if (!int.TryParse(values[0], out var giveawayId))
        {
            await FollowupAsync("ID de tirage invalide.", ephemeral: true);
            return;
        }

        var giveaway = await _giveaways.GetAsync(giveawayId);
        if (giveaway is null || giveaway.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Tirage introuvable.", ephemeral: true);
            return;
        }

        if (giveaway.IsClosed)
        {
            await FollowupAsync("Ce tirage est terminé.", ephemeral: true);
            return;
        }

        await DeleteCardAsync(giveaway);

        var message = await Context.Channel.SendMessageAsync(
            embed: BuildEmbed(giveaway),
            components: BuildComponents(giveaway));
        await _giveaways.SetMessageLocationAsync(giveaway.Id, Context.Channel.Id, message.Id);

        await FollowupAsync($"Tirage **#{giveawayId}** republié ici.", ephemeral: true);
    }

    [SlashCommand("delete", "Supprimer un tirage que tu as lancé")]
    public async Task DeleteAsync(
        [Summary("giveaway-id", "L'ID affiché dans le pied du tirage")] int giveawayId)
    {
        await DeferAsync(ephemeral: true);

        var giveaway = await _giveaways.GetAsync(giveawayId);
        if (giveaway is null || giveaway.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Tirage introuvable.", ephemeral: true);
            return;
        }

        if (!SessionPermissions.CanManage(Context.User, giveaway))
        {
            await FollowupAsync(
                "Seul l'organisateur ou un administrateur peut supprimer ce tirage.", ephemeral: true);
            return;
        }

        await DeleteCardAsync(giveaway);
        await _giveaways.DeleteAsync(giveawayId);

        await FollowupAsync($"Tirage **#{giveawayId}** supprimé.", ephemeral: true);
    }

    // Removes the published card if it is still there, so a republish doesn't leave
    // two live cards for one giveaway and a delete doesn't leave an orphan.
    private async Task DeleteCardAsync(Giveaway giveaway)
    {
        if (giveaway.MessageId == 0) return;

        var channel = Context.Guild.GetTextChannel(giveaway.ChannelId);
        if (channel is null) return;

        try
        {
            var old = await channel.GetMessageAsync(giveaway.MessageId);
            if (old is not null) await old.DeleteAsync();
        }
        catch { /* already gone — ignore */ }
    }

    // ---- Rendering --------------------------------------------------------
    //
    // static and shared, like PollModule's pair: the button handler and the draw sweep
    // both re-render through these, so a change to the card follows everywhere.

    public static Embed BuildEmbed(Giveaway giveaway)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"🎉 {giveaway.Prize}")
            .WithColor(giveaway.IsClosed ? Color.DarkGrey : Color.Gold)
            .WithFooter($"ID #{giveaway.Id} · {WinnerCountLabel(giveaway.WinnerCount)}");

        if (giveaway.Description.Length > 0)
            embed.WithDescription(giveaway.Description);

        if (!giveaway.IsClosed)
        {
            // Both forms of the same instant: the client renders each in the reader's
            // own locale and timezone, so nothing here is formatted server-side.
            embed.AddField("Fin", $"{Relative(giveaway.EndsAt)} ({Absolute(giveaway.EndsAt)})");
            AddEntrantsField(embed, giveaway);
            return embed.Build();
        }

        var winners = giveaway.Entries.Where(e => e.IsWinner).ToList();

        embed.AddField("Terminé", Absolute(giveaway.EndsAt));
        AddEntrantsField(embed, giveaway);
        embed.AddField(
            winners.Count > 1 ? "Gagnants" : "Gagnant",
            winners.Count == 0
                ? "Personne n'a participé. Gênant."
                : string.Join("\n", winners.Select(w => $"🏆 <@{w.UserId}>")));

        return embed.Build();
    }

    // The entrant roster, like a session card's. Mentions render as names and do *not*
    // ping from inside an embed, which is why no AllowedMentions is needed here.
    //
    // Capped, unlike the session card: an embed field holds 1024 characters and a
    // mention costs ~23 with its newline, so an unbounded list throws at send time once
    // a giveaway gets popular — and a giveaway is exactly the thing that attracts a
    // crowd, where a session is a handful of people.
    private static void AddEntrantsField(EmbedBuilder embed, Giveaway giveaway)
    {
        var entrants = giveaway.Entries.OrderBy(e => e.EnteredAt).ToList();

        if (entrants.Count == 0)
        {
            embed.AddField("Participants", "Personne pour l'instant. Soyez le premier ✨");
            return;
        }

        var shown = entrants.Take(MaxEntrantsShown).Select(e => $"<@{e.UserId}>").ToList();
        var text = string.Join("\n", shown);

        var hidden = entrants.Count - shown.Count;
        if (hidden > 0) text += $"\n… et {hidden} autre(s)";

        embed.AddField($"Participants ({entrants.Count})", text);
    }

    public static MessageComponent BuildComponents(Giveaway giveaway)
    {
        // Nothing to click once it is drawn — the card carries the result. Same choice
        // the session card makes when a session is cancelled or already started, rather
        // than leaving a disabled button behind.
        if (giveaway.IsClosed) return new ComponentBuilder().Build();

        return new ComponentBuilder()
            .WithButton("Participer", $"giveaway:enter:{giveaway.Id}",
                ButtonStyle.Success, new Emoji("🎉"))
            .WithButton("Ne plus participer", $"giveaway:leave:{giveaway.Id}",
                ButtonStyle.Danger, new Emoji("✖️"))
            .Build();
    }

    private static string WinnerCountLabel(int count) =>
        count > 1 ? $"{count} gagnants" : "1 gagnant";

    private static string Relative(DateTimeOffset instant) =>
        TimestampTag.FromDateTimeOffset(instant, TimestampTagStyles.Relative).ToString();

    private static string Absolute(DateTimeOffset instant) =>
        TimestampTag.FromDateTimeOffset(instant, TimestampTagStyles.ShortDateTime).ToString();
}
