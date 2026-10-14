using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /health — her check-up, for anyone: the Pi, her process, the gateway, storage, temperature and
// the outage log (PiHealthService.SnapshotAsync, rendered by HealthCards). Posted in the channel
// for everyone to see, once per half-minute per person (the refusal alone is private); the owner
// skips the cooldown and also gets the state file's path. Also the first check after a deploy: it shows whether /sys and /proc are readable
// from inside the add-on's container.
[CommandContextType(InteractionContextType.Guild)]
public class HealthModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PiHealthService _health;
    private readonly ResponsePicker _picker;
    private readonly ILogger<HealthModule> _logger;

    public HealthModule(PiHealthService health, ResponsePicker picker, ILogger<HealthModule> logger)
    {
        _health = health;
        _picker = picker;
        _logger = logger;
    }

    [SlashCommand("health", "Voir l'état de santé du bot et de son Raspberry Pi")]
    public async Task HealthAsync()
    {
        var isOwner = Context.User.Id == AvailabilityService.OwnerId;
        if (!isOwner && !_health.TryClaimHealthCheck(Context.User.Id, out var readyAt))
        {
            await RespondAsync($"Je viens de te faire mon bilan. Reviens <t:{readyAt.ToUnixTimeSeconds()}:R>.", ephemeral: true);
            return;
        }

        // Reads the database: may be past Discord's 3 s on the Pi.
        await DeferAsync();

        HealthReport report;
        try
        {
            report = await _health.SnapshotAsync(Context.Guild);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "/health: snapshot failed.");
            // Replaces the public « réfléchit… » placeholder, so it is public too.
            await FollowupAsync("Je n'arrive pas à faire mon bilan pour l'instant. Réessaie dans un moment.");
            return;
        }

        var pool = PiHealth.Mood(
            report.Temperature.CurrentMilli,
            report.Pi.Power,
            report.Outages.Recent.Select(e => e.EndedAt),
            report.TakenAt) switch
        {
            HealthMood.Hot => BotResponses.HealthHotLines,
            HealthMood.Shaken => BotResponses.HealthShakenLines,
            _ => BotResponses.HealthFineLines,
        };

        await FollowupAsync(
            components: HealthCards.Build(report, _picker.Pick(pool), isOwner),
            flags: MessageFlags.ComponentsV2,
            allowedMentions: AllowedMentions.None);
    }
}
