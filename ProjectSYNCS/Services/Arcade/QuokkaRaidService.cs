using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Whack-a-Quokka: up to four times a day (12:00–23:00 Paris, home guild only) Quokka slips into
/// the bots channel, and the first to hit him wins a few cailloux. No interval, like
/// MorningGreetingService: it sleeps until each of the day's slots, which are computed from the
/// date (<see cref="QuokkaRaid.SlotsFor"/>) — a restart finds the same ones, nothing is stored.
/// A slot is skipped when nobody has spoken in the server for half an hour: he never shows up to
/// an empty room. BotService feeds it the messages that keep that clock.
/// </summary>
public sealed class QuokkaRaidService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly ArcadeTables _tables;
    private readonly PartyRunner _party;
    private readonly GuildConfigService _config;
    private readonly PlynlingAnnouncer _announcer;
    private readonly ResponsePicker _picker;
    private readonly IServiceProvider _services;
    private readonly ILogger<QuokkaRaidService> _logger;

    private long _lastActivityTicks;

    public QuokkaRaidService(DiscordSocketClient client, ArcadeTables tables, PartyRunner party, GuildConfigService config,
        PlynlingAnnouncer announcer, ResponsePicker picker, IServiceProvider services, ILogger<QuokkaRaidService> logger)
    {
        _client = client;
        _tables = tables;
        _party = party;
        _config = config;
        _announcer = announcer;
        _picker = picker;
        _services = services;
        _logger = logger;
    }

    /// <summary>A human spoke in the home guild, in a channel that counts: the room is awake.</summary>
    public async Task HandleMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot || message.Channel is not SocketGuildChannel { Guild.Id: HomeGuild.Id } channel) return;
        try
        {
            if (await _services.GetRequiredService<XpTracker>().IsChannelExcludedAsync(HomeGuild.Id, channel)) return;
        }
        catch
        {
            return;
        }
        Interlocked.Exchange(ref _lastActivityTicks, DateTimeOffset.UtcNow.UtcTicks);
    }

    private bool RoomAwake =>
        DateTimeOffset.UtcNow - new DateTimeOffset(Interlocked.Read(ref _lastActivityTicks), TimeSpan.Zero) <= QuokkaRaid.ActiveWithin;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var today = AppTime.DayKey(now);
            var next = QuokkaRaid.SlotsFor(today).Concat(QuokkaRaid.SlotsFor(PlynlingLife.NextDay(today)))
                .FirstOrDefault(t => t > now);
            if (next == default)
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                continue;
            }

            await Task.Delay(next - now, stoppingToken);
            try
            {
                if (_client.ConnectionState == ConnectionState.Connected && RoomAwake)
                    await SpawnAsync(HomeGuild.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Quokka's visit failed.");
            }
            // Past this slot either way.
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    /// <summary>Quokka shows up now (also /debug quokka). False when he couldn't (no channel, already there).</summary>
    public async Task<bool> SpawnAsync(ulong guildId)
    {
        var channel = await BotsChannel.ResolveAsync(_client, _config, _announcer, guildId);
        if (channel is null) return false;

        var visit = new QuokkaVisit { GuildId = guildId, ChannelId = channel.Id, Reward = QuokkaRaid.Reward(Random.Shared) };
        var (opened, _) = _tables.TryOpen(visit, g => g is QuokkaVisit && g.GuildId == guildId);
        if (opened is null) return false;

        try
        {
            visit.Line = _picker.Pick(BotResponses.QuokkaRaidLines);
            var message = await channel.SendMessageAsync(embed: PartyCards.QuokkaCard(visit), components: PartyCards.QuokkaButtons(visit),
                allowedMentions: AllowedMentions.None);
            visit.MessageId = message.Id;
            _party.ScheduleQuokka(visit);
            return true;
        }
        catch
        {
            _tables.End(visit.Id);
            throw;
        }
    }
}
