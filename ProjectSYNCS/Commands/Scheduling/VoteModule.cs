using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Interactions.Autocomplete;
using ProjectSYNCS.Interactions.Modals;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;
using System.Text;

namespace ProjectSYNCS.Commands;

[Group("vote", "Votes à choix multiples (jeux, films, etc.)")]
// Guild-only: this module reads Context.Guild, which is null in a DM, and
// config.yaml ships register_globally: true (a global command is DM-enabled by
// default). Without this it is reachable somewhere it can only throw.
[CommandContextType(InteractionContextType.Guild)]
public class VoteModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PollService _pollService;
    private readonly ILogger<VoteModule> _logger;

    // Options being assembled before publishing, keyed by user (same approach as
    // the date-based PollModule wizard). Thirty minutes untouched and it's gone.
    private static readonly DraftBox<VoteDraft> _drafts = new(TimeSpan.FromMinutes(30));

    private sealed class VoteDraft
    {
        public string Title { get; set; } = string.Empty;
        public List<string> Options { get; } = new();
        public PollSettings Settings { get; init; } = PollSettings.Default;
    }

    public VoteModule(PollService pollService, ILogger<VoteModule> logger)
    {
        _pollService = pollService;
        _logger = logger;
    }

    // ---- Entry: the slash command owns the wizard message ----------------
    // The wizard message is created here (by the slash command) and only ever
    // *updated* afterwards. Creating it from a modal response instead caused the
    // first option-add update to be dropped.

    [SlashCommand("create", "Proposer plusieurs options (texte) et laisser voter")]
    public async Task VoteCreateAsync(
        [Summary("ends", "Quand le vote se termine (2 jours si rien)"),
         Choice("dans 1 h", 1), Choice("dans 3 h", 3), Choice("dans 6 h", 6), Choice("dans 12 h", 12),
         Choice("dans 1 jour", 24), Choice("dans 2 jours", 48), Choice("dans 3 jours", 72), Choice("dans 1 semaine", 168)] int ends = 48,
        [Summary("single", "Une seule option par personne")] bool single = false,
        [Summary("anonymous", "Montrer les comptes, jamais les noms")] bool anonymous = false)
    {
        _drafts.Set(Context.User.Id, new VoteDraft { Settings = new PollSettings(TimeSpan.FromHours(ends), single, anonymous) });
        await RespondAsync(
            "**Nouveau vote**\nClique sur **Définir le vote** : un titre, puis les options, une par ligne.",
            components: new ComponentBuilder()
                .WithButton("Définir le vote", "vote:begin", ButtonStyle.Primary, new Emoji("📝"))
                .Build(),
            ephemeral: true);
    }

    [ComponentInteraction("vote:begin", ignoreGroupNames: true)]
    public async Task OnBeginAsync()
    {
        await RespondWithModalAsync<VoteStartModal>("vote:start");
    }

    [ModalInteraction("vote:start", ignoreGroupNames: true)]
    public async Task OnVoteStartAsync(VoteStartModal modal)
    {
        if (!_drafts.TryGet(Context.User.Id, out var draft))
            draft = new VoteDraft();
        draft.Title = modal.PollTitle.Trim();
        draft.Options.Clear();
        draft.Options.AddRange(PollRules.ParseOptions(modal.Options));
        _drafts.Set(Context.User.Id, draft);

        var component = (SocketModal)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = BuildDraftSummary(draft);
            msg.Components = BuildReviewStep(draft);
        });
    }

    [ComponentInteraction("vote:add", ignoreGroupNames: true)]
    public async Task OnAddOptionAsync()
    {
        await RespondWithModalAsync<VoteOptionModal>("vote:addoption");
    }

    [ModalInteraction("vote:addoption", ignoreGroupNames: true)]
    public async Task OnOptionSubmittedAsync(VoteOptionModal modal)
    {
        if (!_drafts.TryGet(Context.User.Id, out var draft))
        {
            await RespondAsync("Ce vote a expiré. Relance `/vote create`.", ephemeral: true);
            return;
        }

        var label = modal.OptionLabel.Trim();
        if (label.Length > PollRules.MaxLabelLength) label = label[..PollRules.MaxLabelLength];

        if (label.Length > 0
            && draft.Options.Count < PollRules.MaxOptions
            && !draft.Options.Contains(label, StringComparer.OrdinalIgnoreCase))
        {
            draft.Options.Add(label);
        }

        var component = (SocketModal)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = BuildDraftSummary(draft);
            msg.Components = BuildReviewStep(draft);
        });
    }

    [ComponentInteraction("vote:canceldraft", ignoreGroupNames: true)]
    public async Task OnCancelDraftAsync()
    {
        _drafts.Remove(Context.User.Id);
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = "Vote annulé.";
            msg.Components = new ComponentBuilder().Build();
        });
    }

    [ComponentInteraction("vote:finish", ignoreGroupNames: true)]
    public async Task OnFinishAsync()
    {
        // Claimed first: of two clicks on « Terminer », one publishes.
        if (!_drafts.TryTake(Context.User.Id, out var draft) || draft.Options.Count < 2)
        {
            if (draft is not null) _drafts.Set(Context.User.Id, draft);
            await RespondAsync("Ajoute au moins deux options avant de terminer.", ephemeral: true);
            return;
        }

        // Two writes, a read and a send before the wizard can close: defer first.
        await DeferAsync();

        var poll = await _pollService.CreateTextPollAsync(
            Context.Guild.Id, Context.Channel.Id, Context.User.Id, draft.Title, draft.Options, draft.Settings);
        var full = (await _pollService.GetPollWithVotesAsync(poll.Id))!;
        try
        {
            var message = await PollCards.SendAsync(Context.Channel, full);
            await _pollService.SetMessageIdAsync(poll.Id, message.Id);
        }
        catch (Exception ex)
        {
            // No card, no vote: nothing in /vote list nobody can see.
            _logger.LogWarning(ex, "Failed to post the card of vote {PollId}; removing it.", poll.Id);
            await _pollService.DeletePollAsync(poll.Id);
            _drafts.Set(Context.User.Id, draft);
            await FollowupAsync(CardPostFailure.Explain(Context.Guild, Context.Channel, "le vote"), ephemeral: true);
            return;
        }

        await DeleteOriginalResponseAsync();
    }

    // ---- Listing + republishing active text votes ------------------------

    [SlashCommand("list", "Lister les votes actifs du serveur")]
    public async Task VoteListAsync()
    {
        await DeferAsync(ephemeral: true);

        var polls = await _pollService.GetActivePollsAsync(Context.Guild.Id, PollKind.Text);
        if (polls.Count == 0)
        {
            await FollowupAsync("Aucun vote actif pour le moment.", ephemeral: true);
            return;
        }

        var sb = new StringBuilder();
        foreach (var p in polls.Take(25))
            sb.AppendLine($"**#{p.Id}** 🗳️ {Format.Sanitize(p.Title)} — {p.Options.Count} options — {PollRules.VoterCount(p)} votant(s) — fin <t:{p.ClosesAt.ToUnixTimeSeconds()}:R>");

        var embed = new EmbedBuilder()
            .WithTitle($"Votes actifs ({polls.Count})")
            .WithDescription(sb.ToString())
            .WithColor(Color.Purple)
            .Build();

        // Reuses the shared "poll:republish" handler (works by poll id, any kind).
        var menu = new SelectMenuBuilder()
            .WithCustomId("poll:republish")
            .WithPlaceholder("Republier un vote dans ce salon");
        foreach (var p in polls.Take(25))
        {
            string label = $"#{p.Id} — {p.Title}";
            if (label.Length > 100) label = label[..100];
            menu.AddOption(label, p.Id.ToString());
        }

        var components = new ComponentBuilder().WithSelectMenu(menu).Build();
        await FollowupAsync(embed: embed, components: components, ephemeral: true);
    }

    [SlashCommand("delete", "Supprimer un vote que tu as créé")]
    public async Task VoteDeleteAsync(
        [Summary("vote-id", "Le vote à supprimer"), Autocomplete(typeof(VoteIdAutocompleteHandler))] int voteId)
    {
        await DeferAsync(ephemeral: true);

        var poll = await _pollService.GetPollWithVotesAsync(voteId);
        if (poll is null || poll.GuildId != Context.Guild.Id || poll.Kind != PollKind.Text)
        {
            await FollowupAsync("Vote introuvable.", ephemeral: true);
            return;
        }

        if (!SessionPermissions.CanManage(Context.User, poll))
        {
            await FollowupAsync("Seul l'organisateur ou un administrateur peut supprimer ce vote.", ephemeral: true);
            return;
        }

        await PollModule.DeleteCardAsync(Context.Guild, poll);
        await _pollService.DeletePollAsync(voteId);
        await FollowupAsync($"Vote **#{voteId}** supprimé.", ephemeral: true);
    }

    // ---- Wizard helpers --------------------------------------------------

    private static MessageComponent BuildReviewStep(VoteDraft draft)
    {
        var builder = new ComponentBuilder();
        if (draft.Options.Count < PollRules.MaxOptions)
            builder.WithButton("Ajouter une option", "vote:add", ButtonStyle.Secondary, new Emoji("➕"), row: 0);
        if (draft.Options.Count >= 2)
            builder.WithButton("Terminer", "vote:finish", ButtonStyle.Success, new Emoji("✅"), row: 0);
        builder.WithButton("Tout reprendre", "vote:begin", ButtonStyle.Secondary, new Emoji("📝"), row: 0);
        builder.WithButton("Annuler", "vote:canceldraft", ButtonStyle.Danger, row: 0);
        return builder.Build();
    }

    private static string BuildDraftSummary(VoteDraft draft)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"**Vote : {Format.Sanitize(draft.Title)}**");
        sb.AppendLine($"-# {PollModule.SettingsLine(draft.Settings)}");
        sb.AppendLine($"Options proposées ({draft.Options.Count}/{PollRules.MaxOptions}) :");
        if (draft.Options.Count == 0)
            sb.AppendLine("_(aucune pour l'instant — ajoute-en au moins deux)_");
        else
            foreach (var o in draft.Options)
                sb.AppendLine($"• {Format.Sanitize(o)}");
        if (draft.Options.Count == 1)
            sb.AppendLine("_Il en faut au moins deux._");
        return sb.ToString();
    }
}
