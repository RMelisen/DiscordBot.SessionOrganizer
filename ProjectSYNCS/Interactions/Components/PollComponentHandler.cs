using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Interactions.Modals;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;
using Poll = ProjectSYNCS.Models.Poll;

namespace ProjectSYNCS.Interactions.Components;

// The poll and vote card's buttons, and its « ⚙️ Gérer » panel. Every handler that reads the
// database before it can show anything defers first (root CLAUDE.md, reliability); « Renommer » and
// « Ajouter » answer with a modal, so they open from the panel's snapshot when they can.
//
// Votes and every change to a poll take its lock (SchedulingLocks) around check → write → reload →
// redraw: a vote can't land after the close, and two can't redraw the card out of order.
public class PollComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PollService _pollService;
    private readonly SchedulingLocks _locks;
    private readonly ManageSnapshots _snapshots;
    private readonly CardUpdater _cards;
    private readonly CardRepost _repost;
    private readonly ILogger<PollComponentHandler> _logger;

    public PollComponentHandler(PollService pollService, SchedulingLocks locks, ManageSnapshots snapshots,
        CardUpdater cards, CardRepost repost, ILogger<PollComponentHandler> logger)
    {
        _pollService = pollService;
        _locks = locks;
        _snapshots = snapshots;
        _cards = cards;
        _repost = repost;
        _logger = logger;
    }

    private bool OnEphemeral =>
        Context.Interaction is SocketMessageComponent c && c.Message.Flags?.HasFlag(MessageFlags.Ephemeral) == true;

    private static string What(Poll poll) => poll.Kind == PollKind.Text ? "ce vote" : "ce sondage";

    [ComponentInteraction("poll:vote:*:*")]
    public async Task OnVoteAsync(string pollIdStr, string optionIdStr)
    {
        if (!int.TryParse(pollIdStr, out int pollId) || !int.TryParse(optionIdStr, out int optionId))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }

        // A read and a write before the card can change: defer first (root CLAUDE.md, reliability).
        await DeferAsync();
        using (await _locks.Polls.AcquireAsync(pollId))
        {
            var poll = await _pollService.GetPollWithVotesAsync(pollId);
            if (poll is null || poll.GuildId != Context.Guild.Id)
            {
                await FollowupAsync("Sondage introuvable.", ephemeral: true);
                return;
            }

            // Closed, or past its deadline and waiting for the next auto-close pass.
            if (!PollRules.IsOpen(poll, DateTimeOffset.UtcNow))
            {
                await FollowupAsync($"{Capitalized(What(poll))} est clôturé.", ephemeral: true);
                return;
            }

            Poll? updated;
            try
            {
                updated = await _pollService.ToggleVoteAsync(
                    pollId, optionId, Context.User.Id, Context.User.Username, poll.SingleChoice);
            }
            catch (DbUpdateException ex)
            {
                // The unique index on (option, person): a click that raced another. Already counted.
                _logger.LogInformation(ex, "Duplicate vote on poll {PollId} by {UserId}.", pollId, Context.User.Id);
                await FollowupAsync("C'est déjà compté.", ephemeral: true);
                return;
            }
            if (updated is null)
            {
                await FollowupAsync("Cette option n'existe plus.", ephemeral: true);
                return;
            }

            await RedrawHereAsync(updated);
        }
    }

    // What *you* picked: the one thing an anonymous card still tells you.
    [ComponentInteraction("poll:mine:*")]
    public async Task OnMineAsync(string pollIdStr)
    {
        if (!int.TryParse(pollIdStr, out int pollId))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        var poll = await _pollService.GetPollWithVotesAsync(pollId);
        if (poll is null || poll.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Sondage introuvable.", ephemeral: true);
            return;
        }

        var mine = PollRules.Ordered(poll).Where(o => o.Votes.Any(v => v.UserId == Context.User.Id)).ToList();
        var text = mine.Count == 0
            ? $"Tu n'as pas encore voté sur {What(poll)}."
            : $"Tes votes sur « {Format.Sanitize(poll.Title)} » :\n" + string.Join("\n", mine.Select(o => $"• {PollCards.OptionText(poll, o)}"));
        if (poll.SingleChoice && PollRules.IsOpen(poll, DateTimeOffset.UtcNow))
            text += "\n-# Un seul choix : cliquer une autre option déplace ton vote.";
        await FollowupAsync(text, ephemeral: true);
    }

    // ---- « ⚙️ Gérer » ----------------------------------------------------

    [ComponentInteraction("poll:manage:*")]
    public async Task OnManageAsync(string pollIdStr)
    {
        if (!int.TryParse(pollIdStr, out int pollId))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        var poll = await _pollService.GetPollWithVotesAsync(pollId);
        if (poll is null || poll.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Sondage introuvable.", ephemeral: true);
            return;
        }
        if (!SessionPermissions.CanManage(Context.User, poll))
        {
            await FollowupAsync($"{Capitalized(What(poll))} n'est pas à toi : seul l'organisateur ou un administrateur peut le gérer.",
                ephemeral: true);
            return;
        }

        _snapshots.Remember(Context.User.Id, poll);
        await FollowupAsync(PollManage.Panel(poll), components: PollManage.PanelComponents(poll), ephemeral: true);
    }

    // From the panel, or from the « Clôturer » of a card posted before it.
    [ComponentInteraction("poll:close:*")]
    public async Task OnCloseAsync(string pollIdStr)
    {
        if (!int.TryParse(pollIdStr, out int pollId))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }

        bool panel = OnEphemeral;
        // A read and a write before the card can change: defer first (root CLAUDE.md, reliability).
        await DeferAsync();
        Poll poll;
        using (await _locks.Polls.AcquireAsync(pollId))
        {
            var found = await LoadManagedAsync(pollId, panel);
            if (found is null) return;
            poll = found;
            if (poll.IsClosed)
            {
                await Refuse($"{Capitalized(What(poll))} est déjà clôturé.", panel);
                return;
            }

            // A manual close says nothing in the channel: the organiser knows (ReminderService
            // announces the auto-closes).
            await _pollService.ClosePollAsync(pollId);
            poll.IsClosed = true;
            _snapshots.ForgetPoll(pollId);
            if (panel) await _cards.RedrawPollAsync(Context.Guild, poll);
            else await RedrawHereAsync(poll);
        }
        if (panel) await PanelAsync(poll, "🔒 Clôturé.");
    }

    [ComponentInteraction("poll:extend:*:*")]
    public async Task OnExtendAsync(string pollIdStr, string hoursStr)
    {
        if (!int.TryParse(pollIdStr, out int pollId) || !int.TryParse(hoursStr, out int hours))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }

        await DeferAsync();
        Poll poll;
        using (await _locks.Polls.AcquireAsync(pollId))
        {
            var found = await LoadManagedAsync(pollId, panel: true);
            if (found is null) return;
            poll = found;
            if (poll.IsClosed)
            {
                await Refuse($"{Capitalized(What(poll))} est clôturé : plus rien à prolonger.", panel: true);
                return;
            }

            var endsAt = PollRules.Extend(poll, TimeSpan.FromHours(hours), DateTimeOffset.UtcNow);
            await _pollService.SetEndsAtAsync(pollId, endsAt);
            poll.EndsAt = endsAt;
            await _cards.RedrawPollAsync(Context.Guild, poll);
        }
        await PanelAsync(poll, $"⏳ Se termine maintenant <t:{poll.ClosesAt.ToUnixTimeSeconds()}:R>.");
    }

    [ComponentInteraction("poll:repost:*")]
    public async Task OnRepostAsync(string pollIdStr)
    {
        if (!int.TryParse(pollIdStr, out int pollId))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }

        await DeferAsync();
        if (await LoadManagedAsync(pollId, panel: true) is not { } poll) return;
        var result = await _repost.RepostAsync(Context.Guild, Context.Channel, new CardRef(CardKind.Poll, pollId));
        await PanelAsync(poll, result == RepostResult.Reposted ? "📌 Carte republiée ici." : $"{Capitalized(What(poll))} est clôturé.");
    }

    // ---- Renommer ---------------------------------------------------------

    [ComponentInteraction("poll:rename:*")]
    public async Task OnRenameAsync(string pollIdStr)
    {
        if (!int.TryParse(pollIdStr, out int pollId))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }
        // A modal must be the first response: the snapshot's title pre-fills it, no read needed.
        var title = _snapshots.Poll(Context.User.Id, pollId)?.Title;
        await RespondWithModalAsync(new ModalBuilder()
            .WithTitle("Renommer")
            .WithCustomId($"poll:renamemodal:{pollId}")
            .AddTextInput("Nouveau titre", "title", maxLength: InputCaps.Title, value: title, required: true)
            .Build());
    }

    [ModalInteraction("poll:renamemodal:*")]
    public async Task OnRenameSubmittedAsync(string pollIdStr, PollRenameModal modal)
    {
        if (!int.TryParse(pollIdStr, out int pollId))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        var title = modal.PollTitle.Trim();
        if (title.Length == 0)
        {
            await FollowupAsync("Il faut un titre.", ephemeral: true);
            return;
        }
        using (await _locks.Polls.AcquireAsync(pollId))
        {
            if (await LoadManagedAsync(pollId, panel: false) is not { } poll) return;
            if (poll.IsClosed)
            {
                await FollowupAsync($"{Capitalized(What(poll))} est clôturé.", ephemeral: true);
                return;
            }
            await _pollService.RenameAsync(pollId, title);
            poll.Title = title;
            _snapshots.ForgetPoll(pollId);
            await _cards.RedrawPollAsync(Context.Guild, poll);
        }
        await FollowupAsync("Renommé ✅", ephemeral: true);
    }

    // ---- Ajouter une option ----------------------------------------------

    [ComponentInteraction("poll:addopt:*")]
    public async Task OnAddOptionAsync(string pollIdStr)
    {
        if (!int.TryParse(pollIdStr, out int pollId))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }

        // Which modal depends on the kind; the snapshot knows it, a read is the fallback.
        var kind = _snapshots.Poll(Context.User.Id, pollId)?.Kind
                   ?? (await _pollService.GetPollWithVotesAsync(pollId))?.Kind;
        if (kind == PollKind.Text)
            await RespondWithModalAsync<VoteOptionModal>($"poll:addtext:{pollId}");
        else
            await RespondWithModalAsync<PollSlotModal>($"poll:addslot:{pollId}");
    }

    [ModalInteraction("poll:addtext:*")]
    public Task OnAddTextSubmittedAsync(string pollIdStr, VoteOptionModal modal) =>
        AddOptionAsync(pollIdStr, slotText: null, label: modal.OptionLabel);

    [ModalInteraction("poll:addslot:*")]
    public Task OnAddSlotSubmittedAsync(string pollIdStr, PollSlotModal modal) =>
        AddOptionAsync(pollIdStr, slotText: modal.When, label: null);

    private async Task AddOptionAsync(string pollIdStr, string? slotText, string? label)
    {
        if (!int.TryParse(pollIdStr, out int pollId))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        DateTimeOffset? slot = null;
        if (slotText is not null)
        {
            slot = SessionTime.Parse(slotText, DateTimeOffset.UtcNow);
            if (slot is null)
            {
                await FollowupAsync($"« {Format.Sanitize(slotText.Trim())} » n'est pas une date à venir que je comprends "
                    + $"(ex. {FrenchDate.Examples}).", ephemeral: true);
                return;
            }
        }
        label = label?.Trim();
        if (label is { Length: 0 })
        {
            await FollowupAsync("Il faut un nom d'option.", ephemeral: true);
            return;
        }

        using (await _locks.Polls.AcquireAsync(pollId))
        {
            if (await LoadManagedAsync(pollId, panel: false) is not { } poll) return;
            if (poll.IsClosed)
            {
                await FollowupAsync($"{Capitalized(What(poll))} est clôturé.", ephemeral: true);
                return;
            }
            if (poll.Options.Count >= PollRules.MaxOptions)
            {
                await FollowupAsync($"{PollRules.MaxOptions} options au maximum.", ephemeral: true);
                return;
            }
            if (!await _pollService.AddOptionAsync(pollId, slot, label))
            {
                await FollowupAsync("Cette option y est déjà.", ephemeral: true);
                return;
            }
            var updated = (await _pollService.GetPollWithVotesAsync(pollId))!;
            _snapshots.ForgetPoll(pollId);
            await _cards.RedrawPollAsync(Context.Guild, updated);
        }
        await FollowupAsync("Ajouté ✅", ephemeral: true);
    }

    // ---- Retirer une option (asks first when it has votes) ----------------

    [ComponentInteraction("poll:rmopt:*")]
    public async Task OnRemoveOptionPickedAsync(string pollIdStr, string[] values)
    {
        if (!int.TryParse(pollIdStr, out int pollId) || !int.TryParse(values[0], out int optionId))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }

        await DeferAsync();
        if (await LoadManagedAsync(pollId, panel: true) is not { } poll) return;
        if (poll.Options.FirstOrDefault(o => o.Id == optionId) is not { } option)
        {
            await PanelAsync(poll, "Cette option n'existe plus.");
            return;
        }

        if (option.Votes.Count > 0)
        {
            await ModifyOriginalResponseAsync(p =>
            {
                p.Content = $"Retirer {PollCards.OptionText(poll, option)} ? Ses **{option.Votes.Count}** vote(s) partent avec.";
                p.Components = PollManage.RemoveConfirm(pollId, optionId);
            });
            return;
        }
        await RemoveAsync(pollId, optionId);
    }

    [ComponentInteraction("poll:rmoptyes:*:*")]
    public async Task OnRemoveOptionConfirmedAsync(string pollIdStr, string optionIdStr)
    {
        if (!int.TryParse(pollIdStr, out int pollId) || !int.TryParse(optionIdStr, out int optionId))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }
        await DeferAsync();
        await RemoveAsync(pollId, optionId);
    }

    [ComponentInteraction("poll:rmoptno:*")]
    public async Task OnRemoveOptionDeclinedAsync(string pollIdStr)
    {
        if (!int.TryParse(pollIdStr, out int pollId))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }
        await DeferAsync();
        if (await LoadManagedAsync(pollId, panel: true) is { } poll) await PanelAsync(poll, "D'accord, rien n'est retiré.");
    }

    private async Task RemoveAsync(int pollId, int optionId)
    {
        Poll updated;
        using (await _locks.Polls.AcquireAsync(pollId))
        {
            if (await LoadManagedAsync(pollId, panel: true) is not { } poll) return;
            if (poll.IsClosed)
            {
                await Refuse($"{Capitalized(What(poll))} est clôturé.", panel: true);
                return;
            }
            if (poll.Options.Count <= PollManage.MinOptions(poll))
            {
                await PanelAsync(poll, "Il ne resterait pas assez d'options : rien n'est retiré.");
                return;
            }
            await _pollService.RemoveOptionAsync(pollId, optionId);
            updated = (await _pollService.GetPollWithVotesAsync(pollId))!;
            _snapshots.ForgetPoll(pollId);
            await _cards.RedrawPollAsync(Context.Guild, updated);
        }
        await PanelAsync(updated, "➖ Option retirée.");
    }

    // ---- Shared -----------------------------------------------------------

    // The poll, when it exists here and the clicker may manage it; refuses otherwise.
    private async Task<Poll?> LoadManagedAsync(int pollId, bool panel)
    {
        var poll = await _pollService.GetPollWithVotesAsync(pollId);
        if (poll is null || poll.GuildId != Context.Guild.Id)
        {
            await Refuse("Sondage introuvable.", panel);
            return null;
        }
        if (!SessionPermissions.CanManage(Context.User, poll))
        {
            await Refuse("Seul l'organisateur ou un administrateur peut gérer ce sondage.", panel);
            return null;
        }
        return poll;
    }

    private Task Refuse(string text, bool panel) =>
        panel
            ? ModifyOriginalResponseAsync(p =>
            {
                p.Content = text;
                p.Components = new ComponentBuilder().Build();
            })
            : FollowupAsync(text, ephemeral: true);

    // Redraws the panel the click came from, with a line saying what just happened.
    private Task PanelAsync(Poll poll, string note) =>
        ModifyOriginalResponseAsync(p =>
        {
            p.Content = PollManage.Panel(poll, note);
            p.Components = PollManage.PanelComponents(poll);
        });

    // Redraws the card this button is on. A legacy embed card that refuses the V2 edit is reposted
    // instead, so the click is never lost.
    private async Task RedrawHereAsync(Poll poll)
    {
        try
        {
            await ModifyOriginalResponseAsync(p => PollCards.ApplyTo(p, poll));
        }
        catch (HttpException ex) when (!poll.IsClosed)
        {
            _logger.LogWarning(ex, "Poll card {PollId} refused the edit; reposting it.", poll.Id);
            await _repost.RepostAsync(Context.Guild, Context.Channel, new CardRef(CardKind.Poll, poll.Id));
        }
    }

    private static string Capitalized(string text) => text.Length == 0 ? text : char.ToUpper(text[0]) + text[1..];
}
