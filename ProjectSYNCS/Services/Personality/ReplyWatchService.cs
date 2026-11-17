using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Her replies, watched for a while: when the message she answered is deleted, or edited into
/// something else (<see cref="EditReading"/>: a real change, never a typo), she rewrites her reply —
/// her old words struck through, and under them suspicion, a pout, or « trop tard, j'ai lu ».
/// </summary>
/// <remarks>
/// <para>ChatterService records each reply it sends (<see cref="Watch"/>); BotService feeds
/// <c>MessageUpdated</c> and <c>MessageDeleted</c> here. Singleton, in memory, bounded: a restart
/// forgets every watch by design.</para>
/// <para>One rewrite per reply: the watch is taken before anything is sent, so a second edit, or an
/// edit then a deletion, changes nothing more.</para>
/// </remarks>
public sealed class ReplyWatchService
{
    /// <summary>How long after her reply an edit or a deletion still counts.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromHours(1);

    // Plenty for an hour of her replies; past it the oldest go first.
    private const int Capacity = 300;

    // The pause before she rewrites, so it reads as noticing rather than a trigger.
    private const int MinPauseMs = 1500;
    private const int MaxPauseMs = 3500;

    private sealed record Watched(
        IUserMessage Hers, string HerLine, string Name, string Content, bool WasMean, bool IsOwner, DateTimeOffset At);

    private readonly object _lock = new();
    private readonly Dictionary<ulong, Watched> _byTheirs = new();
    private readonly Queue<ulong> _order = new();

    private readonly ResponsePicker _picker;
    private readonly BreakdownService _breakdown;
    private readonly ILogger<ReplyWatchService> _logger;

    public ReplyWatchService(ResponsePicker picker, BreakdownService breakdown, ILogger<ReplyWatchService> logger)
    {
        _picker = picker;
        _breakdown = breakdown;
        _logger = logger;
    }

    /// <summary>
    /// Watch <paramref name="hers"/>, her reply to <paramref name="theirs"/>. <paramref name="herLine"/>
    /// is what she meant to say (sticker marker stripped), <paramref name="wasMean"/> whether she read
    /// their message as mean.
    /// </summary>
    public void Watch(IUserMessage theirs, IUserMessage hers, string herLine, string name, bool wasMean, bool isOwner)
    {
        if (theirs.Author.IsBot || theirs.Channel is not IGuildChannel) return;
        lock (_lock)
        {
            if (!_byTheirs.ContainsKey(theirs.Id)) _order.Enqueue(theirs.Id);
            _byTheirs[theirs.Id] = new Watched(hers, herLine, name, theirs.Content ?? string.Empty, wasMean, isOwner,
                DateTimeOffset.UtcNow);
            while (_order.Count > Capacity) _byTheirs.Remove(_order.Dequeue());
        }
    }

    /// <summary>BotService's <c>MessageUpdated</c>: a watched message edited for real.</summary>
    public Task HandleUpdatedAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
    {
        if (after.Author.IsBot) return Task.CompletedTask;
        Watched? watched;
        EditKind kind;
        lock (_lock)
        {
            if (!_byTheirs.TryGetValue(after.Id, out watched)) return Task.CompletedTask;
            var content = after.Content ?? string.Empty;
            kind = EditReading.Read(watched.Content, content, watched.WasMean, ChatterService.IsMean(content));
            // A link preview or a typo: keep watching, the next edit may be the real one.
            if (kind == EditKind.None) return Task.CompletedTask;
            _byTheirs.Remove(after.Id);
        }
        Rewrite(watched, LinesFor(kind, watched.IsOwner), $"edit ({kind})");
        return Task.CompletedTask;
    }

    /// <summary>BotService's <c>MessageDeleted</c>: a watched message gone.</summary>
    public Task HandleDeletedAsync(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel)
    {
        Watched? watched;
        lock (_lock)
            if (!_byTheirs.Remove(message.Id, out watched)) return Task.CompletedTask;
        Rewrite(watched, watched.IsOwner ? BotResponses.OwnerEditLines : BotResponses.EditDeletedLines, "deletion");
        return Task.CompletedTask;
    }

    private static string[] LinesFor(EditKind kind, bool isOwner) => (kind, isOwner) switch
    {
        (EditKind.TurnedMean, true) => BotResponses.OwnerEditMeanLines,
        (_, true) => BotResponses.OwnerEditLines,
        (EditKind.TurnedMean, false) => BotResponses.EditTurnedMeanLines,
        (EditKind.CoveredUp, false) => BotResponses.EditCoveredUpLines,
        _ => BotResponses.EditRewrittenLines,
    };

    // Off the gateway handler: the pause would hold up every handler after this one. Swallowed and
    // logged: a reply already deleted by a moderator, or a lost permission, changes nothing.
    private void Rewrite(Watched watched, string[] pool, string what)
    {
        if (DateTimeOffset.UtcNow - watched.At > Window) return;
        _ = Task.Run(async () =>
        {
            try
            {
                if (_breakdown.IsActive(watched.Hers.Channel.Id)) return;
                await Task.Delay(Random.Shared.Next(MinPauseMs, MaxPauseMs));
                var line = Sticker.Strip(string.Format(_picker.Pick(pool), watched.Name));
                await watched.Hers.ModifyAsync(p =>
                {
                    p.Content = EditReading.Struck(watched.HerLine, line);
                    p.AllowedMentions = AllowedMentions.None;
                });
                _logger.LogInformation("Rewrote her reply after an {What} of the message she answered.", what);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not rewrite her reply after an {What}.", what);
            }
        });
    }
}
