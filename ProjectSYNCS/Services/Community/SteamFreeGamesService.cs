using System.Text.Json;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Steam games that turn free to keep: every 3 hours she reads Steam's store (Helpers/Community/
// SteamFreeGames, no key), and each game she hasn't announced yet goes to every guild's
// /config freegames-channel, with a card and a line of hers. Free weekends, free-to-play games
// and DLC are left out on purpose: only what you keep.
//
// Its own 3-hour interval, like every loop here (root CLAUDE.md): Steam's giveaways last days, so
// a few hours late is invisible. The first pass runs a few minutes after start, so a day of
// restarts can't keep pushing it back.
//
// freegames-state.json (next to the database, so under /data) remembers what was announced, so a
// restart never posts a game twice. A game drops out of it a month after it stopped being free,
// so a later giveaway of the same game is announced again. Nothing is marked while she is silent,
// or while no guild has a channel: the game then goes out once that changes, if it is still free.
public sealed class SteamFreeGamesService : BackgroundService
{
    private static readonly TimeSpan FirstDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(3);
    private static readonly TimeSpan ForgetAfter = TimeSpan.FromDays(30);

    private sealed record State(Dictionary<uint, DateTimeOffset> Announced);

    private readonly DiscordSocketClient _client;
    private readonly GuildConfigService _config;
    private readonly ResponsePicker _picker;
    private readonly SilenceService _silence;
    private readonly ILogger<SteamFreeGamesService> _logger;
    private readonly string _statePath;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private readonly Dictionary<uint, DateTimeOffset> _announced;

    // Ids the details call turned down (a DLC slipping through, a free-to-play game), so they
    // aren't asked about again every pass. In memory: a restart asks once more, which is cheap.
    private readonly HashSet<uint> _rejected = new();

    public SteamFreeGamesService(DiscordSocketClient client, GuildConfigService config, ResponsePicker picker,
        SilenceService silence, IConfiguration configuration, ILogger<SteamFreeGamesService> logger)
    {
        _client = client;
        _config = config;
        _picker = picker;
        _silence = silence;
        _logger = logger;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("ProjectSYNCS (Discord bot)");

        var dbPath = Path.GetFullPath(configuration["Database:Path"] ?? "ProjectSYNCS.db");
        _statePath = Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", "freegames-state.json");
        _announced = LoadState();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(FirstDelay, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_client.ConnectionState == ConnectionState.Connected)
            {
                // One try around the fetch; the posts below catch per game and per guild, so one
                // bad game or channel never stops the rest. An escaping exception stops the host.
                try
                {
                    await TickAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Steam free games pass failed.");
                }
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        if (_silence.IsSilent) return;

        var channels = await ResolveChannelsAsync();
        if (channels.Count == 0) return;

        var freeIds = SteamFreeGames.ParseSearch(await _http.GetStringAsync(SteamFreeGames.SearchUrl, ct));
        Forget(freeIds);

        foreach (var appId in freeIds)
        {
            if (_announced.ContainsKey(appId) || _rejected.Contains(appId)) continue;

            SteamFreeGame? game;
            try
            {
                game = SteamFreeGames.ParseDetails(appId, await _http.GetStringAsync(SteamFreeGames.DetailsUrl(appId), ct));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Not rejected: a network hiccup is asked about again next pass.
                _logger.LogWarning(ex, "Could not read Steam's details for app {AppId}.", appId);
                continue;
            }

            if (game is null)
            {
                _rejected.Add(appId);
                continue;
            }

            foreach (var channel in channels) await PostAsync(channel, game);

            // Marked once every channel had its try: a channel that failed isn't worth posting the
            // game again to the ones that got it.
            _announced[appId] = DateTimeOffset.UtcNow;
            SaveState();
        }
    }

    private async Task<List<ITextChannel>> ResolveChannelsAsync()
    {
        var channels = new List<ITextChannel>();
        foreach (var guild in _client.Guilds)
        {
            var config = await _config.GetAsync(guild.Id);
            if (config.FreeGamesChannelId != 0 && guild.GetTextChannel(config.FreeGamesChannelId) is { } channel)
                channels.Add(channel);
        }
        return channels;
    }

    private async Task PostAsync(ITextChannel channel, SteamFreeGame game)
    {
        try
        {
            var line = string.Format(_picker.Pick(BotResponses.FreeGameLines),
                $"**{Format.Sanitize(game.Name)}**",
                string.IsNullOrEmpty(game.NormalPrice) ? "payant" : game.NormalPrice);

            await channel.SendMessageAsync(line,
                embed: BuildCard(game),
                components: new ComponentBuilder()
                    .WithButton("Voir sur Steam", style: ButtonStyle.Link, url: game.StoreUrl)
                    .Build(),
                allowedMentions: AllowedMentions.None);

            _logger.LogInformation("Announced free Steam game {AppId} in channel {ChannelId}.", game.AppId, channel.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to announce free Steam game {AppId} in channel {ChannelId}.", game.AppId, channel.Id);
        }
    }

    /// <summary>The card under her line: the game, its normal price struck out, Steam's banner.</summary>
    public static Embed BuildCard(SteamFreeGame game)
    {
        var description = game.Description.Length > 300 ? game.Description[..297].TrimEnd() + "…" : game.Description;
        var price = string.IsNullOrEmpty(game.NormalPrice)
            ? "**Gratuit**"
            : $"~~{game.NormalPrice}~~ → **Gratuit**";

        var embed = new EmbedBuilder()
            .WithTitle(game.Name.Length > 200 ? game.Name[..200] : game.Name)
            .WithUrl(game.StoreUrl)
            .WithDescription(string.IsNullOrEmpty(description) ? price : $"{description}\n\n{price}")
            .WithColor(new Color(0x1B2838))
            .WithFooter("Steam · gratuit à garder");
        if (!string.IsNullOrEmpty(game.HeaderImage)) embed.WithImageUrl(game.HeaderImage);
        return embed.Build();
    }

    // Drops games that stopped being free a while ago, so a later giveaway of one is news again.
    private void Forget(IReadOnlyList<uint> stillFree)
    {
        var cutoff = DateTimeOffset.UtcNow - ForgetAfter;
        var stale = _announced
            .Where(kv => kv.Value < cutoff && !stillFree.Contains(kv.Key))
            .Select(kv => kv.Key)
            .ToList();
        if (stale.Count == 0) return;
        foreach (var id in stale) _announced.Remove(id);
        SaveState();
    }

    private Dictionary<uint, DateTimeOffset> LoadState()
    {
        try
        {
            if (File.Exists(_statePath)
                && JsonSerializer.Deserialize<State>(File.ReadAllText(_statePath)) is { Announced: { } announced })
                return announced;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read {Path}; starting with no announced games.", _statePath);
        }
        return new Dictionary<uint, DateTimeOffset>();
    }

    private void SaveState()
    {
        try
        {
            File.WriteAllText(_statePath, JsonSerializer.Serialize(new State(_announced)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write {Path}; a restart may announce a game twice.", _statePath);
        }
    }

    public override void Dispose()
    {
        _http.Dispose();
        base.Dispose();
    }
}
