using System.Globalization;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

internal sealed partial class ChatterService
{
    // « c'est la faute de qui ? »: /blame's draw (Helpers/Personality/Blame) among the humans in the
    // asker's voice call, and its pools. A roll on Papa can't be edited away here as the command
    // does, so it goes out as one of the dodge lines that strike his name through. Guild only.
    private bool TryBlame(SocketUserMessage message, out IntentPick pick)
    {
        pick = default;
        if (message.Author is not SocketGuildUser asker) return false;

        var candidates = asker.VoiceChannel?.ConnectedUsers.Where(u => !u.IsBot).Select(u => u.Id).ToList() ?? [];
        var result = Blame.Pick(candidates, asker.Id, OwnerId, Random.Shared);
        var pool = result.Kind switch
        {
            BlameKind.Self => BotResponses.BlameSelfLines,
            BlameKind.Quokka => BotResponses.BlameQuokkaLines,
            BlameKind.PapaDodge => StruckPapaDodgeLines,
            BlameKind.PapaDodgeQuokka => StruckPapaDodgeQuokkaLines,
            BlameKind.Nobody => BotResponses.BlameNobodyLines,
            _ => BotResponses.BlameLines,
        };
        // The /blame pools take the mention as {0}; here {0} is the name, so it goes in as {2}.
        pick = new IntentPick(BlameChatPool(pool), $"<@{result.TargetId}>", string.Empty, QuotedMentions,
            AnswerKinds.Intent(ChatIntent.Blame));
        return true;
    }

    // The dodge lines that carry the slip themselves (« ~~C'est la faute de Papa.~~ … »): the others
    // speak of a message before, which in chat there isn't.
    private static readonly string[] StruckPapaDodgeLines = BotResponses.BlamePapaDodgeLines.Where(l => l.StartsWith("~~")).ToArray();
    private static readonly string[] StruckPapaDodgeQuokkaLines =
        BotResponses.BlamePapaDodgeQuokkaLines.Where(l => l.StartsWith("~~")).ToArray();

    // Each /blame pool with its {0} moved to {2}, computed once per pool.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string[], string[]> _blameChatPools = new();
    private static string[] BlameChatPool(string[] pool) =>
        _blameChatPools.GetOrAdd(pool, p => p.Select(l => l.Replace("{0}", "{2}")).ToArray());

    // Someone telling her about their day (« j'ai mangé de la soupe »): interested half the time
    // (IntentLines), « euh, ok ? » the other half (SharingShrugLines). Papa always gets interest.
    private static IntentPick SharingPick(bool isOwner)
    {
        var kind = AnswerKinds.Intent(ChatIntent.Sharing);
        if (isOwner) return IntentPick.Of(BotResponses.OwnerIntentLines[ChatIntent.Sharing], kind);
        return IntentPick.Of(Random.Shared.NextDouble() < SharingInterestChance
            ? BotResponses.IntentLines[ChatIntent.Sharing]
            : BotResponses.SharingShrugLines, kind);
    }

    // « T'en penses quoi de X ? »: /rate's verdict on X, so chat and the command agree. « Tu préfères
    // X ou Y ? »: the side /rate ranks higher (Opinion.Prefers). The subject is the person's own words.
    private bool TryOpinion(SocketUserMessage message, out IntentPick pick)
    {
        pick = default;
        if (Opinion.Read(message.Content ?? string.Empty) is not { } ask) return false;

        var (asker, self) = (message.Author.Id, _client.CurrentUser.Id);
        RatingResult Rate(string subject) =>
            Rating.Rate(subject, asker, self, OwnerId, BotResponses.TataId);

        if (ask.Other is { } other)
        {
            var aWins = Opinion.Prefers(Rate(ask.Subject), Rate(other), Rating.Key(ask.Subject, asker), Rating.Key(other, asker));
            var (chosen, rejected) = aWins ? (ask.Subject, other) : (other, ask.Subject);
            pick = new IntentPick(BotResponses.PreferLines, chosen, rejected, QuotedMentions, AnswerKinds.Intent(ChatIntent.Opinion));
            return true;
        }

        pick = new IntentPick(BotResponses.OpinionLines, ask.Subject, RateModule.Verdict(Rate(ask.Subject), _picker), QuotedMentions,
            AnswerKinds.Intent(ChatIntent.Opinion));
        return true;
    }

