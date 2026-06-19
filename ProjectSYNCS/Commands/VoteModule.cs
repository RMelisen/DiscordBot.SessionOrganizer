using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Interactions.Modals;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;
using System.Collections.Concurrent;
using System.Text;

namespace ProjectSYNCS.Commands;

[Group("vote", "Votes à choix multiples (jeux, films, etc.)")]
public class VoteModule : InteractionModuleBase<SocketInteractionContext>
{
    private const int MaxOptions = 10;
    private const int MaxLabelLength = 80;

    private readonly PollService _pollService;

    // Options being assembled before publishing, keyed by user (same approach as
    // the date-based PollModule wizard).
    private static readonly ConcurrentDictionary<ulong, VoteDraft> _drafts = new();

    private sealed class VoteDraft
    {
        public string Title { get; init; } = string.Empty;
        public List<string> Options { get; } = new();
    }

    public VoteModule(PollService pollService)
    {
        _pollService = pollService;
    }

    // ---- Entry: the slash command owns the wizard message ----------------
    // The wizard message is created here (by the slash command) and only ever
    // *updated* afterwards. Creating it from a modal response instead caused the
    // first option-add update to be dropped.

    [SlashCommand("create", "Proposer plusieurs options (texte) et laisser voter")]
    public async Task VoteCreateAsync()
    {
        _drafts.TryRemove(Context.User.Id, out _);
        await RespondAsync(
            "**Nouveau vote**\nClique sur **Définir le titre** pour commencer.",
            components: new ComponentBuilder()
                .WithButton("Définir le titre", "vote:begin", ButtonStyle.Primary, new Emoji("📝"))
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
        var draft = new VoteDraft { Title = modal.PollTitle.Trim() };
        _drafts[Context.User.Id] = draft;

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
        if (!_drafts.TryGetValue(Context.User.Id, out var draft))
        {
            await RespondAsync("Ce vote a expiré. Relance `/vote create`.", ephemeral: true);
            return;
        }

        var label = modal.OptionLabel.Trim();
        if (label.Length > MaxLabelLength) label = label[..MaxLabelLength];

        if (label.Length > 0
            && draft.Options.Count < MaxOptions
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
        _drafts.TryRemove(Context.User.Id, out _);
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
        if (!_drafts.TryGetValue(Context.User.Id, out var draft) || draft.Options.Count < 2)
        {
            await RespondAsync("Ajoute au moins deux options avant de terminer.", ephemeral: true);
            return;
        }

        var poll = await _pollService.CreateTextPollAsync(
            Context.Guild.Id, Context.Channel.Id, Context.User.Id, draft.Title, draft.Options);
        var full = await _pollService.GetPollWithVotesAsync(poll.Id);

        var message = await Context.Channel.SendMessageAsync(
            embed: PollModule.BuildPollEmbed(full!),
            components: PollModule.BuildPollComponents(full!));
        await _pollService.SetMessageIdAsync(poll.Id, message.Id);

        _drafts.TryRemove(Context.User.Id, out _);

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = "Vote publié.";
            msg.Components = new ComponentBuilder().Build();
        });
        await component.DeleteOriginalResponseAsync();
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
        foreach (var p in polls)
        {
            int voters = p.Options.SelectMany(o => o.Votes).Select(v => v.UserId).Distinct().Count();
            sb.AppendLine($"**#{p.Id}** 📊 {p.Title} — {p.Options.Count} options — 🗳️ {voters} votant(s)");
        }

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

    // ---- Wizard helpers --------------------------------------------------

    private static MessageComponent BuildReviewStep(VoteDraft draft)
    {
        var builder = new ComponentBuilder();
        if (draft.Options.Count < MaxOptions)
            builder.WithButton("Ajouter une option", "vote:add", ButtonStyle.Secondary, new Emoji("➕"), row: 0);
        if (draft.Options.Count >= 2)
            builder.WithButton("Terminer", "vote:finish", ButtonStyle.Success, new Emoji("✅"), row: 0);
        builder.WithButton("Annuler", "vote:canceldraft", ButtonStyle.Danger, row: 0);
        return builder.Build();
    }

    private static string BuildDraftSummary(VoteDraft draft)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"**Vote : {draft.Title}**");
        sb.AppendLine($"Options proposées ({draft.Options.Count}/{MaxOptions}) :");
        if (draft.Options.Count == 0)
            sb.AppendLine("_(aucune pour l'instant — ajoute-en au moins deux)_");
        else
            foreach (var o in draft.Options)
                sb.AppendLine($"• {o}");
        return sb.ToString();
    }
}
