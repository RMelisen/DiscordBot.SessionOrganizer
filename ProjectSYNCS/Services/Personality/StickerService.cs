using Discord;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Posts one of her sticker strips (Assets/Stickers) after a line tagged with Helpers/Core/Sticker.
// BotChat reaches TryFollowUp through its static FollowUp hook, set in Program.cs; /debug sticker
// calls PostAsync directly. An ordinary tag wins 1 in 3; a sure tag (the moment pools, and
// WALL-E's robot-heart lines once RobotHeartService has rolled their own 2 in 3) skips the roll.
// At most one strip per channel every five minutes, sure tags included; no daily cap. Nothing
// during a breakdown — except the breakdown's own glitch strip, which goes straight through
// SendAsync, like the glitched wake's.
//
// A singleton, like the other services BotChat's static hook reaches: the channel gate is its state.
public sealed class StickerService
{
    public const double Chance = 1.0 / 3;

    // Between two strips in the same channel: a run of tagged lines would otherwise post a wall
    // of pictures.
    public static readonly TimeSpan ChannelCooldown = TimeSpan.FromMinutes(5);

    // The beat between the line and its picture, so the strip reads as her reaction to it.
    public static readonly TimeSpan Beat = TimeSpan.FromSeconds(1);

    private readonly BreakdownService _breakdown;
    private readonly ILogger<StickerService> _logger;
    private readonly CooldownGate<ulong> _channelGate = new(ChannelCooldown);

    public StickerService(BreakdownService breakdown, ILogger<StickerService> logger)
    {
        _breakdown = breakdown;
        _logger = logger;
    }

    /// <summary>
    /// The gate alone: the roll for an ordinary tag, no strip in a channel in breakdown, and the
    /// channel's cooldown, claimed last so a lost roll doesn't spend it. Public so it can be
    /// checked without a gateway.
    /// </summary>
    public bool TryAdmit(ulong channelId, StickerTag tag)
    {
        if (!tag.Sure && Random.Shared.NextDouble() >= Chance) return false;
        if (_breakdown.IsActive(channelId)) return false;
        return _channelGate.TryClaim(channelId);
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
    public async Task<bool> PostAsync(IMessageChannel channel, string mood) =>
        await SendAsync(channel, mood, _logger) is not null;

    /// <summary>
    /// <see cref="PostAsync"/> without the service, returning the message (null: it failed). For
    /// the glitch strip's hand-placed sends: BreakdownService can't inject this service (it
    /// depends on BreakdownService), and the glitched wake deletes its strip afterwards.
    /// </summary>
    public static async Task<IUserMessage?> SendAsync(IMessageChannel channel, string mood, ILogger logger)
    {
        try
        {
            return await channel.SendFileAsync(Sticker.FileFor(mood), text: null, allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to post the {Mood} sticker in channel {ChannelId}.", mood, channel.Id);
            return null;
        }
    }
}