    // « ship X et Y », « X et moi ça match ? »: /ship's whole answer (ShipModule.Render), so chat and
    // the command agree. « on irait bien ensemble ? » ships the asker with whoever « on » is; « c'est
    // qui mon âme sœur ? » with their best match on the server (ShipMatch). A side asking for someone
    // at random is drawn from the server, as /ship does. In a DM, or with nobody to draw or to match,
    // there is no ship and the message goes on to the other pools.
    private bool TryShip(SocketUserMessage message, out IntentPick pick)
    {
        pick = default;
        if (ShipAsk.Read(message.Content ?? string.Empty) is not { } ask) return false;

        var guild = (message.Channel as SocketGuildChannel)?.Guild;
        var (asker, self) = (message.Author.Id, _client.CurrentUser.Id);
        var (a, b, pool) = (ask.A, ask.B, BotResponses.ShipChatLines);

        switch (ask.Kind)
        {
            case ShipAskKind.Us:
                b = UsPartner(message, self);
                break;
            case ShipAskKind.BestMatch or ShipAskKind.WorstMatch:
                if (guild is null || ShipMatch(guild, asker, self, best: ask.Kind == ShipAskKind.BestMatch) is not { } match)
                    return false;
                (b, pool) = ($"<@{match}>",
                    ask.Kind == ShipAskKind.BestMatch ? BotResponses.ShipBestMatchLines : BotResponses.ShipWorstMatchLines);
                break;
        }

        if (Shipping.IsRandom(a) || Shipping.IsRandom(b))
        {
            if (guild is null
                || !ShipModule.TryDrawRandom(ref a, b, asker, guild.Users)
                || !ShipModule.TryDrawRandom(ref b, a, asker, guild.Users))
                return false;
        }

        // After the draws, with the sides as shipped: « on irait bien ensemble » with her is the asker
        // and her.
        // The badge rides on the pick and is earned when the answer goes out (SendPickAsync).
        var heart = Shipping.IsSelfWithHer(a, b, asker, self, OwnerId, BotResponses.TataId) ? MemberBadges.Keys.Heart : null;

        var answer = ShipModule.Render(a, b, asker, self, _picker, side => ShipModule.NameOf(side, guild, _client));
        pick = new IntentPick(pool, answer, string.Empty, QuotedMentions, AnswerKinds.Intent(ChatIntent.Ship), heart);
        return true;
    }

    // Who « on » is in « on irait bien ensemble ? »: the person tagged (not her), else the person
    // replied to (not her, not a bot, not the asker), else her — « toi », as /ship reads it.
    private static string UsPartner(SocketUserMessage message, ulong selfId)
    {
        if (message.MentionedUsers.FirstOrDefault(u => u.Id != selfId && u.Id != message.Author.Id && !u.IsBot) is { } tagged)
            return $"<@{tagged.Id}>";
        if (message.ReferencedMessage?.Author is { IsBot: false } replied && replied.Id != message.Author.Id)
            return $"<@{replied.Id}>";
        return "toi";
    }

    // The member whose /ship score with the asker is the highest (or the lowest): every human on the
    // server but the asker, through Shipping.Ship itself, so the answer is the one /ship gives for
    // that pair. Ties go to the lowest id, so the same server gives the same answer. Null when nobody
    // else is there.
    private static ulong? ShipMatch(SocketGuild guild, ulong askerId, ulong selfId, bool best)
    {
        var scored = guild.Users
            .Where(u => !u.IsBot && u.Id != askerId)
            .Select(u => (u.Id, Shipping.Ship("moi", $"<@{u.Id}>", askerId, selfId, OwnerId, BotResponses.TataId).Percent))
            .OrderBy(s => best ? -s.Percent : s.Percent)
            .ThenBy(s => s.Id)
            .ToList();
        return scored.Count == 0 ? null : scored[0].Id;
    }

