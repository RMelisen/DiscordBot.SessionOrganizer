using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /reminders' cancel menu: deletes the pick (its owner's only) and redraws the list in place.
public class RemindComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PersonalReminderStore _store;

    public RemindComponentHandler(PersonalReminderStore store)
    {
        _store = store;
    }

    [ComponentInteraction("rem:cancel")]
    public async Task OnCancelAsync(string[] values)
    {
        await DeferAsync();
        if (values.Length > 0 && int.TryParse(values[0], out var id))
            await _store.CancelAsync(id, Context.User.Id);
        var (embed, components) = await RemindModule.BuildListAsync(_store, Context.User.Id);
        await ((SocketMessageComponent)Context.Interaction).ModifyOriginalResponseAsync(p =>
        {
            p.Embed = embed;
            p.Components = components;
        });
    }
}
