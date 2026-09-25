using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Makes sure the bot's own application emojis exist for every item sprite it ships with (the
// Champignons, in Assets/Mushrooms), then records their markup in ItemEmojis.
//
// Application emojis belong to the bot's application rather than to a server, so the bot can
// show them in any server it is in, and each bot — dev or prod — keeps its own copy: nobody has
// to upload anything by hand, and a dev bot that is a different application still gets them.
// A sprite is uploaded only when no emoji of its name exists yet, so a normal start costs one
// list request. To replace a picture, delete that emoji in the developer portal and restart.
//
// Not a loop: it runs once, on the first Ready (Ready fires again on every reconnect), in the
// background so the gateway handler returns at once — 30 first-time uploads take a while. A
// failure to *list* allows another try on the next Ready; a failed upload is logged and that
// item keeps its 🍄 fallback. Hooks Ready itself, like PresenceService, since nothing else in
// BotService needs to know.
internal sealed class ApplicationEmojiService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<ApplicationEmojiService> _logger;
    private int _started;

    public ApplicationEmojiService(DiscordSocketClient client, ILogger<ApplicationEmojiService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public static string SpriteDirectory => Path.Combine(AppContext.BaseDirectory, "Assets", "Mushrooms");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Ready += OnReadyAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client.Ready -= OnReadyAsync;
        return Task.CompletedTask;
    }

    private Task OnReadyAsync()
    {
        if (Interlocked.Exchange(ref _started, 1) == 0)
            _ = Task.Run(SyncAsync);
        return Task.CompletedTask;
    }

    private async Task SyncAsync()
    {
        IReadOnlyCollection<Emote> existing;
        try
        {
            existing = await _client.GetApplicationEmotesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not list the application emojis; items keep their fallback until the next Ready.");
            Interlocked.Exchange(ref _started, 0);
            return;
        }

        var byName = existing.ToDictionary(e => e.Name, StringComparer.Ordinal);
        var uploaded = 0;
        foreach (var file in Directory.Exists(SpriteDirectory) ? Directory.GetFiles(SpriteDirectory, "*.png") : Array.Empty<string>())
        {
            var slug = Path.GetFileNameWithoutExtension(file);
            var key = $"col.{slug}";
            if (ItemCatalog.ByKey(key) is null)
            {
                _logger.LogWarning("Sprite {File} matches no item; skipped.", Path.GetFileName(file));
                continue;
            }

            var name = ItemEmojis.MushroomPrefix + slug;
            try
            {
                if (!byName.TryGetValue(name, out var emote))
                {
                    using var image = new Image(file);
                    emote = await _client.CreateApplicationEmoteAsync(name, image);
                    uploaded++;
                }
                ItemEmojis.Set(key, ItemEmojis.Markup(emote.Name, emote.Id));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not upload the {Name} emoji; that item keeps its fallback.", name);
            }
        }
        _logger.LogInformation("Item emojis ready: {Count} in use, {Uploaded} uploaded now.", ItemEmojis.Count, uploaded);
    }
}
