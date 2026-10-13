using Discord;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Posts one of her sticker strips (Assets/Stickers) after a line tagged with Helpers/Core/Sticker.
// BotChat reaches TryFollowUp through its static FollowUp hook, set in Program.cs; /debug sticker
// calls PostAsync directly. Rare by construction: an ordinary tag wins 1 in 3, a sure tag (the
// moment pools) skips the roll, and both respect a cooldown per channel and a daily cap — a strip
// is big, and posted often it becomes noise.
//
// A singleton: the gates are in-memory state and reset on restart, by design. Its own gates,
// shared with nobody.
public sealed class StickerService
{
    public const double Chance = 1.0 / 3;
    public static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(30);
    public const int DailyCap = 12;

    // The beat between the line and its picture, so the strip reads as her reaction to it.
    private static readonly TimeSpan Beat = TimeSpan.FromSeconds(1);

    private readonly BreakdownService _breakdown;
    private readonly ILogger<StickerService> _logger;

    // Keyed by channel id, DMs included. A day is ample before an idle channel is forgotten.
    private readonly CooldownGate<ulong> _channels = new(Cooldown, TimeSpan.FromDays(1));

    private readonly object _lock = new();
    private int _day;
    private int _postedToday;

    public StickerService(BreakdownService breakdown, ILogger<StickerService> logger)
    {
        _breakdown = breakdown;
        _logger = logger;
    }

    /// <summary>
    /// The gate alone. The cheapest and non-consuming checks first: a lost roll, a breakdown in
    /// progress or a full day uses up nothing; only an admitted strip claims the channel and
    /// counts toward the day. Public so it can be checked without a gateway.
    /// </summary>
    public bool TryAdmit(ulong channelId, StickerTag tag)
    {
        if (!tag.Sure && Random.Shared.NextDouble() >= Chance) return false;
        if (_breakdown.IsActive(channelId)) return false;

        lock (_lock)
        {
            var today = AppTime.TodayKey;
            if (today != _day)
            {
                _day = today;
                _postedToday = 0;
            }
            if (_postedToday >= DailyCap) return false;
            if (!_channels.TryClaim(channelId)) return false;
            _postedToday++;
            return true;
        }
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
    /// Attach Files just gets the line. The claim is kept on failure — a missing permission is
    /// persistent, and releasing it would retry and log on every line.
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
