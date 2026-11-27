using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /balance's reminder button, and the « Travailler » button on the reminder DM.
public class EconomyComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PebbleService _pebbles;
    private readonly ResponsePicker _picker;
    private readonly PlynlingAnnouncer _announcer;
    private readonly ILogger<EconomyComponentHandler> _logger;

    public EconomyComponentHandler(PebbleService pebbles, ResponsePicker picker, PlynlingAnnouncer announcer,
        ILogger<EconomyComponentHandler> logger)
    {
        _pebbles = pebbles;
        _picker = picker;
        _announcer = announcer;
        _logger = logger;
    }

    // The /balance card is ephemeral, so whoever clicks is its owner, and the flag is always set
    // for Context.User. It writes the database before it can answer, so it defers first.
    [ComponentInteraction(EconomyModule.RemindOnId, ignoreGroupNames: true)]
    public Task OnRemindOnAsync() => SetAsync(true);

    [ComponentInteraction(EconomyModule.RemindOffId, ignoreGroupNames: true)]
    public Task OnRemindOffAsync() => SetAsync(false);

    private async Task SetAsync(bool on)
    {
        await DeferAsync();
        await _pebbles.SetWorkReminderAsync(Context.Guild.Id, Context.User.Id, on);
        var balance = await _pebbles.GetBalanceAsync(Context.Guild.Id, Context.User.Id);
        var (text, components) = EconomyModule.BalanceCard(balance, DateTimeOffset.UtcNow, on ? EconomyModule.RemindOnNotice : EconomyModule.RemindOffNotice);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Content = text;
            m.Components = components;
        });
    }

    // The reminder DM's « Travailler »: the same shift as /work, for the server in the id. Clicked in
    // a DM, so there is no Context.Guild and only the recipient can click. A paid shift is also posted
    // in the Plynling channel, so it stays as visible as a /work there, and the DM becomes the result
    // with a link to that post (a button cannot move anyone to a channel by itself). A refusal stays
    // private; either way the button goes, it has done its job.
    [ComponentInteraction(EconomyModule.WorkFromDmPrefix + "*", ignoreGroupNames: true)]
    public async Task OnWorkFromDmAsync(string guildIdText)
    {
        await DeferAsync();
        if (!ulong.TryParse(guildIdText, out var guildId)) return;

        var dm = await GetOriginalResponseAsync();
        if (Context.Client.GetGuild(guildId)?.GetUser(Context.User.Id) is null)
        {
            await ModifyOriginalResponseAsync(m => m.Components = new ComponentBuilder().Build());
            await FollowupAsync(EconomyModule.WorkFromDmGone, ephemeral: true);
            return;
        }

        var result = await _pebbles.WorkAsync(guildId, Context.User.Id, PebbleEconomy.RollWorkPay(), DateTimeOffset.UtcNow);
        var (text, refused) = EconomyModule.WorkReply(result, _picker);
        if (refused)
        {
            await ModifyOriginalResponseAsync(m => m.Components = new ComponentBuilder().Build());
            await FollowupAsync(text, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        var posted = await PostInChannelAsync(guildId, text);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Content = $"{dm.Content}\n\n{text}";
            m.AllowedMentions = AllowedMentions.None;
            m.Components = posted is null
                ? new ComponentBuilder().Build()
                : new ComponentBuilder()
                    .WithButton("Voir dans le salon", style: ButtonStyle.Link, url: posted.GetJumpUrl())
                    .Build();
        });
    }

    // The public copy of the shift, as a /work in the channel would read: who worked, as a pill
    // that never pings. The Plynling channel, where deaths go, with no fallback. A Discord failure
    // costs the post, never the pay.
    private async Task<IUserMessage?> PostInChannelAsync(ulong guildId, string text)
    {
        try
        {
            var channel = await _announcer.ResolveGameChannelAsync(guildId);
            if (channel is null) return null;
            return await channel.SendMessageAsync($"<@{Context.User.Id}>\n{text}",
                allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post the /work shift from the DM in guild {GuildId}.", guildId);
            return null;
        }
    }
}
