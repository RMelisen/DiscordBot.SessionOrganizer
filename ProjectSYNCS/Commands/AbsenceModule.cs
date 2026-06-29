using Discord.Interactions;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// Lets the owner flag themselves as absent. While absent, the bot intercepts
// mentions of the owner and replies, formally, that they are unavailable.
// In-memory only: a restart clears the flag.
public class AbsenceModule : InteractionModuleBase<SocketInteractionContext>
{
    public enum AbsenceState
    {
        [ChoiceDisplay("Activer")] On,
        [ChoiceDisplay("Désactiver")] Off,
    }

    private readonly AvailabilityService _availability;

    public AbsenceModule(AvailabilityService availability)
    {
        _availability = availability;
    }

    [SlashCommand("absent", "Activer ou désactiver ton mode absent (réservé à l'organisateur)")]
    public async Task SetAbsenceAsync(
        [Summary("etat", "Activer ou désactiver le mode absent")] AbsenceState state)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }

        bool absent = state == AbsenceState.On;
        _availability.SetOwnerAbsent(absent);

        await RespondAsync(
            absent
                ? "Mode absent **activé**. Je préviendrai poliment quiconque te mentionne. ✨"
                : "Mode absent **désactivé**. Tu es de nouveau disponible (˶˃ ᵕ ˂˶)",
            ephemeral: true);
    }
}
