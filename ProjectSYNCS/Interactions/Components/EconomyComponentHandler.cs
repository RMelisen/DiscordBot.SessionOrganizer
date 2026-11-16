using Discord;
using Discord.Interactions;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /balance's reminder button. The card is ephemeral, so whoever clicks is its owner, and the flag
// is always set for Context.User. It writes the database before it can answer, so it defers first.
public class EconomyComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PebbleService _pebbles;

    public EconomyComponentHandler(PebbleService pebbles)
    {
        _pebbles = pebbles;
    }

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
}
