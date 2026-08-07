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

    // Two buttons, two handlers, neither a toggle: with an explicit "Ne plus
    // participer" beside it, a "Participer" that also withdrew you would be the very
    // ambiguity the second button was added to remove.
    [ComponentInteraction("giveaway:enter:*")]
    public Task OnEnterAsync(string giveawayIdStr) => HandleAsync(giveawayIdStr, entering: true);

    [ComponentInteraction("giveaway:leave:*")]
    public Task OnLeaveAsync(string giveawayIdStr) => HandleAsync(giveawayIdStr, entering: false);

    private async Task HandleAsync(string giveawayIdStr, bool entering)
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

        var changed = entering
            ? await _giveaways.AddEntryAsync(giveawayId, Context.User.Id)
            : await _giveaways.RemoveEntryAsync(giveawayId, Context.User.Id);

        // Re-read so the roster on the card matches what was just written.
        var updated = await _giveaways.GetAsync(giveawayId);
        if (updated is null) return;

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(props =>
        {
            props.Embed = GiveawayModule.BuildEmbed(updated);
            props.Components = GiveawayModule.BuildComponents(updated);
        });

        // The card lists everyone, but scanning it for your own name is work — and a
        // click that changed nothing has to say so, or it reads as broken.
        await FollowupAsync(Acknowledgement(entering, changed), ephemeral: true);
    }

    private static string Acknowledgement(bool entering, bool changed) => (entering, changed) switch
    {
        (true, true) => "C'est noté, tu participes ! Bonne chance (˶˃ ᵕ ˂˶)",
        (true, false) => "Tu participes déjà à ce tirage. Une fois suffit ( ˶ˆ ᗜ ˆ˵ )",
        (false, true) => "Tu ne participes plus à ce tirage. Tant pis pour toi.",
        (false, false) => "Tu ne participais pas à ce tirage de toute façon (ᵕ • ᴗ •)",
    };
}
