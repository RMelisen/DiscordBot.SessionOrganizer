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
    // ---- Listening: intents, reply context, streaks, facts --------------------------------

    /// <summary>
    /// A pool, and what its lines' {2} and {3} stand for: her temperature, an opinion's subject and
    /// verdict, a request's command. <see cref="Mentions"/> is set when {2}/{3} quote the person's
    /// own words (an opinion's subject), which may hold a mention: shown, never pinged.
    /// <see cref="Kind"/> is the AnswerKinds key `/debug answers` counts it by.
    /// </summary>
    private readonly record struct IntentPick(string[] Pool, string A, string B, AllowedMentions? Mentions, string Kind, string? Badge = null)
    {
        public static IntentPick Of(string[] pool, string kind) => new(pool, string.Empty, string.Empty, null, kind);
    }

    // Counts an answer for `/debug answers`, in memory (ActivityCounter, written every few minutes):
    // never a database write per answer. A message she found nothing specific to say to is also kept,
    // without its author (UnansweredLog: 30 rolling days).
    private void Count(SocketUserMessage message, ChatPath path, string kind)
    {
        var guildId = (message.Channel as SocketGuildChannel)?.Guild.Id ?? 0;
        var unanswered = AnswerKinds.IsUnrecognised(kind) ? UnansweredLog.Clean(message.Content ?? string.Empty) : null;
        _activity.CountAnswer(guildId, AnswerKinds.Key(path), kind, unanswered, DateTimeOffset.UtcNow);
    }

    // Papa's pet name for her, as its family's pool; null for none. The callers check it is Papa.
    private static IntentPick? PetNamePick(PetName petName) => petName switch
    {
        PetName.Raspberry => IntentPick.Of(BotResponses.OwnerRaspberryNameLines, AnswerKinds.PapaRaspberry),
        PetName.Child => IntentPick.Of(BotResponses.OwnerChildNameLines, AnswerKinds.PapaChild),
        _ => null,
    };

    // Quoted words keep their look but ping nobody; the person replied to still gets the reply ping.
    private static readonly AllowedMentions QuotedMentions = new(AllowedMentionTypes.None) { MentionRepliedUser = true };

    // The template is picked first, formatted second: {0} the name, {1} the weekday, {2}/{3} the pick's.
    private string Format(IntentPick pick, string name, string weekday) =>
        string.Format(_picker.Pick(pick.Pool), name, weekday, pick.A, pick.B);

    // Sends a pick's answer; the badge a pick carries (a ship with her) is earned only once the
    // answer is actually out, never when the pick was merely built and a later branch cut it off.
    private async Task SendPickAsync(SocketUserMessage message, IntentPick pick, string name, string weekday, string what)
    {
        if (await ReplyWithTypingAsync(message, Format(pick, name, weekday), what, pick.Mentions) is not null && pick.Badge is { } badge)
            Award(message, badge);
    }

    /// <summary>
    /// The answer to a specific intent (not a generic question or a low-effort word), or false.
    /// Papa only has twins for some intents; for the others this is false and he goes on to his
    /// own pools — except an opinion, a ship, a yes-or-no, a blame, a joke and a fun fact, the same
    /// whoever asks, and help and requests, which he gets in his devoted request pool.
    /// </summary>
    private bool TryIntent(ChatIntent intent, SocketUserMessage message, bool isOwner, out IntentPick pick)
    {
        pick = default;
        if (intent is ChatIntent.None or ChatIntent.Question or ChatIntent.Sharing or ChatIntent.LowEffort) return false;

        if (intent == ChatIntent.Opinion) return TryOpinion(message, out pick);
        if (intent == ChatIntent.Ship) return TryShip(message, out pick);
        if (intent == ChatIntent.Blame) return TryBlame(message, out pick);
        if (intent == ChatIntent.ShouldI)
        {
            pick = ShouldIPick(message);
            return true;
        }
        // The same joke and the same fact for everyone, Papa included.
        if (intent == ChatIntent.Joke)
        {
            pick = IntentPick.Of(BotResponses.IntentLines[ChatIntent.Joke], AnswerKinds.Intent(intent));
            return true;
        }
        if (intent == ChatIntent.FunFact)
        {
            pick = IntentPick.Of(BotResponses.MorningFunFacts, AnswerKinds.Intent(intent));
            return true;
        }
        if (intent == ChatIntent.Roll) return TryRoll(message, out pick);
        if (intent == ChatIntent.TakeSides) return TryTakeSides(message, out pick);
        if (intent == ChatIntent.RateMe)
        {
            // /rate's verdict on « moi », the asker: Papa and Tata get their fixed verdicts, as /rate gives them.
            var verdict = RateModule.Verdict(Rating.Rate("moi", message.Author.Id, _client.CurrentUser.Id, OwnerId,
                BotResponses.TataId), _picker);
            pick = new IntentPick(BotResponses.RateMeLines, verdict, string.Empty, null, AnswerKinds.Intent(intent));
            return true;
        }
        // Answered from the sessions and from their records (TryAnswerFromRecords), for everyone: a fact
        // is the same whoever asks. This pool is what goes out when the read can't be done.
        if (intent is ChatIntent.NextSession or ChatIntent.MyStats)
        {
            pick = IntentPick.Of(BotResponses.IntentLines[intent], AnswerKinds.Intent(intent));
            return true;
        }
        // Told to hush: the line she answers with is her last in this channel for a while. Papa too:
        // his word holds for everyone else, and he is still answered (HushGateAsync).
        if (intent == ChatIntent.ShutUp)
        {
            _hush.Hush(message.Channel.Id, DateTimeOffset.UtcNow);
            Award(message, MemberBadges.Keys.Hush);
        }

        if (intent == ChatIntent.Help)
        {
            // The command for what they asked about; Papa gets his devoted request pool around it.
            var command = BotResponses.HelpCommands[MessageCues.ReadTopic(message.Content ?? string.Empty)];
            pick = new IntentPick(isOwner ? BotResponses.OwnerRequestLines : BotResponses.HelpLines, command,
                string.Empty, null, AnswerKinds.Intent(intent));
            return true;
        }

        if (intent == ChatIntent.AboutHer)
        {
            var topic = MessageCues.ReadHerTopic(message.Content ?? string.Empty) ?? HerTopic.Favourite;
            // Her version is the one fact Papa asks for real (did the deploy go through?): he gets
            // the number too, in his own pool. The rest of AboutHer is his twin's.
            if (topic == HerTopic.Version)
            {
                pick = new IntentPick(isOwner ? BotResponses.OwnerVersionLines : BotResponses.AboutHerLines[topic],
                    AppInfo.Version, string.Empty, null, AnswerKinds.Intent(intent));
                return true;
            }
            if (!isOwner)
            {
                var days = topic == HerTopic.Age ? MorningGreeting.DaysOld(DateTimeOffset.UtcNow).ToString() : string.Empty;
                pick = new IntentPick(BotResponses.AboutHerLines[topic], days, string.Empty, null, AnswerKinds.Intent(intent));
                return true;
            }
        }

        if (intent == ChatIntent.Request)
        {
            var topic = MessageCues.ReadRequestTopic(message.Content ?? string.Empty) ?? RequestTopic.Other;
            // Papa gets one devoted pool, with the command as {2}; everyone else a pool per topic.
            var kind = AnswerKinds.Intent(intent);
            pick = isOwner
                ? new IntentPick(BotResponses.OwnerRequestLines, BotResponses.RequestCommands[topic], string.Empty, null, kind)
                : IntentPick.Of(BotResponses.RequestLines[topic], kind);
            return true;
        }

        // Family asking « tu m'aimes ? » or « tu te souviens de moi ? » is not anyone asking.
        if (message.Author.Id == BotResponses.TataId && BotResponses.TataIntentLines.TryGetValue(intent, out var tata))
        {
            pick = IntentPick.Of(tata, AnswerKinds.Intent(intent));
            return true;
        }

        if (isOwner)
        {
            if (!BotResponses.OwnerIntentLines.TryGetValue(intent, out var twin)) return false;
            pick = IntentPick.Of(twin, AnswerKinds.Intent(intent));
            return true;
        }

        // Only while she runs below the hot threshold: those lines call the number fine.
        if (intent == ChatIntent.HowAreYou && _health.CurrentMilli is { } milli && milli < PiHealth.HotMilli
            && Random.Shared.NextDouble() < TemperatureChance)
        {
            pick = new IntentPick(BotResponses.HowAreYouTempLines, PiHealth.Celsius(milli), string.Empty, null,
                AnswerKinds.Intent(intent));
            return true;
        }

        if (!BotResponses.IntentLines.TryGetValue(intent, out var pool)) return false;
        pick = IntentPick.Of(pool, AnswerKinds.Intent(intent));
        return true;
    }

    // A mean reading, unless the message is calling her a liar (« t'es une menteuse », « pourquoi tu
    // mens ? »): « menteuse » is an insult cue, but short and aimed at her it is teasing, and Disbelief
    // answers it — from Papa too, whose mean pool would take it as hurt. Longer insults stay mean
    // (Disbelief is six words at most).
    // Feeling down is let through the same way when it speaks to nobody: « journée de merde » swears
    // at the day, not at her, while « j'en ai marre de toi » stays an insult.
    private static bool IsMean(MessageMood mood, string content)
    {
        if (mood.Emotion != EmotionKind.Mean) return false;
        var intent = MessageCues.ReadIntent(content);
        return intent != ChatIntent.Disbelief && !(intent == ChatIntent.Down && !MessageCues.SpeaksToSomeone(content));
    }

    // « je devrais y aller ? »: /yesno's coin flip and its pools, so chat and the command agree.
    // « Valo ou LoL ? », « je joue ou je dors ? »: the same flip between the two sides, quoted as
    // written (ChoiceLines, {2} chosen, {3} not).
    private static IntentPick ShouldIPick(SocketUserMessage message)
    {
        var kind = AnswerKinds.Intent(ChatIntent.ShouldI);
        var yes = Random.Shared.Next(2) == 0;
        if (MessageCues.ReadChoice(message.Content ?? string.Empty) is { } choice)
        {
            var (chosen, rejected) = yes ? (choice.A, choice.B) : (choice.B, choice.A);
            return new IntentPick(BotResponses.ChoiceLines, chosen, rejected, QuotedMentions, kind);
        }
        return IntentPick.Of(yes ? BotResponses.YesLines : BotResponses.NoLines, kind);
    }

    // « lance un dé », « 1d20 », « un nombre entre 1 et 100 »: the roll itself (Helpers/Personality/Dice),
    // the same for everyone. {2} the result, {3} what was rolled.
    private static bool TryRoll(SocketUserMessage message, out IntentPick pick)
    {
        pick = default;
        if (Dice.Read(message.Content ?? string.Empty) is not { } ask) return false;
        var roll = Dice.Roll(ask, Random.Shared);
        pick = new IntentPick(BotResponses.IntentLines[ChatIntent.Roll], roll.Result, roll.Spec, null,
            AnswerKinds.Intent(ChatIntent.Roll));
        return true;
    }

    // « qui a raison, moi ou @Luca ? »: the side /rate ranks higher (Opinion.Prefers), so Papa wins
    // every argument and Quokka none. With one side only (« qui a raison ? », « dis-lui que j'ai
    // raison »), the asker faces whoever the message tags or replies to; nobody there, and she asks
    // against whom (IntentLines). The sides are shown as written, « moi » as the asker, muted.
    private bool TryTakeSides(SocketUserMessage message, out IntentPick pick)
    {
        pick = default;
        if (TakeSides.Read(message.Content ?? string.Empty) is not { } ask) return false;

        var kind = AnswerKinds.Intent(ChatIntent.TakeSides);
        var (asker, self) = (message.Author.Id, _client.CurrentUser.Id);
        if ((ask.B ?? Opponent(message, self)) is not { } b)
        {
            pick = IntentPick.Of(BotResponses.IntentLines[ChatIntent.TakeSides], kind);
            return true;
        }

        RatingResult Rate(string side) => Rating.Rate(side, asker, self, OwnerId, BotResponses.TataId);
        var aWins = Opinion.Prefers(Rate(ask.A), Rate(b), Rating.Key(ask.A, asker), Rating.Key(b, asker));
        var (winner, loser) = aWins ? (ask.A, b) : (b, ask.A);
        pick = new IntentPick(BotResponses.TakeSidesLines, SideName(winner, asker), SideName(loser, asker), QuotedMentions, kind);
        return true;
    }

    // The other side of « qui a raison ? »: the person tagged (not her, not the asker, not a bot), else
    // the person replied to. Null when there is nobody.
    private static string? Opponent(SocketUserMessage message, ulong selfId)
    {
        if (message.MentionedUsers.FirstOrDefault(u => u.Id != selfId && u.Id != message.Author.Id && !u.IsBot) is { } tagged)
            return $"<@{tagged.Id}>";
        if (message.ReferencedMessage?.Author is { IsBot: false } replied && replied.Id != message.Author.Id)
            return $"<@{replied.Id}>";
        return null;
    }

    // A side as she says it: « moi » is the asker's tag, « toi » is her name, anything else as written.
    private static string SideName(string side, ulong askerId) => Rating.Normalize(side) switch
    {
        "moi" or "moi meme" or "je" => $"<@{askerId}>",
        "toi" or "tu" or "toi meme" => "SYNCS",
        _ => side,
    };

    // Her name or tag and nothing else (« syncs ? », a bare tag): a call (ChatListening).
    private void MarkCall(SocketUserMessage message)
    {
        if (MessageCues.IsBareCall(message.Content ?? string.Empty))
            _listening.Called(message.Channel.Id, message.Author.Id, DateTimeOffset.UtcNow);
    }

    // A message with no tag, no reply and no name, from someone she is still listening to
    // (ChatListening): read as a reply to her. After a call, whatever it is (« syncs ? » … « pourquoi le
    // ciel est bleu ? »); after one of her answers, only what plainly asks her something, since they
    // may be talking to the room. Never a message said to someone else (a reply to another message,
    // another person tagged), and never while she is told to hush or breaking down.
    private async Task<bool> TryHandleFollowUpAsync(SocketUserMessage message)
    {
        var now = DateTimeOffset.UtcNow;
        var (called, answered) = _listening.Take(message.Channel.Id, message.Author.Id, now);
        if (!called && !answered) return false;

        if (message.ReferencedMessage is not null || message.MentionedEveryone || message.MentionedRoles.Count > 0
            || message.MentionedUsers.Any(u => u.Id != _client.CurrentUser.Id))
            return false;
        if (_breakdown.IsActive(message.Channel.Id)) return false;
        var isOwner = message.Author.Id == OwnerId;
        if (!isOwner && _hush.IsHushed(message.Channel.Id, now)) return false;

        var content = message.Content ?? string.Empty;
        if (!called && !MessageCues.ThreatensShutdown(content) && !AsksHer(content, message, isOwner)) return false;

        _logger.LogInformation("{Name} went on talking to her without her name — {Why}.", ResolveName(message.Author),
            called ? "after calling her" : "after her answer");
        // As a reply to her, not a mention: the mention pools speak of being tagged, which they weren't.
        // With no message replied to, the reply path reads no post context and no yes-or-no.
        await HandleReplyToBotAsync(message);
        return true;
    }

    // Specific intents that are about the room as often as about her: a laugh, « peut-être », « t'as
    // raison » to whoever spoke last, a goodnight to everyone.
    private static readonly HashSet<ChatIntent> RoomIntents = new()
    {
        ChatIntent.Laughter, ChatIntent.Maybe, ChatIntent.YouAreRight, ChatIntent.YouAreWrong, ChatIntent.Goodnight,
        ChatIntent.Goodbye,
    };

    // Whether a message she wasn't named in still plainly asks her something: a specific intent she
    // has an answer to (TryIntent), not mean, not one the room shares. « chut » is checked by name, as
    // TryIntent would hush the channel before the answer goes out.
    private bool AsksHer(string content, SocketUserMessage message, bool isOwner)
    {
        if (IsMean(MessageCues.Analyze(content), content)) return false;
        var intent = MessageCues.ReadIntent(content);
        if (RoomIntents.Contains(intent)) return false;
        return intent == ChatIntent.ShutUp || TryIntent(intent, message, isOwner, out _);
    }

    // Told « chut » in this channel (ChatHush): until it wears off, a reply or a mention gets a sulk
    // reaction and no words. Papa is never refused. True when it took the message.
    private async Task<bool> HushGateAsync(SocketUserMessage message, ChatPath path)
    {
        if (message.Author.Id == OwnerId || !_hush.IsHushed(message.Channel.Id, DateTimeOffset.UtcNow)) return false;
        Count(message, path, AnswerKinds.Hushed);
        await SulkReactAsync(message);
        return true;
    }
}