    // Which of her own posts a reply answers: one she remembered sending (HerMessages), the
    // morning hello by its text, one of her cards by its buttons, any other slash-command result by
    // its interaction metadata.
    private ReplyContext? ReadReplyContext(SocketUserMessage message)
    {
        if (message.ReferencedMessage is not { } replied) return null;
        if (_herMessages.TryGet(replied.Id, out var kind)) return kind;
        if (MorningGreetingService.IsMorningHello(replied)) return ReplyContext.MorningHello;
        if (CardRefs.Read(CustomIds(replied)) is { } card)
        {
            return card.Kind switch
            {
                CardKind.Poll => ReplyContext.Poll,
                CardKind.Session => ReplyContext.Session,
                CardKind.Tonight => ReplyContext.Tonight,
                _ => ReplyContext.Giveaway,
            };
        }
        if (replied.InteractionMetadata is not null) return ReplyContext.CommandResult;
        return null;
    }

    // A reply under one of her cards that is about the card: rallying the others, « j'ai voté », a
    // question about it, an answer typed instead of clicked, « remonte-le » (CardReplyService). Read
    // before the breakdown roll and the streak: someone urging the room under her poll is not talking
    // to her. A mean reply is roasted as before; a reply about something else goes on down the path,
    // where the card's context line waits for what nothing else caught.
    private bool TryCardReply(SocketUserMessage message, string name)
    {
        if (message.ReferencedMessage is not { } replied || CardRefs.Read(CustomIds(replied)) is not { } card) return false;

        var content = message.Content ?? string.Empty;
        if (IsMean(MessageCues.Analyze(content), content)) return false;

        var selfId = _client.CurrentUser.Id;
        var tagsSomeone = message.MentionedUsers.Any(u => !u.IsBot && u.Id != message.Author.Id && u.Id != selfId);
        return _cards.TryAnswer(message, card, MessageCues.ReadCardReply(content, tagsSomeone), name, CurrentWeekday());
    }

    // Every custom-id on a message's buttons and selects, rows opened.
    private static IEnumerable<string?> CustomIds(IMessage message) =>
        message.Components
            .SelectMany(c => c is ActionRowComponent row ? row.Components : [c])
            .Select(c => c switch
            {
                ButtonComponent button => button.CustomId,
                SelectMenuComponent select => select.CustomId,
                _ => null,
            });

    // Counts this message in the person's back-and-forth with her (ConversationStreak). Returns
    // Handled when the gate answered it itself: a reaction while the conversation is ended, or the
    // line that ends it. Papa is never counted: he is never told to stop.
    private async Task<(bool Handled, int Count)> StreakGateAsync(
        SocketUserMessage message, ChatPath path, string name, string weekday)
    {
        if (message.Author.Id == OwnerId) return (false, 0);

        var now = DateTimeOffset.UtcNow;
        var (channelId, userId) = (message.Channel.Id, message.Author.Id);
        if (_streaks.IsClosed(channelId, userId, now))
        {
            Count(message, path, AnswerKinds.StreakSulk);
            await SulkReactAsync(message);
            return (true, 0);
        }

        var count = _streaks.Record(channelId, userId, now);
        if (count < ConversationStreak.EndAt) return (false, count);

        _streaks.Close(channelId, userId, now);
        _logger.LogInformation("{Name} replied {Count} times in a row — she ends the conversation.", name, count);
        var line = string.Format(_picker.Pick(BotResponses.StreakEndLines), name, weekday, count);
        Count(message, path, AnswerKinds.StreakEnd);
        await ReplyWithTypingAsync(message, line, "streak end");
        return (true, count);
    }

    // The ended conversation's only answer: a reaction, no words. Swallowed on failure.
    private async Task SulkReactAsync(SocketUserMessage message)
    {
        if (EmoteMarkup.Parse(_picker.Pick(BotResponses.StreakSulkReactions)) is not { } emote) return;
        try
        {
            await message.AddReactionAsync(emote);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add a sulk reaction in channel {ChannelId}.", message.Channel.Id);
        }
    }

    // The last resort of both paths (a roast, or « tagged for nothing »): on the streak's third
    // message she remarks on it instead; otherwise, now and then, one of her records replaces it.
    // The records are read off the gateway handler — the fan-out awaits every handler in turn, and
    // RecapService is several queries on the Pi — and the claim is given back if nothing applies.
    private async Task SendFallbackAsync(
        SocketUserMessage message, ChatPath path, string kind, int streak, string[] pool, string name, string weekday,
        string what)
    {
        if (streak == ConversationStreak.NudgeAt)
        {
            var nudge = string.Format(_picker.Pick(BotResponses.StreakNudgeLines), name, weekday, streak);
            Count(message, path, AnswerKinds.StreakNudge);
            await ReplyWithTypingAsync(message, nudge, "streak nudge");
            return;
        }

        var line = string.Format(_picker.Pick(pool), name, weekday);
        if (message.Channel is SocketGuildChannel guildChannel
            && Random.Shared.NextDouble() < FactChance
            && _factGate.TryClaim(message.Author.Id))
        {
            var guildId = guildChannel.Guild.Id;
            _ = Task.Run(() => ReplyWithFactAsync(message, path, kind, guildId, name, weekday, line, what));
            return;
        }

        Count(message, path, kind);
        await ReplyWithTypingAsync(message, line, what);
    }

