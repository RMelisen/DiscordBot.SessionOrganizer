using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// The published giveaway card's button, kept here beside the other card handlers
// rather than in GiveawayModule — the module owns the commands and the rendering.
public class GiveawayComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly GiveawayService _giveaways;

    public GiveawayComponentHandler(GiveawayService giveaways)
    {
        _giveaways = giveaways;
    }

    [ComponentInteraction("giveaway:enter:*")]
    public async Task OnEnterAsync(string giveawayIdStr)
    {
        if (!int.TryParse(giveawayIdStr, out var giveawayId))
        {
            await RespondAsync("Tirage invalide.", ephemeral: true);
            return;
        }

        var giveaway = await _giveaways.GetAsync(giveawayId);
        if (giveaway is null || giveaway.GuildId != Context.Guild.Id)
        {
            await RespondAsync("Tirage introuvable.", ephemeral: true);
            return;
        }

        // The sweep may have drawn it between the card being rendered and this click.
        if (giveaway.IsClosed)
        {
            await RespondAsync("Ce tirage est déjà terminé.", ephemeral: true);
            return;
        }

        var entered = await _giveaways.ToggleEntryAsync(giveawayId, Context.User.Id);

        // Re-read so the count on the card matches what was just written.
        var updated = await _giveaways.GetAsync(giveawayId);
        if (updated is null) return;

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(props =>
        {
            props.Embed = GiveawayModule.BuildEmbed(updated);
            props.Components = GiveawayModule.BuildComponents(updated);
        });

        // The card only shows a count, so it never says whether *you* are in it.
        await FollowupAsync(
            entered
                ? "C'est noté, tu participes ! Bonne chance (˶˃ ᵕ ˂˶)"
                : "Tu ne participes plus à ce tirage. Tant pis pour toi.",
            ephemeral: true);
    }
}
