using Discord.Interactions;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Interactions.Modals;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The two registrations of PlynlingModule's commands. Discord has no command aliases, so the
// shortcut is a second top-level command with the same subcommands — asked for by people on
// phones, where "/pl v" finds "pl view" but never "plynling view". Each group counts toward
// Discord's 25-subcommand cap on its own, identically.

[Group("plynling", "Ton Plynling : l'adopter, t'en occuper, le regarder vivre")]
public sealed class PlynlingLongModule(PlynlingService plynlings, ResponsePicker picker,
    PlynlingAnnouncer announcer, PlynlingCooldowns cooldowns, ShameService shame, PlynlingPlayService play,
    CosmeticService cosmetics, PlynlingVisitRunner visits, ILogger<PlynlingModule> logger)
    : PlynlingModule(plynlings, picker, announcer, cooldowns, shame, play, cosmetics, visits, logger)
{
    // The modals answer whichever group opened them (ignoreGroupNames), so only this group
    // declares them.
    [ModalInteraction("plyn:abandon:*", ignoreGroupNames: true)]
    public Task OnAbandonModalAsync(string idStr, AbandonModal modal) => OnAbandonConfirmedAsync(idStr, modal);

    [ModalInteraction("plyn:passion:*", ignoreGroupNames: true)]
    public Task OnPassionModalAsync(string idStr, PassionModal modal) => OnPassionTaughtAsync(idStr, modal);
}

[Group("pl", "Raccourci de /plynling")]
public sealed class PlynlingShortModule(PlynlingService plynlings, ResponsePicker picker,
    PlynlingAnnouncer announcer, PlynlingCooldowns cooldowns, ShameService shame, PlynlingPlayService play,
    CosmeticService cosmetics, PlynlingVisitRunner visits, ILogger<PlynlingModule> logger)
    : PlynlingModule(plynlings, picker, announcer, cooldowns, shame, play, cosmetics, visits, logger);
