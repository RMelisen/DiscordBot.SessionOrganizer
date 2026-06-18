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

        var poll = await _pollService.GetPollWithVotesAsync(pollId);
        if (poll is null || poll.GuildId != Context.Guild.Id)
        {
            await RespondAsync("Sondage introuvable.", ephemeral: true);
            return;
        }

        if (poll.IsClosed)
        {
            await RespondAsync("Ce sondage est clôturé.", ephemeral: true);
            return;
        }

        var updated = await _pollService.ToggleVoteAsync(
            pollId, optionId, Context.User.Id, Context.User.Username);
        if (updated is null)
        {
            await RespondAsync("Créneau introuvable.", ephemeral: true);
            return;
        }

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(props =>
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

        var poll = await _pollService.GetPollWithVotesAsync(pollId);
        if (poll is null || poll.GuildId != Context.Guild.Id)
        {
            await RespondAsync("Sondage introuvable.", ephemeral: true);
            return;
        }

        if (!SessionPermissions.CanManage(Context.User, poll))
        {
            await RespondAsync("Seul l'organisateur ou un administrateur peut clôturer ce sondage.", ephemeral: true);
            return;
        }

        if (poll.IsClosed)
        {
            await RespondAsync("Ce sondage est déjà clôturé.", ephemeral: true);
            return;
        }

        await _pollService.ClosePollAsync(pollId);
        poll.IsClosed = true;

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(props =>
        {
            props.Embed = PollModule.BuildPollEmbed(poll);
            props.Components = PollModule.BuildPollComponents(poll);
        });
    }
}
