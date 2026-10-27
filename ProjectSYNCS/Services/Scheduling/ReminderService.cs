using Discord;
using Discord.Net;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Poll = ProjectSYNCS.Models.Poll;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

public class ReminderService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly SilenceService _silence;
    private readonly ILogger<ReminderService> _logger;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    public ReminderService(
        IServiceProvider services,
        DiscordSocketClient client,
        ResponsePicker picker,
        SilenceService silence,
        ILogger<ReminderService> logger)
    {
        _silence = silence;
        _services = services;
        _client = client;
        _picker = picker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderService started. Checking every {Interval} minutes.",
            CheckInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);
            await ProcessRemindersAsync();
            await ProcessMaybeNudgesAsync();
            await ProcessLifecycleAsync();
            await ProcessPollAutoCloseAsync();
            await ProcessTonightExpiryAsync();
            await ProcessRecurringAsync();
        }
    }

    // Weekly series: each occurrence is posted as an ordinary session, LeadDays before it, in the
    // series' channel. A command set it up, so silence mode doesn't hold it back. The session and
    // the series' LastOccurrence land in one save (EventService.CreateOccurrenceAsync), and a unique
    // index on (series, slot) backs it up: a restart mid-pass can't post one twice. A card that fails
    // to post is undone and retried next pass; a channel that is gone stops the series.
    private async Task ProcessRecurringAsync()
    {
        if (_client.ConnectionState != ConnectionState.Connected) return;

        await using var scope = _services.CreateAsyncScope();
        var events = scope.ServiceProvider.GetRequiredService<EventService>();

        List<RecurringSession> series;
        try
        {
            series = await events.GetActiveSeriesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query weekly series.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var s in series)
        {
            try
            {
                var next = Recurrence.NextOccurrence(s, now);
                if (!Recurrence.DueToPost(next, now, s.LeadDays)) continue;

                // Not in the cache yet (a guild still loading): try again next pass.
                if (_client.GetGuild(s.GuildId) is not { } guild) continue;
                if (guild.GetTextChannel(s.ChannelId) is not { } channel)
                {
                    await events.StopSeriesAsync(s.Id);
                    _logger.LogWarning("Weekly series {SeriesId} stopped: its channel {ChannelId} is gone.", s.Id, s.ChannelId);
                    continue;
                }

                var previous = s.LastOccurrence;
                if (await events.CreateOccurrenceAsync(s.Id, next) is not { } session) continue;
                try
                {
                    var message = await SessionCards.SendAsync(channel, session, guild);
                    await events.SetMessageIdAsync(session.Id, message.Id);
                    _logger.LogInformation("Weekly series {SeriesId} posted session {EventId}.", s.Id, session.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Weekly series {SeriesId}: card not posted; retrying next pass.", s.Id);
                    await events.UndoOccurrenceAsync(s.Id, session.Id, previous);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process weekly series {SeriesId}.", s.Id);
            }
        }
    }

    // /tonight calls whose evening is over close at midnight Paris. Five minutes late is fine for
    // that, so it rides this loop. The close is claimed before the card is redrawn: a failed edit
    // leaves a closed call whose buttons say so when clicked.
    private async Task ProcessTonightExpiryAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var tonight = scope.ServiceProvider.GetRequiredService<TonightService>();

        List<TonightCall> expired;
        try
        {
            expired = await tonight.GetExpiredOpenAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query expired tonight calls.");
            return;
        }

        foreach (var call in expired)
        {
            try
            {
                if (!await tonight.ClaimCloseAsync(call.Id)) continue;
                call.Closed = true;
                if (call.MessageId == 0) continue;
                var channel = _client.GetGuild(call.GuildId)?.GetTextChannel(call.ChannelId);
                if (channel is null || await channel.GetMessageAsync(call.MessageId) is not IUserMessage message) continue;
                await message.ModifyAsync(p =>
                {
                    p.Embed = TonightModule.BuildEmbed(call);
                    p.Components = TonightModule.BuildComponents(call);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to close tonight call {CallId}.", call.Id);
            }
        }
    }

    // Polls and votes close on their own at their deadline (Poll.ClosesAt: EndsAt, or the old
    // two-day lifetime on rows from before deadlines existed).
    private async Task ProcessPollAutoCloseAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var pollService = scope.ServiceProvider.GetRequiredService<PollService>();
        var cards = scope.ServiceProvider.GetRequiredService<CardUpdater>();

        List<Poll> polls;
        try
        {
            polls = await pollService.GetPollsToAutoCloseAsync(DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query polls for auto-close.");
            return;
        }

        foreach (var poll in polls)
        {
            // Guarded per item for the reason above. The close is written before the card
            // is re-rendered, so a failure here leaves the poll open and it is simply
            // retried on the next pass — which is why this may swallow and continue.
            try
            {
                await pollService.ClosePollAsync(poll.Id);
                poll.IsClosed = true;
                if (_client.GetGuild(poll.GuildId) is { } guild) await cards.RedrawPollAsync(guild, poll);
                // Her own weekend poll (WeekendPollService) has its own lines; everyone else's
                // result goes to its organiser.
                if (poll.OrganizerId == _client.CurrentUser.Id)
                    await AnnounceOwnPollAsync(poll);
                else
                    await AnnounceResultAsync(poll);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-close poll {PollId}.", poll.Id);
            }
        }
    }

    // Someone's poll or vote reached its deadline: she says what won, as a reply to the card,
    // pinging its organiser and nobody else. A command set it up, so silence mode doesn't hold
    // it back. Swallows its own failures: the close is already written.
    private async Task AnnounceResultAsync(Poll poll)
    {
        if (poll.MessageId == 0) return;

        try
        {
            if (_client.GetGuild(poll.GuildId)?.GetTextChannel(poll.ChannelId) is not { } channel) return;

            var title = Format.Sanitize(poll.Title);
            var organizer = $"<@{poll.OrganizerId}>";
            var winners = PollRules.Leaders(poll);
            // A date poll's slots already past can't become a session: only the future ones count.
            if (poll.Kind == PollKind.DateSlots)
            {
                var now = DateTimeOffset.UtcNow;
                winners = winners.Where(o => o.ScheduledAt > now).ToList();
            }

            string line;
            if (winners.Count == 0)
                line = string.Format(_picker.Pick(BotResponses.PollClosedEmptyLines), organizer, title);
            else
            {
                var named = winners.Select(o => PollCards.OptionText(poll, o)).ToList();
                var result = named.Count == 1 ? named[0] : $"égalité entre {string.Join(" et ", named)}";
                var pool = poll.Kind == PollKind.DateSlots ? BotResponses.PollClosedSlotLines : BotResponses.VoteClosedResultLines;
                line = string.Format(_picker.Pick(pool), organizer, title, result);
            }

            var reference = new MessageReference(poll.MessageId, poll.ChannelId, poll.GuildId, failIfNotExists: false);
            await BotChat.PostWithTypingAsync(channel, line, _logger, "poll result",
                new AllowedMentions { UserIds = new List<ulong> { poll.OrganizerId } }, reference);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to announce the result of poll {PollId}.", poll.Id);
        }
    }

    // Her weekend poll just closed: she says which evening won, as a reply to the card (whose
    // « Créer une session » button anyone who voted for that evening may use), or sulks that
    // nobody voted. Only future slots count, like the button. Swallows its own failures: the
    // close is already written, and a missing line is the worst case.
    private async Task AnnounceOwnPollAsync(Poll poll)
    {
        if (poll.MessageId == 0) return;

        try
        {
            if (_client.GetGuild(poll.GuildId)?.GetTextChannel(poll.ChannelId) is not { } channel) return;

            var now = DateTimeOffset.UtcNow;
            var future = poll.Options.Where(o => o.ScheduledAt > now).ToList();
            int max = future.Count == 0 ? 0 : future.Max(o => o.Votes.Count);

            string line;
            if (max == 0)
            {
                line = _picker.Pick(BotResponses.WeekendPollDeadLines);
            }
            else
            {
                var slots = string.Join(" ou ", future
                    .Where(o => o.Votes.Count == max)
                    .OrderBy(o => o.ScheduledAt)
                    .Select(o => $"<t:{o.ScheduledAt.ToUnixTimeSeconds()}:F>"));
                line = string.Format(_picker.Pick(BotResponses.WeekendPollResultLines), slots);
            }

            var reference = new MessageReference(poll.MessageId, poll.ChannelId, poll.GuildId, failIfNotExists: false);
            await BotChat.PostWithTypingAsync(channel, line, _logger, "weekend poll result", AllowedMentions.None, reference);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to announce the result of poll {PollId}.", poll.Id);
        }
    }

    // Advances session cards through their lifecycle (En cours -> Terminée) and
    // disables their buttons once they start.
    private async Task ProcessLifecycleAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var eventService = scope.ServiceProvider.GetRequiredService<EventService>();
        var cards = scope.ServiceProvider.GetRequiredService<CardUpdater>();

        List<SessionEvent> events;
        try
        {
            events = await eventService.GetEventsNeedingLifecycleUpdateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query events for lifecycle updates.");
            return;
        }

        foreach (var sessionEvent in events)
        {
            // Guarded per item for the reason above. SetRenderedPhaseAsync is the write
            // that matters here: CardUpdater swallows its own Discord failures, so a
            // throw reaching this far is a database problem, and losing the host over one
            // card is worse than re-rendering it on the next pass.
            try
            {
                var phase = sessionEvent.PhaseAt(DateTimeOffset.UtcNow);
                if (_client.GetGuild(sessionEvent.GuildId) is { } guild)
                    await cards.RedrawSessionAsync(guild, sessionEvent);
                await eventService.SetRenderedPhaseAsync(sessionEvent.Id, phase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to advance lifecycle for event {EventId}.", sessionEvent.Id);
            }
        }
    }

    private async Task ProcessRemindersAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var eventService = scope.ServiceProvider.GetRequiredService<EventService>();

        List<SessionEvent> events;
        try
        {
            events = await eventService.GetEventsNeedingReminderAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query events for reminders.");
            return;
        }

        foreach (var sessionEvent in events)
        {
            // Per item, not one try around the pass: an exception escaping ExecuteAsync
            // stops the whole host (BackgroundServiceExceptionBehavior defaults to
            // StopHost), and one unreachable session must not cost the others their
            // reminder. Same shape as GiveawayDrawService.SweepAsync.
            try
            {
                // Claimed before the DMs (root CLAUDE.md, reliability): a crash mid-send costs
                // a reminder, never a second round of them.
                await eventService.MarkReminderSentAsync(sessionEvent.Id);
                await SendRemindersForEventAsync(sessionEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process reminders for event {EventId}.", sessionEvent.Id);
            }
        }
    }

    // About an hour before a session with room left, a DM to whoever is still on « Peut-être »: pick
    // a side. Nobody asked for it, so silence mode holds it back (the flag stays unset, and the window
    // closes on its own). The flag is set before the DMs: a crash costs the nudge, never a second one.
    private async Task ProcessMaybeNudgesAsync()
    {
        if (_silence.IsSilent) return;

        await using var scope = _services.CreateAsyncScope();
        var eventService = scope.ServiceProvider.GetRequiredService<EventService>();

        List<SessionEvent> events;
        try
        {
            events = await eventService.GetEventsNeedingMaybeNudgeAsync(DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query sessions for « peut-être » nudges.");
            return;
        }

        foreach (var sessionEvent in events)
        {
            try
            {
                await eventService.MarkMaybeNudgeSentAsync(sessionEvent.Id);
                await NudgeMaybesAsync(sessionEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to nudge the « peut-être » of event {EventId}.", sessionEvent.Id);
            }
        }
    }

    private async Task NudgeMaybesAsync(SessionEvent sessionEvent)
    {
        var maybes = sessionEvent.Participants.Where(p => p.Status == ParticipantStatus.Maybe).ToList();
        var ts = sessionEvent.ScheduledAt.ToUnixTimeSeconds();
        var line = string.Format(_picker.Pick(BotResponses.SessionMaybeNudgeLines),
            Format.Sanitize(sessionEvent.Title), $"<t:{ts}:R>", SessionCards.Link(sessionEvent));

        foreach (var participant in maybes)
        {
            try
            {
                var user = await _client.GetUserAsync(participant.UserId);
                if (user is null) continue;
                var dm = await user.CreateDMChannelAsync();
                await dm.SendMessageAsync(line, allowedMentions: AllowedMentions.None);
            }
            catch (HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
            {
                _logger.LogInformation("User {UserId} has DMs disabled; no « peut-être » nudge.", participant.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "« Peut-être » nudge for event {EventId} not sent to {UserId}.", sessionEvent.Id, participant.UserId);
            }
        }
        _logger.LogInformation("Nudged {Count} « peut-être » of event {EventId}.", maybes.Count, sessionEvent.Id);
    }

    // The 30-minute DM, to everyone Joined and to the organiser even if they never clicked
    // « Rejoindre » (it's their session). Each DM links to the card. A reminder missed while she
    // was down goes out late (the window reaches down to five minutes), which the line handles
    // through <t:R>.
    private async Task SendRemindersForEventAsync(SessionEvent sessionEvent)
    {
        var recipients = sessionEvent.Participants
            .Where(p => p.Status == ParticipantStatus.Joined)
            .Select(p => p.UserId)
            .ToList();
        if (sessionEvent.OrganizerId != _client.CurrentUser.Id && !recipients.Contains(sessionEvent.OrganizerId))
            recipients.Add(sessionEvent.OrganizerId);

        _logger.LogInformation(
            "Sending reminders for event {EventId} ({Title}) to {Count} people.",
            sessionEvent.Id, sessionEvent.Title, recipients.Count);

        var ts = sessionEvent.ScheduledAt.ToUnixTimeSeconds();
        var link = sessionEvent.MessageId != 0 ? $"\n{SessionCards.Link(sessionEvent)}" : string.Empty;
        var where = sessionEvent.VoiceChannelId != 0 ? $" dans <#{sessionEvent.VoiceChannelId}>" : string.Empty;

        foreach (var userId in recipients)
        {
            try
            {
                var user = await _client.GetUserAsync(userId);
                if (user is null)
                {
                    _logger.LogWarning("Could not resolve user {UserId} for reminder.", userId);
                    continue;
                }

                var dm = await user.CreateDMChannelAsync();
                await dm.SendMessageAsync(
                    $"Kilou kilou {user.Mention}! {Emotes.HiCat}{Emotes.HiCat}{Emotes.HiCat} (˶>⩊<˶)\n" +
                    $"Rappel: {SessionCategories.Emoji(sessionEvent.Category)} **{Format.Sanitize(sessionEvent.Title)}** va commencer " +
                    $"<t:{ts}:R> (<t:{ts}:t>){where}. Tiens toi prêt ! UwU{link}",
                    allowedMentions: AllowedMentions.None);
            }
            catch (HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
            {
                _logger.LogWarning("User {UserId} has DMs disabled; skipping reminder.", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reminder to user {UserId}.", userId);
            }
        }
    }
}
