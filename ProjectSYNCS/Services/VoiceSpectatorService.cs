using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The jealous spectator: she organises the game nights and can't play in them. When a voice
// get-together of an hour or more ends (Helpers/VoiceStretch), she posts one line in that voice
// channel's own text chat, at most once a day, home guild only. The pool follows what the channel
// is for (Helpers/VoiceRoom): a game night, a film, a study session or just talking.
//
// No loop and no gateway subscription of its own: VoiceXpService's one-minute sweep already reads
// every voice channel's live state, and feeds it here. A singleton: the per-channel stretches are
// in-memory state (a restart forgets a session in progress, by design).
internal sealed class VoiceSpectatorService
{
    private const ulong SpectatorSalt = 0x5359_4E43_5650_4543; // "SYNCVPEC"

    private readonly XpTracker _xp;
    private readonly BreakdownService _breakdown;
    private readonly ILogger<VoiceSpectatorService> _logger;

    // Guards both. The sweep is sequential today; the lock keeps it safe if that changes.
    private readonly object _gate = new();
    private readonly Dictionary<ulong, VoiceStretch> _stretches = new();
    private int _lastLineDay;

    public VoiceSpectatorService(XpTracker xp, BreakdownService breakdown, ILogger<VoiceSpectatorService> logger)
    {
        _xp = xp;
        _breakdown = breakdown;
        _logger = logger;
    }

    /// <summary>
    /// One sweep of one voice channel: <paramref name="active"/> people taking part (voice XP's
    /// rule), <paramref name="humans"/> people connected at all, <paramref name="tick"/> the
    /// sweep's interval.
    /// </summary>
    public async Task ObserveAsync(SocketVoiceChannel channel, int active, int humans, TimeSpan tick)
    {
        if (channel.Guild.Id != HomeGuild.Id) return;

        TimeSpan? ended;
        lock (_gate)
        {
            if (!_stretches.TryGetValue(channel.Id, out var stretch))
            {
                // Nothing going on and nothing to remember: don't keep a row for every empty channel.
                if (active < 2) return;
                _stretches[channel.Id] = stretch = new VoiceStretch();
            }
            ended = stretch.Observe(active, humans, tick);
        }
        if (ended is not { } together) return;

        var today = AppTime.DayKey(DateTimeOffset.UtcNow);
        lock (_gate)
            if (_lastLineDay == today) return;

        if (_breakdown.IsActive(channel.Id)) return;
        // Same exclusions as voice XP: a channel the server keeps out of everything stays out.
        if (await _xp.IsChannelExcludedAsync(channel.Guild.Id, channel)) return;

        var room = VoiceRooms.For(channel.Id);
        var pool = room switch
        {
            VoiceRoom.Gaming => BotResponses.VoiceSpectatorGamingLines,
            VoiceRoom.Cinema => BotResponses.VoiceSpectatorCinemaLines,
            VoiceRoom.Study => BotResponses.VoiceSpectatorStudyLines,
            _ => BotResponses.VoiceSpectatorGeneralLines,
        };
        // Each pool gets its own order: the salt moves with the room.
        var line = string.Format(
            DailyRotation.Pick(pool, AppTime.DayNumber(DateTimeOffset.UtcNow), SpectatorSalt + (ulong)room),
            LevelCardUi.Duration((long)together.TotalMinutes));
        // A voice channel carries its own text chat: the line lands where the session was.
        var sent = await BotChat.PostWithTypingAsync(channel, line, _logger, "voice spectator line", AllowedMentions.None);
        if (sent is null) return;

        lock (_gate) _lastLineDay = today;
        _logger.LogInformation("Spectator: {Minutes:0} min together in {ChannelId} ({Room}), line posted.", together.TotalMinutes, channel.Id, room);
    }
}
