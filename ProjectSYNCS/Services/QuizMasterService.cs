using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>What came of a click on one of the quiz card's buttons.</summary>
public enum QuizPickResult
{
    /// <summary>The round or its question no longer exists.</summary>
    Gone,
    /// <summary>Already won, timed out, or past its closing time.</summary>
    TooLate,
    /// <summary>This person already had their one try on this round.</summary>
    AlreadyTried,
    /// <summary>Wrong choice; this person is out of the round.</summary>
    Wrong,
    /// <summary>Right, and first: paid, card closed, win announced.</summary>
    Won,
}

// Her pop quiz: posts a question up to twice a day in the guild's quiz channel
// (/config quiz-channel), takes the first right answer, pays it, and reveals the answer
// when nobody finds it within the hour. When to post is Helpers/QuizSchedule, what to ask
// is Helpers/QuizBank, how a typed answer is judged is Helpers/QuizAnswer.
//
// **Registered twice on one instance**, like MorningGreetingService: the host runs its
// loop, BotService feeds it every message (typed answers, and the channel's quiet clock),
// and QuizComponentHandler and /debug quiz call into it. Public for those two, so the
// internal BreakdownService is resolved from the provider rather than injected.
//
// **Its own 1-minute tick**, shared with no other loop: a round must close close to the
// hour it announced, and a slot waiting for someone to show up should catch them quickly.
//
// **Rounds are stored, the rest is in memory.** A restart resumes every open round (the
// buttons find it by id; the first tick reloads the typed-answer cache and closes whatever
// came due meanwhile). Lost on restart, by design: today's slots (redrawn, past ones
// dropped), the channel's quiet clock (reseeded from history), and who already tried a
// multiple-choice round — so after a restart someone who clicked wrong may click again.
//
// **A win happens once**: an in-memory claim (_claimed) stops a second caller before any
// database work, and QuizService.RecordWinAsync's conditional update stops anything the
// claim can't see (a timeout racing the win, a restart). A claim is released if the save
// throws, or the round would be stuck unwinnable for something that never happened.
public sealed class QuizMasterService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan FirstDelay = TimeSpan.FromSeconds(30);

    // How far back to look for a human message when the channel's quiet clock is unknown.
    private const int SeedDepth = 20;

    // DailyRotation salts, keyed by the guild's round count (questions) or the round id
    // (her lines): spent twice a day at most, so ResponsePicker's history would be wiped by
    // restarts long before it helped. Changing a salt reshuffles that walk.
    private const ulong QuestionSalt = 0x5359_4E43_5351_5545; // "SYNCSQUE"
    private const ulong IntroSalt = 0x5359_4E43_5351_494E;    // "SYNCSQIN"
    private const ulong WinSalt = 0x5359_4E43_5351_5749;      // "SYNCSQWI"
    private const ulong TimeoutSalt = 0x5359_4E43_5351_544F;  // "SYNCSQTO"

    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly GuildConfigService _config;
    private readonly ILogger<QuizMasterService> _logger;

    // Guards everything below except _claimed (which owns its own concurrency).
    private readonly object _gate = new();

    // Today's slots per guild, drawn on the first tick of the Paris day.
    private readonly Dictionary<ulong, (int Day, List<DateTimeOffset> Slots)> _plans = new();

    // When a human last spoke, per channel. Fed by every message; seeded from history.
    private readonly Dictionary<ulong, DateTimeOffset> _lastHumanAt = new();

    // Open rounds by channel (one per guild at most), for typed answers and the closing pass.
    private readonly Dictionary<ulong, QuizRound> _openByChannel = new();
    private bool _loaded;

    // Who already had their one click, per multiple-choice round.
    private readonly Dictionary<int, HashSet<ulong>> _tried = new();

    private readonly ConcurrentDictionary<int, byte> _claimed = new();

    // One post at a time, so the loop and /debug quiz can't both open a round in a guild.
    private readonly SemaphoreSlim _postGate = new(1, 1);

    public QuizMasterService(
        DiscordSocketClient client,
        IServiceProvider services,
        GuildConfigService config,
        ILogger<QuizMasterService> logger)
    {
        _client = client;
        _services = services;
        _config = config;
        _logger = logger;
    }

    private BreakdownService Breakdown => _services.GetRequiredService<BreakdownService>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QuizMasterService started. Checking every {Interval} minute(s).",
            CheckInterval.TotalMinutes);

        await Task.Delay(FirstDelay, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            // Posting or editing into a stale gateway cache would fail every send; the
            // rounds keep until the next pass.
            if (_client.ConnectionState == ConnectionState.Connected)
                await TickAsync(DateTimeOffset.UtcNow);

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task TickAsync(DateTimeOffset now)
    {
        try
        {
            await EnsureLoadedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the open quiz rounds.");
            return;
        }

        List<QuizRound> due;
        lock (_gate) due = _openByChannel.Values.Where(r => r.ClosesAt <= now).ToList();
        foreach (var round in due)
        {
            try
            {
                await TimeOutAsync(round);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to close quiz round {RoundId}.", round.Id);
            }
        }

        foreach (var guild in _client.Guilds)
        {
            try
            {
                await PostIfDueAsync(guild, now);
            }
            catch (Exception ex)
            {
                // One guild's failure must not keep the others from their quiz.
                _logger.LogError(ex, "Quiz scheduling failed in guild {GuildId}.", guild.Id);
            }
        }
    }

    private async Task EnsureLoadedAsync()
    {
        lock (_gate)
        {
            if (_loaded) return;
        }

        await using var scope = _services.CreateAsyncScope();
        var open = await scope.ServiceProvider.GetRequiredService<QuizService>().GetOpenAsync();

        lock (_gate)
        {
            if (_loaded) return;
            foreach (var round in open) _openByChannel[round.ChannelId] = round;
            _loaded = true;
        }
    }

    // ---- Posting ---------------------------------------------------------------------------------

    private async Task PostIfDueAsync(SocketGuild guild, DateTimeOffset now)
    {
        var config = await _config.GetAsync(guild.Id);
        if (config.QuizChannelId == 0) return;

        DateTimeOffset? slot;
        lock (_gate)
        {
            slot = PlanFor(guild.Id, now).Slots.Where(s => s <= now).Cast<DateTimeOffset?>().FirstOrDefault();
        }
        if (slot is null || IsRoundOpen(guild.Id)) return;

        if (now >= QuizSchedule.DayEndFor(now))
        {
            DropSlots(guild.Id);
            return;
        }

        if (guild.GetTextChannel(config.QuizChannelId) is not { } channel) return;
        if (Breakdown.IsActive(channel.Id)) return;

        // The quiet clock first: it's in memory, and a slot can wait for hours.
        var lastHuman = await LastHumanAtAsync(channel);
        if (lastHuman is null || now - lastHuman > QuizSchedule.ActiveWithin) return;

        await using var scope = _services.CreateAsyncScope();
        var quiz = scope.ServiceProvider.GetRequiredService<QuizService>();
        var (today, lastPosted) = await quiz.GetRecentAsync(guild.Id, AppTime.DayKey(now));

        if (QuizSchedule.DayIsOver(now, today))
        {
            DropSlots(guild.Id);
            return;
        }
        if (!QuizSchedule.CanPost(now, today, lastPosted, lastHuman)) return;

        lock (_gate)
        {
            PlanFor(guild.Id, now).Slots.Remove(slot.Value);
        }
        await PostRoundAsync(guild.Id, channel, forced: null, now);
    }

    // Under _gate.
    private (int Day, List<DateTimeOffset> Slots) PlanFor(ulong guildId, DateTimeOffset now)
    {
        var day = AppTime.DayKey(now);
        if (_plans.TryGetValue(guildId, out var plan) && plan.Day == day) return plan;

        plan = (day, QuizSchedule.DrawSlots(now, Random.Shared));
        _plans[guildId] = plan;
        _logger.LogInformation("Quiz slots for guild {GuildId} today: {Slots}.", guildId,
            plan.Slots.Count == 0
                ? "none"
                : string.Join(", ", plan.Slots.Select(s => AppTime.ToZoned(s).ToString("HH:mm"))));
        return plan;
    }

    private void DropSlots(ulong guildId)
    {
        lock (_gate)
        {
            if (_plans.TryGetValue(guildId, out var plan)) plan.Slots.Clear();
        }
    }

    private bool IsRoundOpen(ulong guildId)
    {
        lock (_gate) return _openByChannel.Values.Any(r => r.GuildId == guildId);
    }

    // The channel's last human message, from the gateway feed, or from its history the
    // first time it is needed. Null when there is none at all.
    private async Task<DateTimeOffset?> LastHumanAtAsync(SocketTextChannel channel)
    {
        lock (_gate)
        {
            if (_lastHumanAt.TryGetValue(channel.Id, out var known))
                return known == DateTimeOffset.MinValue ? null : known;
        }

        var seeded = DateTimeOffset.MinValue;
        try
        {
            var messages = await channel.GetMessagesAsync(SeedDepth).FlattenAsync();
            seeded = messages.Where(m => !m.Author.IsBot)
                .Select(m => m.Timestamp)
                .DefaultIfEmpty(DateTimeOffset.MinValue)
                .Max();
        }
        catch (Exception ex)
        {
            // Unreadable history just means "unknown until someone speaks".
            _logger.LogWarning(ex, "Could not read the history of quiz channel {ChannelId}.", channel.Id);
        }

        lock (_gate)
        {
            // The gateway may have recorded something newer while history was loading.
            if (!_lastHumanAt.TryGetValue(channel.Id, out var current) || current < seeded)
                _lastHumanAt[channel.Id] = current = seeded;
            return current == DateTimeOffset.MinValue ? null : current;
        }
    }

    /// <summary>
    /// /debug quiz: posts a round now in the guild's quiz channel, ignoring the slots, the
    /// daily count and the quiet clock — but never beside one already open. Returns what to
    /// tell the owner.
    /// </summary>
    public async Task<string> PostNowAsync(ulong guildId, string? key)
    {
        await EnsureLoadedAsync();

        var config = await _config.GetAsync(guildId);
        if (config.QuizChannelId == 0)
            return "Aucun salon de quiz configuré ici : `/config quiz-channel set` d'abord.";
        if (_client.GetGuild(guildId)?.GetTextChannel(config.QuizChannelId) is not { } channel)
            return $"Le salon de quiz <#{config.QuizChannelId}> est introuvable.";

        QuizQuestion? forced = null;
        if (!string.IsNullOrWhiteSpace(key) && (forced = QuizBank.ByKey(key.Trim())) is null)
            return $"Clé inconnue : `{key}`.";

        var error = await PostRoundAsync(guildId, channel, forced, DateTimeOffset.UtcNow);
        return error ?? $"🔧 Quiz posté dans <#{channel.Id}>.";
    }

    /// <summary>/debug quiz close: ends the guild's open round now, as the hour running out would.</summary>
    public async Task<string> CloseNowAsync(ulong guildId)
    {
        await EnsureLoadedAsync();

        QuizRound? round;
        lock (_gate) round = _openByChannel.Values.FirstOrDefault(r => r.GuildId == guildId);
        if (round is null) return "Aucun quiz en cours ici.";

        await TimeOutAsync(round);
        return $"🔧 Quiz #{round.Id} fermé.";
    }

    // Returns null on success, else why not. Writes the row before sending, so a click can
    // never reach a round that doesn't exist; a failed send closes the row again.
    private async Task<string?> PostRoundAsync(ulong guildId, SocketTextChannel channel, QuizQuestion? forced,
        DateTimeOffset now)
    {
        await _postGate.WaitAsync();
        try
        {
            if (IsRoundOpen(guildId)) return "Un quiz est déjà en cours sur ce serveur.";

            await using var scope = _services.CreateAsyncScope();
            var quiz = scope.ServiceProvider.GetRequiredService<QuizService>();

            var question = forced ?? QuizBank.All[
                DailyRotation.IndexFor(QuizBank.All.Count, await quiz.CountAsync(guildId), QuestionSalt)];

            var round = await quiz.CreateAsync(new QuizRound
            {
                GuildId = guildId,
                ChannelId = channel.Id,
                QuestionKey = question.Key,
                ChoiceOrder = question.IsChoice ? ShuffledOrder(question.Choices!.Count) : string.Empty,
                PostedAt = now,
                ClosesAt = now + QuizSchedule.OpenFor,
                Day = AppTime.DayKey(now),
                Reward = QuizBank.RewardFor(question.Difficulty),
            });

            var intro = DailyRotation.Pick(BotResponses.QuizIntroLines, round.Id, IntroSalt);

            IUserMessage message;
            try
            {
                message = await channel.SendMessageAsync(
                    components: QuizCards.BuildRound(round, question, intro),
                    flags: MessageFlags.ComponentsV2,
                    allowedMentions: AllowedMentions.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to post quiz round {RoundId} in channel {ChannelId}.", round.Id, channel.Id);
                await quiz.CloseAsync(round.Id);
                return $"Impossible de poster dans <#{channel.Id}>.";
            }

            round.MessageId = message.Id;
            await quiz.SetMessageIdAsync(round.Id, message.Id);

            lock (_gate) _openByChannel[channel.Id] = round;
            _logger.LogInformation("Quiz round {RoundId} ({Key}) posted in guild {GuildId}.", round.Id, question.Key, guildId);
            return null;
        }
        finally
        {
            _postGate.Release();
        }
    }

    private static string ShuffledOrder(int count)
    {
        var order = Enumerable.Range(0, count).ToArray();
        Random.Shared.Shuffle(order);
        return string.Concat(order);
    }

    // ---- Answers ---------------------------------------------------------------------------------

    /// <summary>
    /// Fed every message by BotService. Keeps the channel's quiet clock, and judges a typed
    /// answer to an open question. Returns true when it won the round: BotService then skips
    /// ChatterService, so the winner gets her congratulations, not a comeback.
    /// </summary>
    public async Task<bool> HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message || message.Author.IsBot) return false;
        if (message.Channel is not SocketTextChannel channel) return false;

        var now = DateTimeOffset.UtcNow;
        QuizRound? round;
        lock (_gate)
        {
            _lastHumanAt[channel.Id] = now;
            _openByChannel.TryGetValue(channel.Id, out round);
        }
        if (round is null || round.Closed || now >= round.ClosesAt) return false;

        if (QuizBank.ByKey(round.QuestionKey) is not { IsChoice: false } question) return false;

        var content = message.Content ?? string.Empty;
        if (content.Length == 0 || MessageCues.ReadFeedback(content) != FeedbackKind.None) return false;
        if (Breakdown.IsActive(channel.Id)) return false;
        if (!QuizAnswer.Matches(content, question.Answers)) return false;

        QuizRound? won;
        try
        {
            won = await TryWinAsync(round.Id, message.Author.Id, now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record the quiz win of {UserId} on round {RoundId}.", message.Author.Id, round.Id);
            return false;
        }
        if (won is null) return false;

        await AnnounceWinAsync(won, question, replyTo: message);
        return true;
    }

    /// <summary>
    /// A click on choice <paramref name="position"/> of round <paramref name="roundId"/>.
    /// One try per person; a right first answer wins, closes the card and is announced
    /// here. Throws only if recording the win failed (the caller says so, ephemerally).
    /// </summary>
    public async Task<QuizPickResult> PickAsync(int roundId, ulong userId, int position)
    {
        var now = DateTimeOffset.UtcNow;

        QuizRound? round;
        await using (var scope = _services.CreateAsyncScope())
        {
            round = await scope.ServiceProvider.GetRequiredService<QuizService>().GetAsync(roundId);
        }
        if (round is null || QuizBank.ByKey(round.QuestionKey) is not { IsChoice: true } question)
            return QuizPickResult.Gone;
        if (round.Closed || now >= round.ClosesAt) return QuizPickResult.TooLate;

        lock (_gate)
        {
            if (!_tried.TryGetValue(roundId, out var tried)) _tried[roundId] = tried = new HashSet<ulong>();
            if (!tried.Add(userId)) return QuizPickResult.AlreadyTried;
        }

        if (position != QuizCards.CorrectPosition(round)) return QuizPickResult.Wrong;

        QuizRound? won;
        try
        {
            won = await TryWinAsync(roundId, userId, now);
        }
        catch
        {
            // Nothing happened, so the try isn't spent either.
            lock (_gate)
            {
                if (_tried.TryGetValue(roundId, out var tried)) tried.Remove(userId);
            }
            throw;
        }
        if (won is null) return QuizPickResult.TooLate;

        await AnnounceWinAsync(won, question, replyTo: null);
        return QuizPickResult.Won;
    }

    private async Task<QuizRound?> TryWinAsync(int roundId, ulong userId, DateTimeOffset now)
    {
        if (!_claimed.TryAdd(roundId, 0)) return null;

        try
        {
            await using var scope = _services.CreateAsyncScope();
            var won = await scope.ServiceProvider.GetRequiredService<QuizService>().RecordWinAsync(roundId, userId, now);
            // Null: already won or closed. The claim stays — the round is over either way.
            if (won is not null) Forget(won);
            return won;
        }
        catch
        {
            _claimed.TryRemove(roundId, out _);
            throw;
        }
    }

    // ---- Closing ---------------------------------------------------------------------------------

    private async Task AnnounceWinAsync(QuizRound round, QuizQuestion question, IUserMessage? replyTo)
    {
        var card = await FetchCardAsync(round);
        if (card is not null) await EditCardAsync(card, round, question);

        var pool = round.WinnerId == AvailabilityService.OwnerId
            ? BotResponses.QuizOwnerWinLines
            : BotResponses.QuizWinLines;
        var line = string.Format(
            DailyRotation.Pick(pool, round.Id, WinSalt),
            $"<@{round.WinnerId}>", question.Display, "+" + PebbleEconomy.Cailloux(round.Reward));

        // The mention renders as a pill without pinging: they know they won.
        if ((replyTo ?? card) is { } target)
            await BotChat.ReplyWithTypingAsync(target, line, _logger, "quiz win", AllowedMentions.None);
    }

    private async Task TimeOutAsync(QuizRound round)
    {
        await using (var scope = _services.CreateAsyncScope())
        {
            var closed = await scope.ServiceProvider.GetRequiredService<QuizService>().CloseAsync(round.Id);
            Forget(round);
            // Someone won it a moment ago: that path announces.
            if (!closed) return;
        }

        round.Closed = true;
        if (QuizBank.ByKey(round.QuestionKey) is not { } question)
        {
            _logger.LogWarning("Quiz round {RoundId} closed, but its question {Key} no longer exists.", round.Id, round.QuestionKey);
            return;
        }

        var card = await FetchCardAsync(round);
        if (card is null) return;

        await EditCardAsync(card, round, question);
        var line = string.Format(DailyRotation.Pick(BotResponses.QuizTimeoutLines, round.Id, TimeoutSalt), question.Display);
        await BotChat.ReplyWithTypingAsync(card, line, _logger, "quiz timeout", AllowedMentions.None);
    }

    private void Forget(QuizRound round)
    {
        lock (_gate)
        {
            if (_openByChannel.TryGetValue(round.ChannelId, out var open) && open.Id == round.Id)
                _openByChannel.Remove(round.ChannelId);
            _tried.Remove(round.Id);
        }
    }

    private async Task<IUserMessage?> FetchCardAsync(QuizRound round)
    {
        if (round.MessageId == 0) return null;
        if (_client.GetGuild(round.GuildId)?.GetTextChannel(round.ChannelId) is not { } channel) return null;

        try
        {
            return await channel.GetMessageAsync(round.MessageId) as IUserMessage;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch the card of quiz round {RoundId}.", round.Id);
            return null;
        }
    }

    // A card that can't be edited is not worth losing the announcement over.
    private async Task EditCardAsync(IUserMessage card, QuizRound round, QuizQuestion question)
    {
        try
        {
            await card.ModifyAsync(m =>
            {
                m.Components = QuizCards.BuildRound(round, question, intro: null);
                // Re-asserted on every edit: an update without it is rejected on a V2 message.
                m.Flags = MessageFlags.ComponentsV2;
                m.AllowedMentions = AllowedMentions.None;
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to close the card of quiz round {RoundId}.", round.Id);
        }
    }
}