    // One of her records about this person, or the line it was going to replace (counted as that
    // line's kind). Sent with mentions off: a fact can quote a Plynling name, which is hostile
    // input (root CLAUDE.md).
    private async Task ReplyWithFactAsync(
        SocketUserMessage message, ChatPath path, string kind, ulong guildId, string name, string weekday,
        string fallback, string what)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var stats = await scope.ServiceProvider.GetRequiredService<RecapService>()
                .GetAsync(guildId, message.Author.Id, StatsPeriod.AllTime);

            if (ChatFacts.Pick(stats, Random.Shared) is { } fact
                && BotResponses.ChatFactLines.TryGetValue(fact.Kind, out var pool))
            {
                _logger.LogInformation("{Name} gets one of her records — {Fact}.", name, fact.Kind);
                var line = string.Format(_picker.Pick(pool), name, weekday, fact.A, fact.B);
                Count(message, path, AnswerKinds.Fact(fact.Kind));
                await ReplyWithTypingAsync(message, line, $"fact line ({fact.Kind})", AllowedMentions.None);
                return;
            }

            _factGate.Release(message.Author.Id);
        }
        catch (Exception ex)
        {
            _factGate.Release(message.Author.Id);
            _logger.LogWarning(ex, "Could not read the records of {UserId} for a fact line.", message.Author.Id);
        }

        Count(message, path, kind);
        await ReplyWithTypingAsync(message, fallback, what);
    }

    // Whether the message replied to asked something: a bare « oui » only answers a question.
    private static bool AskedSomething(IMessage? replied) => replied?.Content?.Contains('?') == true;

    // « tu m'aimes ? » and « tu te souviens de moi ? » are answered from her records, in a guild and
    // never for the family, who have their own pools: the good and bad bots they gave her in the last
    // 30 days decide whether she loves them, and one fact she keeps proves she remembers them. Read off the gateway
    // handler, like the fact lines; true when it took the answer over. `plain` goes out when nothing
    // applies or the read fails.
    // « c'est quand la prochaine session ? » and « je suis quel niveau ? » are read the same way, for
    // everyone, family included: a session time or a level is the same whoever asks.
    private bool TryAnswerFromRecords(
        ChatIntent intent, SocketUserMessage message, ChatPath path, IntentPick plain, string name, string weekday)
    {
        if (intent is not (ChatIntent.DoYouLoveMe or ChatIntent.RememberMe or ChatIntent.NextSession or ChatIntent.MyStats))
            return false;
        if (intent is ChatIntent.DoYouLoveMe or ChatIntent.RememberMe && message.Author.Id is OwnerId or BotResponses.TataId)
            return false;
        if (message.Channel is not SocketGuildChannel guildChannel) return false;

        var guildId = guildChannel.Guild.Id;
        _ = Task.Run(() => ReplyFromRecordsAsync(intent, message, path, guildId, plain, name, weekday));
        return true;
    }

    private async Task ReplyFromRecordsAsync(
        ChatIntent intent, SocketUserMessage message, ChatPath path, ulong guildId, IntentPick plain, string name,
        string weekday)
    {
        var what = $"{AnswerKinds.Key(path)} intent ({intent})";
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var sp = scope.ServiceProvider;
            var line = intent switch
            {
                ChatIntent.DoYouLoveMe => LovesMeLine(await sp.GetRequiredService<BotFeedbackService>()
                    .GetTallyAsync(guildId, message.Author.Id, StatsPeriod.Month), name, weekday),
                ChatIntent.NextSession => NextSessionLine(await sp.GetRequiredService<EventService>()
                    .GetUpcomingOrRunningAsync(guildId), guildId, name, weekday),
                ChatIntent.MyStats => MyStatsLine(await sp.GetRequiredService<RecapService>()
                    .GetAsync(guildId, message.Author.Id, StatsPeriod.AllTime), message.Content ?? string.Empty, name, weekday),
                _ => RemembersLine(await sp.GetRequiredService<RecapService>()
                    .GetAsync(guildId, message.Author.Id, StatsPeriod.AllTime), name, weekday),
            };

            if (line is not null)
            {
                Count(message, path, AnswerKinds.Intent(intent));
                await ReplyWithTypingAsync(message, line, $"{what}, from her records", AllowedMentions.None);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read the records of {UserId} for a {Intent} answer.", message.Author.Id, intent);
        }

        Count(message, path, plain.Kind);
        await ReplyWithTypingAsync(message, Format(plain, name, weekday), what, plain.Mentions);
    }

    // Whether she loves them is the last 30 days only, so a grudge can be worked off: more bad bots
    // than good ones is the grudge (quoted, with its window), more good ones the treasure (never
    // counted), a tie or nothing is null.
    private string? LovesMeLine(FeedbackTally tally, string name, string weekday)
    {
        if (tally.Bad > tally.Good)
            return string.Format(_picker.Pick(BotResponses.DoYouLoveMeGrudgeLines), name, weekday, $"{tally.Bad} bad bot en 30 jours");
        if (tally.Good > tally.Bad)
            return string.Format(_picker.Pick(BotResponses.DoYouLoveMeTreasureLines), name, weekday);
        return null;
    }

    // The next session (or the one running), with who is coming; none planned is a line too. Titles
    // and names are hostile input, sanitised, and the whole reply goes out with mentions off.
    private string NextSessionLine(List<SessionEvent> sessions, ulong guildId, string name, string weekday)
    {
        if (sessions.Count == 0) return string.Format(_picker.Pick(BotResponses.NoSessionLines), name, weekday);

        var next = sessions[0];
        var title = $"« {Discord.Format.Sanitize(next.Title)} »";
        var unix = next.ScheduledAt.ToUnixTimeSeconds();
        if (next.PhaseAt(DateTimeOffset.UtcNow) == SessionPhase.InProgress)
            return string.Format(_picker.Pick(BotResponses.SessionRunningLines), name, weekday, title, $"<t:{unix}:R>");

        var guild = _client.GetGuild(guildId);
        var joined = next.Participants
            .Where(p => p.Status == ParticipantStatus.Joined)
            .OrderBy(p => p.JoinedAt)
            .Select(p => Discord.Format.Sanitize(guild?.GetUser(p.UserId) is { } user ? ResolveName(user) : p.Username))
            .ToList();
        var maybes = next.Participants.Count(p => p.Status == ParticipantStatus.Maybe);
        return string.Format(_picker.Pick(BotResponses.SessionNextLines), name, weekday,
            $"{title}, <t:{unix}:F> (<t:{unix}:R>)", SessionRoster.Describe(joined, maybes));
    }

    // Their level, cailloux or Plynling, all-time (/recap's numbers), by what they asked. Nothing to
    // show on that topic (no XP yet, no living Plynling) has its own pool.
    private string MyStatsLine(RecapStats stats, string content, string name, string weekday)
    {
        var topic = MessageCues.ReadMyStatsTopic(content) ?? MyStatsTopic.All;
        if (MyStats.Describe(stats, topic) is { } said)
            return string.Format(_picker.Pick(BotResponses.MyStatsLines[topic]), name, weekday, said.A, said.B);
        var empty = topic == MyStatsTopic.Plynling ? BotResponses.MyPlynlingEmptyLines : BotResponses.MyLevelEmptyLines;
        return string.Format(_picker.Pick(empty), name, weekday);
    }

    // A fact about them, all-time, proves she remembers: its line after an opener that says their
    // name (the answer to « comment je m'appelle ? »). Null when no fact applies.
    private string? RemembersLine(RecapStats stats, string name, string weekday)
    {
        if (ChatFacts.Pick(stats, Random.Shared) is not { } fact
            || !BotResponses.ChatFactLines.TryGetValue(fact.Kind, out var pool))
            return null;
        var opener = string.Format(_picker.Pick(BotResponses.RememberMeOpeners), name);
        return opener + " " + string.Format(_picker.Pick(pool), name, weekday, fact.A, fact.B);
    }
}
