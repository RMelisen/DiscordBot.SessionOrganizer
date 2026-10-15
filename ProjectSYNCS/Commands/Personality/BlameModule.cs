using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /blame: whose fault it is, drawn from a voice channel (Helpers/Personality/Blame). Quokka is
// always a candidate, she never is, and Papa is never the final answer: a roll on him goes out as a
// plain verdict, then she edits it into a denial and someone else. Guild-only: it reads who is in
// the server's voice channels.
[CommandContextType(InteractionContextType.Guild)]
public class BlameModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ResponsePicker _picker;
    private readonly ILogger<BlameModule> _logger;

    public BlameModule(ResponsePicker picker, ILogger<BlameModule> logger)
    {
        _picker = picker;
        _logger = logger;
    }

    [SlashCommand("blame", "C'est la faute de qui ? Je tranche, parmi les gens en vocal")]
    public async Task BlameAsync(
        [Summary("what", "La faute de quoi : la défaite, le ping, le retard…")]
        [MaxLength(InputCaps.Blame)]
        string? what = null,
        [Summary("channel", "Le salon vocal où chercher (par défaut, le tien)")]
        [ChannelTypes(ChannelType.Voice, ChannelType.Stage)] IGuildChannel? channel = null)
    {
        var voice = channel as SocketVoiceChannel ?? (Context.User as SocketGuildUser)?.VoiceChannel;
        // Humans only, as for /teams: other bots, and her own seat in voice, are never to blame.
        var candidates = voice?.ConnectedUsers.Where(u => !u.IsBot).Select(u => u.Id).ToList() ?? [];

        var result = Blame.Pick(candidates, Context.User.Id, AvailabilityService.OwnerId, Random.Shared);
        var pool = result.Kind switch
        {
            BlameKind.Self => BotResponses.BlameSelfLines,
            BlameKind.Quokka => BotResponses.BlameQuokkaLines,
            BlameKind.PapaDodge => BotResponses.BlamePapaDodgeLines,
            BlameKind.PapaDodgeQuokka => BotResponses.BlamePapaDodgeQuokkaLines,
            BlameKind.Nobody => BotResponses.BlameNobodyLines,
            _ => BotResponses.BlameLines,
        };

        // The pick first, the format second; the Quokka pools ignore {0}.
        var verdict = string.Format(_picker.Pick(pool), $"<@{result.TargetId}>");

        if (!Blame.IsPapaSlip(result.Kind))
        {
            await RespondAsync(WithWhat(what, verdict), allowedMentions: AllowedMentions.None);
            return;
        }

        // The slip: his name goes out first, plainly, and the correction replaces it a few seconds
        // later. Mentions off throughout: he keeps his pill, unpinged.
        var slip = string.Format(_picker.Pick(BotResponses.BlameOnPapaLines), $"<@{AvailabilityService.OwnerId}>");
        await RespondAsync(WithWhat(what, slip), allowedMentions: AllowedMentions.None);

        var correction = WithWhat(what, verdict);
        var delay = Random.Shared.Next(Blame.SlipDelayMs.Min, Blame.SlipDelayMs.Max);
        var interaction = Context.Interaction;
        // Off the handler: the reply is already sent, and the edit only needs the interaction's
        // token (valid 15 minutes). A failed edit leaves the slip standing, which is just logged.
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay);
                await interaction.ModifyOriginalResponseAsync(m =>
                {
                    m.Content = correction;
                    m.AllowedMentions = AllowedMentions.None;
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to correct a /blame slip.");
            }
        });
    }

    // What it's about is quoted on top, like /rate's subject, so it never reads as her own words.
    private static string WithWhat(string? what, string line) =>
        string.IsNullOrWhiteSpace(what) ? line : $"{MessageFormat.Quote(what.Trim(), InputCaps.Blame)}\n{line}";
}
