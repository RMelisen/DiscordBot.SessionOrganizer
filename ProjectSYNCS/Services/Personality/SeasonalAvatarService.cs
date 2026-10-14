using System.Security.Cryptography;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Dresses her for the season: the bot's profile picture follows ItemCatalog.SeasonAt, switching
// on the first day of each season (Paris calendar). The pictures ship in Assets/Avatars
// (docs/syncs-appearance.md, "Seasonal avatars"): an animated GIF per season, made from the PNG
// by tools/syncs-avatar, and the PNG itself as the fallback when a GIF is missing.
//
// Discord rate-limits avatar changes hard, so the picture is uploaded only when it differs from
// the last one applied: avatar-state.json, next to the database, keeps the season and a
// fingerprint of its file. A redrawn picture therefore goes up on its own after an update, and
// a restart costs nothing. A failed upload is logged and retried on the next check.
//
// Its own hourly loop, like the other loops: the season turns at midnight, and an hour late is
// fine. It also checks on every Ready, so a cold start doesn't wait an hour.
internal sealed class SeasonalAvatarService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    // Share of each side kept when uploading: 0.9 trims 5 % off every edge. The GIFs have it baked
    // in by tools/syncs-avatar (ZOOM_KEEP there); keep the two equal.
    private const double ZoomKeep = 0.9;

    private readonly DiscordSocketClient _client;
    private readonly ILogger<SeasonalAvatarService> _logger;
    private readonly string _statePath;
    // Ready and the loop can both check at once; only one may upload.
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SeasonalAvatarService(DiscordSocketClient client, IConfiguration config, ILogger<SeasonalAvatarService> logger)
    {
        _client = client;
        _logger = logger;

        // Next to the database, so it lives under /data in production and survives updates.
        var dbPath = Path.GetFullPath(config["Database:Path"] ?? "ProjectSYNCS.db");
        _statePath = Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", "avatar-state.json");
    }

    private static string AvatarDirectory => Path.Combine(AppContext.BaseDirectory, "Assets", "Avatars");

    private static string FileFor(Season season, string extension) => Path.Combine(AvatarDirectory, season switch
    {
        Season.Spring => "syncs-spring",
        Season.Summer => "syncs-summer",
        Season.Autumn => "syncs-autumn",
        _ => "syncs-winter",
    } + extension);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.Ready += OnReadyAsync;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(CheckInterval, stoppingToken);
                if (_client.ConnectionState != ConnectionState.Connected) continue;
                await CheckAsync();
            }
        }
        finally
        {
            _client.Ready -= OnReadyAsync;
        }
    }

    private Task OnReadyAsync()
    {
        _ = Task.Run(CheckAsync);
        return Task.CompletedTask;
    }

    private async Task CheckAsync()
    {
        if (!await _gate.WaitAsync(0)) return;
        try
        {
            var season = ItemCatalog.SeasonAt(DateTimeOffset.UtcNow);
            // The animated one when it ships, the still PNG otherwise.
            var gif = FileFor(season, ".gif");
            var animated = File.Exists(gif);
            var file = animated ? gif : FileFor(season, ".png");
            if (!File.Exists(file))
            {
                _logger.LogWarning("Seasonal avatar {File} is missing; keeping the current picture.", file);
                return;
            }

            var key = $"{season}:{Fingerprint(file, animated)}";
            if (LoadState().Applied == key) return;

            // Zoomed in a little for the round crop: the PNG here, the file on disk staying the full
            // picture; the GIF comes already zoomed (tools/syncs-avatar), as nothing here can crop one.
            var bytes = animated ? File.ReadAllBytes(file) : PngCrop.CenterZoom(file, ZoomKeep);
            using (var stream = new MemoryStream(bytes))
            using (var image = new Image(stream))
                await _client.CurrentUser.ModifyAsync(p => p.Avatar = image);

            SaveState(new AvatarState(key));
            _logger.LogInformation("Avatar switched to {Season}{Animated}.", season, animated ? " (animated)" : "");
            if (animated) await WarnIfStillAsync();
        }
        catch (Exception ex)
        {
            // Rate limits included: the next check tries again.
            _logger.LogWarning(ex, "Could not switch the seasonal avatar.");
        }
        finally
        {
            _gate.Release();
        }
    }

    // Discord.Net labels every upload image/jpeg; Discord reads the bytes, but whether it keeps a
    // GIF animated under that label is only seen afterwards: an animated avatar's hash starts "a_".
    // Not retried (avatar changes are rate-limited hard): the warning is for the owner.
    private async Task WarnIfStillAsync()
    {
        try
        {
            await _client.Rest.CurrentUser.UpdateAsync();
            var hash = _client.Rest.CurrentUser.AvatarId;
            if (hash?.StartsWith("a_") != true)
                _logger.LogWarning("Seasonal avatar: the GIF went up but Discord shows it still (hash {Hash}).", hash);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Seasonal avatar: could not check whether the GIF stayed animated.");
        }
    }

    // The zoom is part of the PNG's fingerprint, since it is applied here; the GIF's is in the file.
    private static string Fingerprint(string file, bool animated) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)))[..16] + (animated ? "" : $"@{ZoomKeep}");

    // What a restart must not forget: which picture is already up.
    private sealed record AvatarState(string? Applied);

    private AvatarState LoadState()
    {
        try
        {
            if (File.Exists(_statePath))
                return JsonSerializer.Deserialize<AvatarState>(File.ReadAllText(_statePath))
                       ?? new AvatarState(null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Seasonal avatar: could not read {Path}; uploading again.", _statePath);
        }
        return new AvatarState(null);
    }

    private void SaveState(AvatarState state)
    {
        try
        {
            File.WriteAllText(_statePath, JsonSerializer.Serialize(state));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Seasonal avatar: could not write {Path}.", _statePath);
        }
    }
}
