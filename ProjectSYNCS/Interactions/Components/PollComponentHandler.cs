using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

public class PollComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PollService _pollService;

    public PollComponentHandler(PollService pollService)
    {
        _pollService = pollService;
    }

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
        var poll = await _pollService.GetPollWithVotesAsync(pollId);
        if (poll is null || poll.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Sondage introuvable.", ephemeral: true);
            return;
        }

        if (poll.IsClosed)
        {
            await FollowupAsync("Ce sondage est clôturé.", ephemeral: true);
            return;
        }

        var updated = await _pollService.ToggleVoteAsync(
            pollId, optionId, Context.User.Id, Context.User.Username);
        if (updated is null)
        {
            await FollowupAsync("Créneau introuvable.", ephemeral: true);
            return;
        }

        await ModifyOriginalResponseAsync(props =>
        {
            props.Embed = PollModule.BuildPollEmbed(updated);
            props.Components = PollModule.BuildPollComponents(updated);
        });
    }

    [ComponentInteraction("poll:close:*")]
    public async Task OnCloseAsync(string pollIdStr)
    {
        if (!int.TryParse(pollIdStr, out int pollId))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }

        // A read and a write before the card can change: defer first (root CLAUDE.md, reliability).
        await DeferAsync();
        var poll = await _pollService.GetPollWithVotesAsync(pollId);
        if (poll is null || poll.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Sondage introuvable.", ephemeral: true);
            return;
        }

        if (!SessionPermissions.CanManage(Context.User, poll))
        {
            await FollowupAsync("Seul l'organisateur ou un administrateur peut clôturer ce sondage.", ephemeral: true);
            return;
        }

        if (poll.IsClosed)
        {
            await FollowupAsync("Ce sondage est déjà clôturé.", ephemeral: true);
            return;
        }

        await _pollService.ClosePollAsync(pollId);
        poll.IsClosed = true;

        await ModifyOriginalResponseAsync(props =>
        {
            props.Embed = PollModule.BuildPollEmbed(poll);
            props.Components = PollModule.BuildPollComponents(poll);
        });
    }
}
