using Discord;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Posts one of her sticker strips (Assets/Stickers) after a line tagged with Helpers/Core/Sticker.
// BotChat reaches TryFollowUp through its static FollowUp hook, set in Program.cs; /debug sticker
// calls PostAsync directly. An ordinary tag wins 1 in 3; a sure tag (the moment pools, and
// WALL-E's robot-heart lines once RobotHeartService has rolled their own 2 in 3) skips the roll.
// No cooldown per channel and no daily cap: the owner removed both in 6.14.0, so the lines' own
// rarity is the only limit. Nothing during a breakdown.
//
// A singleton, like the other services BotChat's static hook reaches; it holds no state.
public sealed class StickerService
{
    public const double Chance = 1.0 / 3;

    // The beat between the line and its picture, so the strip reads as her reaction to it.
    private static readonly TimeSpan Beat = TimeSpan.FromSeconds(1);

    private readonly BreakdownService _breakdown;
    private readonly ILogger<StickerService> _logger;

    public StickerService(BreakdownService breakdown, ILogger<StickerService> logger)
    {
        _breakdown = breakdown;
        _logger = logger;
    }

    /// <summary>
    /// The gate alone: the roll for an ordinary tag, and no strip in a channel in breakdown.
    /// Public so it can be checked without a gateway.
    /// </summary>
    public bool TryAdmit(ulong channelId, StickerTag tag)
    {
        if (!tag.Sure && Random.Shared.NextDouble() >= Chance) return false;
        return !_breakdown.IsActive(channelId);
    }

    /// <summary>
    /// After a tagged line went out: maybe post its strip. Off the gateway handler, which
    /// BotChat's typing pause has already brought close to Discord.Net's 3 s timeout.
    /// </summary>
    public void TryFollowUp(IMessageChannel channel, StickerTag tag)
    {
        if (!TryAdmit(channel.Id, tag)) return;
        _ = Task.Run(async () =>
        {
            await Task.Delay(Beat);
            await PostAsync(channel, tag.Mood);
        });
    }

    /// <summary>
    /// Posts a strip as a file with no text, never a ping. Swallows and logs: a channel without
    /// Attach Files just gets the line.
    /// </summary>
    public async Task<bool> PostAsync(IMessageChannel channel, string mood)
    {
        try
        {
            await channel.SendFileAsync(Sticker.FileFor(mood), text: null, allowedMentions: AllowedMentions.None);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post the {Mood} sticker in channel {ChannelId}.", mood, channel.Id);
            return false;
        }
    }
}
