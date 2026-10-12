using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Makes sure the bot's own application emojis exist for every sprite it ships with — the
// Champignons (Assets/Mushrooms) and the other icons (Assets/Icons: collectibles, sets, cosmetics
// and Plynling traits) — then records their markup in ItemEmojis.
//
// Application emojis belong to the bot's application rather than to a server, so the bot can
// show them in any server it is in, and each bot — dev or prod — keeps its own copy: nobody has
// to upload anything by hand, and a dev bot that is a different application still gets them.
// A sprite is uploaded only when no emoji of its name exists yet, so a normal start costs one
// list request. To replace a picture, delete that emoji in the developer portal and restart.
//
// Not a loop: it runs once, on the first Ready (Ready fires again on every reconnect), in the
// background so the gateway handler returns at once — up to 186 first-time uploads take a while. A
// failure to *list* allows another try on the next Ready; a failed upload is logged and that
// item keeps its Unicode fallback. Hooks Ready itself, like PresenceService, since nothing else in
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
        foreach (var source in ItemEmojis.Sources(ItemEmojis.MushroomDirectory, ItemEmojis.IconDirectory))
        {
            if (source.EmojiName is not { } name)
            {
                _logger.LogWarning("Sprite {File} matches no item, set, cosmetic or trait; skipped.", Path.GetFileName(source.File));
                continue;
            }

            try
            {
                if (!byName.TryGetValue(name, out var emote))
                {
                    using var image = new Image(source.File);
                    emote = await _client.CreateApplicationEmoteAsync(name, image);
                    uploaded++;
                }
                ItemEmojis.Set(source.Key, ItemEmojis.Markup(emote.Name, emote.Id));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not upload the {Name} emoji; that item keeps its fallback.", name);
            }
        }
        _logger.LogInformation("Item emojis ready: {Count} in use, {Uploaded} uploaded now.", ItemEmojis.Count, uploaded);
    }
}
