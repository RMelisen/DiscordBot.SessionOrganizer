using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The premium easter egg: about one slash command in 500 (Helpers/Personality/PremiumPrank) is
// answered with a fake paywall — « un abonnement premium est requis » —, edited 5 to 7 s later into
// « je rigole, je suis gratuite », and only then run. The command's own answers arrive as follow-ups
// under the joke (Commands/Core/SyncsModuleBase does the rerouting).
//
// Commands carrying [NoPremiumPrank] are never caught: modals, answers through Context.Interaction,
// /confess. Skipped in silence mode, like her other unprompted lines. The wait runs off the gateway
// handler. Absent from README.md, /help and the changelog, like the other easter eggs.
public sealed class PremiumPrankService
{
    // An interaction's token lives 15 minutes: past that, nothing can answer through it any more.
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly ResponsePicker _picker;
    private readonly SilenceService _silence;
    private readonly ILogger<PremiumPrankService> _logger;

    public PremiumPrankService(DiscordSocketClient client, InteractionService interactions, IServiceProvider services,
        ResponsePicker picker, SilenceService silence, ILogger<PremiumPrankService> logger)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _picker = picker;
        _silence = silence;
        _logger = logger;
    }

    /// <summary>
    /// Rolls for the prank on an incoming interaction. True when it took the interaction over: the
    /// caller must not run the command, the prank runs it after the reveal.
    /// </summary>
    public bool TryTakeOver(SocketInteraction interaction)
    {
        if (interaction is not SocketSlashCommand slash) return false;
        // The roll first: it skips the command lookup for 499 commands in 500.
        if (!PremiumPrank.Rolls(Random.Shared) || _silence.IsSilent) return false;
        var search = _interactions.SearchSlashCommand(slash);
        if (!search.IsSuccess || IsExempt(search.Command)) return false;

        _ = Task.Run(() => PlayAsync(slash));
        return true;
    }

    private static bool IsExempt(SlashCommandInfo command)
    {
        if (command.Attributes.Any(a => a is NoPremiumPrankAttribute)) return true;
        for (var module = command.Module; module is not null; module = module.Parent)
            if (module.Attributes.Any(a => a is NoPremiumPrankAttribute)) return true;
        return false;
    }

    private async Task PlayAsync(SocketSlashCommand slash)
    {
        SyncsModuleBase.MarkPranked(slash.Id);
        try
        {
            await slash.RespondAsync(_picker.Pick(BotResponses.PremiumPaywallLines), allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            // No paywall, no joke: the command runs as if nothing happened, if it still can.
            _logger.LogWarning(ex, "Failed to post the premium paywall.");
            SyncsModuleBase.ForgetPranked(slash.Id);
            if (!slash.HasResponded) await ExecuteAsync(slash);
            return;
        }

        try
        {
            await Task.Delay(Random.Shared.Next(PremiumPrank.RevealDelayMs.Min, PremiumPrank.RevealDelayMs.Max));
            var reveal = _picker.Pick(BotResponses.PremiumRevealLines);
            await slash.ModifyOriginalResponseAsync(m =>
            {
                m.Content = reveal;
                m.AllowedMentions = AllowedMentions.None;
            });
        }
        catch (Exception ex)
        {
            // The paywall stays up, but the command still runs: it was a joke either way.
            _logger.LogWarning(ex, "Failed to reveal the premium prank.");
        }

        await ExecuteAsync(slash);
        _ = ForgetLaterAsync(slash.Id);
    }

    private async Task ExecuteAsync(SocketSlashCommand slash)
    {
        try
        {
            var result = await _interactions.ExecuteCommandAsync(new SocketInteractionContext(_client, slash), _services);
            if (!result.IsSuccess)
                _logger.LogWarning("Pranked interaction failed: {Error} — {Reason}", result.Error, result.ErrorReason);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to run a pranked command.");
        }
    }

    // Commands run async: their answers can come long after ExecuteCommandAsync returned.
    private static async Task ForgetLaterAsync(ulong interactionId)
    {
        await Task.Delay(TokenLifetime);
        SyncsModuleBase.ForgetPranked(interactionId);
    }
}
