using System.Globalization;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The bot's "personality": decides how to react to user messages — replies to
// the bot, @mentions, level-up announcements — and occasionally triggers the
// breakdown easter egg. Pure response logic; the canned text lives in
// BotResponses and the breakdown playback in BreakdownService.
internal sealed class ChatterService
{
    private readonly DiscordSocketClient _client;
    private readonly BreakdownService _breakdown;
    private readonly AvailabilityService _availability;
    private readonly GlitchService _glitch;
    private readonly ResponsePicker _picker;
    private readonly HerMessages _herMessages;
    private readonly PiHealthService _health;
    private readonly IServiceProvider _services;
    private readonly UnansweredLog _unanswered;
    private readonly ILogger<ChatterService> _logger;

    // Who has been going back and forth with her, per channel (ConversationStreak). In memory, and
    // shared with RobotHeartService, whose answers to her are turns of the same conversation.
    private readonly ConversationStreak _streaks;

    // Fact lines (her records, quoted): at most one per person every FactCooldown. Its own gate,
    // shared with nothing else.
    private readonly CooldownGate<ulong> _factGate = new(FactCooldown, forget: FactCooldown);

    // How often a fallback line becomes one of her records instead (ChatFacts), and how long a
    // person then waits before the next one. Rare on purpose: a surprise, not a dashboard.
    private const double FactChance = 0.15;
    private static readonly TimeSpan FactCooldown = TimeSpan.FromHours(6);

    // On the Pi, half of her « ça va ? » answers quote her real temperature.
    private const double TemperatureChance = 0.5;

    // Someone telling her about their day: half the time she is interested, half the time « euh, ok ? ».
    private const double SharingInterestChance = 0.5;

    // Rodhengard, the owner: gets compliments instead of roasts.
    private const ulong OwnerId = AvailabilityService.OwnerId;

    // 1-in-1000 chance of the breakdown; 1-in-200 of a pop-culture reference.
    private const double BreakdownChance = 0.001;
    private const double ReferenceChance = 0.008;

    // How often a sentence ending on "quoi ?" gets answered. Raised from the original
    // 1-in-50: the bait is rarer in practice than it looks, and at 2% the joke fired so
    // seldom that nobody connected the answer to the setup.
    private const double QuoicoubehChance = 0.2;

    // How often a sentence ending on a "si" sound gets "Tron" / "Trouille". Lower than
    // quoicoubeh: "merci", "aussi" and "ici" end sentences all day.
    private const double TronChance = 0.08;
    private static readonly string[] _tronLines = { "Tron", "Trouille" };

    // How often Tata gets warmth instead of the usual roast or brush-off. Deliberately
    // short of Papa's unconditional treatment: he is never teased, she mostly isn't,
    // everyone else always is. Three legible tiers rather than two arbitrary favourites.
    private const double TataWarmthChance = 0.6;

    // Secret passphrase: anyone replying with exactly this forces a breakdown.
    private const string BreakdownPassphrase = "The cake is a lie.";

    public ChatterService(
        DiscordSocketClient client,
        BreakdownService breakdown,
        AvailabilityService availability,
        ResponsePicker picker,
        GlitchService glitch,
        HerMessages herMessages,
        PiHealthService health,
        IServiceProvider services,
        UnansweredLog unanswered,
        ConversationStreak streaks,
        ILogger<ChatterService> logger)
    {
        _unanswered = unanswered;
        _streaks = streaks;
        _glitch = glitch;
        _client = client;
        _breakdown = breakdown;
        _availability = availability;
        _picker = picker;
        _herMessages = herMessages;
        _health = health;
        _services = services;
        _logger = logger;
    }

    public async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;

        // Bots are ignored, with one exception: the level-up bot's "passage de
        // niveau" announcement, which we congratulate.
        if (message.Author.IsBot)
        {
            await HandleLevelUpAsync(message);
            return;
        }

        // The owner answering, in DM, one of the absence notices the bot forwarded
        // him. Must come before the reply-to-bot branch below, which would
        // otherwise treat it as a reply to the bot and fire a comeback.
        if (await TryRelayOwnerReplyAsync(message)) return;

        // While the owner is flagged absent, a ping aimed at him gets a formal
        // "unavailable" notice — this takes priority over the usual chatter.
        if (await TryHandleOwnerAbsenceAsync(message)) return;

        // A verdict on the bot herself ("good bot" / "bad bot") belongs to
        // BotFeedbackTracker, which answers it and keeps the tally. Without this the
        // reply-to-bot branch below would swallow it and fire a comeback instead —
        // praising her would get you insulted. Checked after the DM relay above so
        // the owner answering a forwarded mention still relays normally.
        if (message.Channel is SocketGuildChannel
            && MessageCues.ReadFeedback(message.Content ?? string.Empty) != FeedbackKind.None) return;

        // Established behaviour: a reply to one of the bot's own messages gets a comeback.
        if (message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id)
        {
            await HandleReplyToBotAsync(message);
            return;
        }

        // A normal message that @mentions the bot. The owner can summon the bot
        // to roast whoever they're replying to; anyone else just gets a confused
        // one-liner (or a greeting back).
        if (message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id))
        {
            await HandleMentionAsync(message);
            return;
        }

        // The one branch that fires without being addressed at all: a shutdown verb
        // sitting next to her *name*. Saying "redémarrer syncs" is addressing her —
        // the name does the same disambiguating work an @mention does for the pronoun
        // phrases, so this is the only threat wording safe to catch ambiently. Last,
        // so anything aimed at her above still wins.
        if (MessageCues.ThreatensShutdownByName(message.Content ?? string.Empty))
        {
            await TryHandleShutdownThreatAsync(message, ChatPath.Name);
            return;
        }

        // Her name said in a conversation nobody addressed to her. After the threat above,
        // which names her too and must win.
        if (await TryHandleTalkedAboutAsync(message)) return;

        // Both eggs bait on the last word, so at most one can match; the first to fire
        // ends the pass either way.
        if (await TryQuoicoubehAsync(message)) return;
        await TryTronAsync(message);
    }

    // Someone said « syncs » without tagging or replying to her. She heard her name and has no
    // idea why: a puzzled one-liner (Papa gets his own, she is glad he talks about her). A mean
    // reading is the exception, and gets roasted as if it had been said to her face — from Papa
    // it lands, as on the reply path. A greeting with her name is addressed to her ("salut
    // syncs"), so it is greeted back like a mention would be. Guild only: in a DM her name is
    // already addressing her.
    //
    // No limits, on purpose: every message naming her gets its answer, like a mention does.
    //
    // A mean reading is scored over the whole message, so "syncs, ce boss est nul" reads as an
    // insult. Accepted: her name narrows the target far more than ReactionService's ambient read
    // does, and an undeserved roast is her normal register.
    private async Task<bool> TryHandleTalkedAboutAsync(SocketUserMessage message)
    {
        if (message.Channel is not SocketGuildChannel) return false;

        var content = message.Content ?? string.Empty;
        if (!MessageCues.NamesHer(content)) return false;
        // Silent, and swallowed: nothing interrupts a breakdown.
        if (_breakdown.IsActive(message.Channel.Id)) return true;

        var mood = MessageCues.Analyze(content);
        var isOwner = message.Author.Id == OwnerId;
        IntentPick pick;
        string what;

        // Called a liar to her face is teasing (Disbelief, below); behind her back it stays a backbite.
        if (mood.Emotion == EmotionKind.Mean
            && (IsMean(mood, content) || !MessageCues.AddressesHer(content, _client.CurrentUser.Id)))
        {
            what = "backbite roast";
            if (isOwner)
                pick = IntentPick.Of(BotResponses.OwnerMeanReplies, AnswerKinds.PapaMean);
            else
            {
                var pool = BotResponses.BackbiteRoasts;
                // Same double weight as the reply path's roast.
                if (BotResponses.PersonalComebacks.TryGetValue(message.Author.Id, out var personal))
                    pool = pool.Concat(personal).Concat(personal).ToArray();
                pick = IntentPick.Of(pool, AnswerKinds.Backbite);
            }
        }
        // « syncs, ça va ? » is addressing her even without a tag: a specific intent is answered
        // as such. Not a generic question or a low-effort word: she may not be the one asked. Nor
        // when her name is only mentioned (« @Luca, discute avec Syncs quand tu peux »): that
        // « tu » is Luca.
        else if (MessageCues.AddressesHer(content, _client.CurrentUser.Id)
                 && TryIntent(MessageCues.ReadIntent(content), message, isOwner, out var intentPick))
        {
            what = "named intent";
            pick = intentPick;
        }
        else if (mood.IsGreeting)
        {
            what = "named greeting";
            pick = isOwner
                ? IntentPick.Of(BotResponses.OwnerGreetings, AnswerKinds.PapaGreeting)
                : IntentPick.Of(BotResponses.Greetings, AnswerKinds.Greeting);
        }
        else
        {
            what = "talked-about line";
            pick = isOwner
                ? IntentPick.Of(BotResponses.TalkedAboutOwnerLines, AnswerKinds.PapaTalkedAbout)
                : IntentPick.Of(BotResponses.TalkedAboutLines, AnswerKinds.TalkedAbout);
        }

        var name = ResolveName(message.Author);
        _logger.LogInformation("{Name} said her name without addressing her — {What}.", name, what);
        Count(message, ChatPath.Name, pick.Kind);
        await ReplyWithTypingAsync(message, Format(pick, name, CurrentWeekday()), what, pick.Mentions);
        return true;
    }

    // Someone finished a sentence on a "si" sound ("merci", "aussi", "ici") and got
    // caught. Same ambient slot and same rationing philosophy as quoicoubeh, with a
    // lower roll: these endings are far commoner than "quoi", so the bait rations the
    // joke less and the odds have to do more of the work.
    private async Task TryTronAsync(SocketUserMessage message)
    {
        if (Random.Shared.NextDouble() >= TronChance) return;

        if (!MessageCues.EndsOnSiSound(message.Content ?? string.Empty)) return;

        _logger.LogInformation("Tron fired on a message ending in a 'si' sound.");
        await ReplyWithTypingAsync(message, _picker.Pick(_tronLines) + " " + Emotes.Sparkle, "tron");
    }

    // Someone finished a sentence on "quoi ?" and got caught. Ambient like the
    // shutdown-by-name branch above and below it in priority: anything actually aimed
    // at her is handled earlier and returns, so this only ever fires on the
    // conversations she was not part of — which is exactly where the joke belongs.
    //
    // The roll is the only rationing, deliberately. There is no per-channel cooldown
    // the way ReactionService and RivalryService have: ending a sentence on "quoi" is
    // itself uncommon, so the bait rations the joke far more than the odds do, and a
    // cooldown on top would bury it. Same call ReferenceChance makes.
    private async Task<bool> TryQuoicoubehAsync(SocketUserMessage message)
    {
        if (Random.Shared.NextDouble() >= QuoicoubehChance) return false;

        // Rolled before the match, which costs nothing in odds — both orders give
        // P(bait) x QuoicoubehChance — but skips tokenizing four messages in five, on
        // the hottest path in the bot.
        var answer = MessageCues.ReadQuoiBait(message.Content ?? string.Empty);
        if (answer is null) return false;

        _logger.LogInformation("Quoicoubeh fired on a message ending in bait.");
        await ReplyWithTypingAsync(message, answer, "quoicoubeh");
        return true;
    }

    // Answers the *other* leveling bot's announcements — congratulating the person and
    // sulking about where the level came from, in the same line. Posts a plain message
    // in the channel (no reply, no ping).
    //
    // She is jealous here rather than cheerful because the level was earned on a rival's
    // system instead of hers. The person who levelled is still owed a "bravo", which is
    // why this stays a single grudging line rather than a pure sulk, and why
    // RivalryService still skips exactly these messages: one response per announcement,
    // not a reaction and a mutter on top.
    private async Task HandleLevelUpAsync(SocketUserMessage message)
    {
        // Shared with RivalryService, which has to skip exactly the messages answered
        // here. See Helpers/Xp/LevelUpAnnouncement.
        if (!LevelUpAnnouncement.Matches(message.Author, message.Content)) return;
        LevelUpAnnouncement.TryReadLevel(message.Content, out var level);

        // Easter egg: level 67 gets the meme whatever her mood. A fixed string, so it
        // never goes through ResponsePicker and never burns one of that channel's
        // exclusion slots on a line the pool doesn't contain.
        var line = level == "67"
            ? "SIX SEVEEEN"
            : string.Format(_picker.Pick(BotResponses.RivalLevelUpLines), level);

        await PostWithTypingAsync(message.Channel, line, "rival level-up reaction");
    }

    // Handles a message that @mentions the bot (but isn't a reply to the bot).
    private async Task HandleMentionAsync(SocketUserMessage message)
    {
        // Don't let anyone interrupt an in-progress breakdown in this channel.
        if (_breakdown.IsActive(message.Channel.Id)) return;

        // Mistaking SYNCS for "Inabot" gets an indignant correction, no matter who
        // does it — even the owner.
        if (await TryCorrectMistakenIdentityAsync(message, ChatPath.Mention)) return;

        // A threat to switch her off outranks every mood below it, and outranks the
        // owner's rescue-roast branch too: if he is threatening *her*, that is not a
        // summons to roast someone else.
        if (await TryHandleShutdownThreatAsync(message, ChatPath.Mention)) return;

        var weekday = CurrentWeekday();

        // Rescue: the owner replies to someone and tags the bot -> roast that
        // someone. The target must be a real other person (not the bot, not the
        // owner themselves).
        if (message.Author.Id == OwnerId
            && message.ReferencedMessage is SocketUserMessage target
            && target.Author.Id != _client.CurrentUser.Id
            && target.Author.Id != OwnerId
            && !target.Author.IsBot)
        {
            var targetName = ResolveName(target.Author);
            _logger.LogInformation("Owner summoned a rescue roast against {Name}.", targetName);
            var roast = string.Format(
                _picker.Pick(BotResponses.RescueRoasts), targetName, weekday);
            // Reply to the target's own message so the roast is clearly aimed at
            // them (and pings them).
            Count(message, ChatPath.Mention, AnswerKinds.Rescue);
            await ReplyWithTypingAsync(target, roast, "rescue roast");
            return;
        }

        // Owner tagging the bot with no one to rescue: greet him — unless he is being
        // unkind, in which case it lands rather than bouncing. Reached only after the
        // rescue branch above, so a mean @mention aimed at *someone else* is already
        // handled and never mistaken for an insult to her.
        var content = message.Content ?? string.Empty;

        if (message.Author.Id == OwnerId)
        {
            var ownerMood = MessageCues.Analyze(content);
            bool unkind = IsMean(ownerMood, content);

            // Not mean: one of his intent twins when he asks her something she has a twin for
            // (« ça va ? », « bonne nuit »…), else the summons greeting as always.
            IntentPick ownerPick;
            string ownerWhat;
            if (unkind)
                (ownerPick, ownerWhat) = (IntentPick.Of(BotResponses.OwnerMeanReplies, AnswerKinds.PapaMean), "and was mean about it");
            else if (TryIntent(MessageCues.ReadIntent(content), message, isOwner: true, out var twin))
                (ownerPick, ownerWhat) = (twin, "his intent twin");
            else if (MessageCues.ReadIntent(content) == ChatIntent.Sharing)
                (ownerPick, ownerWhat) = (SharingPick(isOwner: true), "his news");
            else
                (ownerPick, ownerWhat) = (IntentPick.Of(BotResponses.OwnerGreetings, AnswerKinds.PapaGreeting), "greeting him");

            _logger.LogInformation("Owner mentioned the bot — {What}.", ownerWhat);
            Count(message, ChatPath.Mention, ownerPick.Kind);
            await ReplyWithTypingAsync(message, Format(ownerPick, ResolveName(message.Author), weekday),
                "owner greeting", ownerPick.Mentions);
            return;
        }

        // Anyone else: a confused one-liner — or, rarely, the breakdown. This is a
        // second entry point for the easter egg, opening on a cut-off "Tu veux qu-"
        // instead of the reply path's "C'est bien {0} on est cont-".
        var name = ResolveName(message.Author);

        if (Random.Shared.NextDouble() < BreakdownChance && _breakdown.TryBegin(message.Channel.Id))
        {
            _logger.LogInformation("Easter egg triggered via mention: consciousness breakdown.");
            var realName = BotResponses.RealNameFor(message.Author.Id, name);
            Count(message, ChatPath.Mention, AnswerKinds.Breakdown);
            await _breakdown.PlayAsync(message, name, realName, intro: "Tu veux qu-");
            return;
        }

        var (ended, streak) = await StreakGateAsync(message, ChatPath.Mention, name, weekday);
        if (ended) return;

        // What the mention says comes first: a specific intent, then a greeting (greeted back, a
        // second entry point beside replying), Tata's warmth, a real question. A mean reading
        // cancels all of them. Otherwise the bot just answers with a confused one-liner — no
        // low-effort reading here: a bare tag *is* her « tagged for nothing » pool.
        var mood = MessageCues.Analyze(content);
        var mean = IsMean(mood, content);
        var intent = mean ? ChatIntent.None : MessageCues.ReadIntent(content);

        IntentPick pick;
        string what = "mention reply";
        if (TryIntent(intent, message, isOwner: false, out var intentPick))
            (pick, what) = (intentPick, $"mention intent ({intent})");
        else if (mood.IsGreeting && !mean)
            pick = IntentPick.Of(BotResponses.Greetings, AnswerKinds.Greeting);
        else if (RollTataWarmth(message.Author.Id, mood))
            // Family doesn't get « tagged for nothing » for tagging her. TataGreetings, not
            // TataReplies: a mention is Tata *calling her over*, so she answers as
            // summoned rather than as mid-conversation — the same split Papa has.
            pick = IntentPick.Of(BotResponses.TataGreetings, AnswerKinds.Tata);
        else if (intent == ChatIntent.Sharing)
            (pick, what) = (SharingPick(isOwner: false), "mention sharing");
        else if (intent == ChatIntent.Question)
            (pick, what) = (IntentPick.Of(BotResponses.IntentLines[ChatIntent.Question], AnswerKinds.Intent(ChatIntent.Question)),
                "mention question");
        else
        {
            _logger.LogInformation("{Name} mentioned the bot.", name);
            await SendFallbackAsync(message, ChatPath.Mention, AnswerKinds.Interrogation, streak, BotResponses.Interrogations,
                name, weekday, "mention reply");
            return;
        }

        _logger.LogInformation("{Name} mentioned the bot — {What}.", name, what);
        Count(message, ChatPath.Mention, pick.Kind);
        await ReplyWithTypingAsync(message, Format(pick, name, weekday), what, pick.Mentions);
    }

    private async Task HandleReplyToBotAsync(SocketUserMessage message)
    {
        // Don't let anyone interrupt an in-progress breakdown in this channel.
        if (_breakdown.IsActive(message.Channel.Id)) return;

        var name = ResolveName(message.Author);
        _logger.LogInformation("{Name} replied to the bot.", name);

        // Calling SYNCS "Inabot" gets an indignant correction before anything else.
        if (await TryCorrectMistakenIdentityAsync(message, ChatPath.Reply)) return;

        // A threat to switch her off outranks every branch below — including the
        // breakdown roll, which must not swallow the one message she most needs to
        // answer, and the owner-mean pool, which is far too gentle for this.
        if (await TryHandleShutdownThreatAsync(message, ChatPath.Reply)) return;

        // Read the message text (requires the MessageContent intent) to detect
        // kind words or a greeting and answer in kind. A mean reading outweighs the
        // nice/greeting treatment — we roast instead.
        var content = message.Content ?? string.Empty;
        var mood = MessageCues.Analyze(content);
        var nice = mood.Emotion == EmotionKind.Nice;
        var greeting = mood.Emotion != EmotionKind.Mean && mood.IsGreeting;

        // Secret trigger: replying with the passphrase forces the breakdown,
        // bypassing the random roll and the cooldown. Open to anyone.
        var secretTrigger = content.Trim() == BreakdownPassphrase;

        if ((secretTrigger || Random.Shared.NextDouble() < BreakdownChance)
            && _breakdown.TryBegin(message.Channel.Id, ignoreCooldown: secretTrigger))
        {
            _logger.LogInformation("Easter egg triggered: consciousness breakdown.");
            // Intro uses the pseudo; the breakdown reveal uses the real name when known.
            // A kind message opens with a glitching thank-you instead of a roast.
            var realName = BotResponses.RealNameFor(message.Author.Id, name);
            var intro = secretTrigger ? BotResponses.BreakdownIntroCake
                : nice ? BotResponses.BreakdownIntroNice
                : BotResponses.BreakdownIntroRoast;
            Count(message, ChatPath.Reply, AnswerKinds.Breakdown);
            await _breakdown.PlayAsync(message, name, realName, intro);
            return;
        }

        var weekday = CurrentWeekday();
        var isOwner = message.Author.Id == OwnerId;
        var mean = IsMean(mood, content);

        // Five replies in a row and she ends it; while ended, only a reaction. Never Papa.
        var (ended, streak) = await StreakGateAsync(message, ChatPath.Reply, name, weekday);
        if (ended) return;

        // What the reply says, then what it answers, then its mood. A mean reply skips the first
        // two: it gets roasted (or, from Papa, lands), whatever it was about.
        var intent = mean ? ChatIntent.None : MessageCues.ReadIntent(content);
        // A sticker alone has no text to read: it says as little as an emote alone, which reads as
        // LowEffort, so it is answered the same way rather than roasted as if unread.
        if (intent == ChatIntent.None && string.IsNullOrWhiteSpace(content) && message.Stickers.Count > 0)
            intent = ChatIntent.LowEffort;

        IntentPick pick;
        string what = "reply comeback";
        if (mean && isOwner)
        {
            // Everyone else gets roasted back. Her creator is the one person she will
            // not fight with, so an insult from him lands instead of bouncing.
            // Checked before the reference roll: a pop-culture one-liner in answer to
            // him being cruel would read as her not having noticed.
            pick = IntentPick.Of(BotResponses.OwnerMeanReplies, AnswerKinds.PapaMean);
        }
        else if (TryIntent(intent, message, isOwner, out var intentPick))
        {
            // Papa reaches this only for an intent he has a twin for (or an opinion, which is
            // the same for everyone); the rest of his replies go on down to his own pools.
            (pick, what) = (intentPick, $"reply intent ({intent})");
        }
        else if (!mean && !isOwner && ReadReplyContext(message) is { } context)
        {
            // An answer to one of her own posts (the late call, a welcome, the morning
            // hello…) is answered about that post. Papa keeps his pools.
            (pick, what) = (IntentPick.Of(BotResponses.ReplyContextLines[context], AnswerKinds.Context(context)),
                $"reply to her {context}");
        }
        else if (nice)
        {
            pick = IntentPick.Of(BotResponses.NiceReplies, AnswerKinds.Nice);
        }
        else if (greeting)
        {
            pick = IntentPick.Of(BotResponses.Greetings, AnswerKinds.Greeting);
        }
        else if (intent == ChatIntent.Sharing)
        {
            (pick, what) = (SharingPick(isOwner), "reply sharing");
        }
        else if (!isOwner && intent is ChatIntent.Question or ChatIntent.LowEffort)
        {
            (pick, what) = (IntentPick.Of(BotResponses.IntentLines[intent], AnswerKinds.Intent(intent)), $"reply intent ({intent})");
        }
        else if (Random.Shared.NextDouble() < ReferenceChance)
        {
            // Rarer easter egg: a pop-culture reference, for everyone.
            pick = IntentPick.Of(BotResponses.ReferenceComebacks, AnswerKinds.Reference);
        }
        else if (RollTataWarmth(message.Author.Id, mood))
        {
            pick = IntentPick.Of(BotResponses.TataReplies, AnswerKinds.Tata);
        }
        else if (isOwner)
        {
            pick = IntentPick.Of(BotResponses.OwnerComebacks, AnswerKinds.PapaComeback);
        }
        else
        {
            var pool = BotResponses.Comebacks;
            // Fold in this person's custom lines twice, so each has double weight.
            if (BotResponses.PersonalComebacks.TryGetValue(message.Author.Id, out var personal))
                pool = pool.Concat(personal).Concat(personal).ToArray();
            await SendFallbackAsync(message, ChatPath.Reply, AnswerKinds.Comeback, streak, pool, name, weekday, what);
            return;
        }

        Count(message, ChatPath.Reply, pick.Kind);
        await ReplyWithTypingAsync(message, Format(pick, name, weekday), what, pick.Mentions);
    }

    // ---- Listening: intents, reply context, streaks, facts --------------------------------

    /// <summary>
    /// A pool, and what its lines' {2} and {3} stand for: her temperature, an opinion's subject and
    /// verdict, a request's command. <see cref="Mentions"/> is set when {2}/{3} quote the person's
    /// own words (an opinion's subject), which may hold a mention: shown, never pinged.
    /// <see cref="Kind"/> is the AnswerKinds key `/debug answers` counts it by.
    /// </summary>
    private readonly record struct IntentPick(string[] Pool, string A, string B, AllowedMentions? Mentions, string Kind)
    {
        public static IntentPick Of(string[] pool, string kind) => new(pool, string.Empty, string.Empty, null, kind);
    }

    // Counts an answer for `/debug answers` (ChatAnswerService), off the gateway handler: the
    // fan-out awaits every handler, and a failed count must never cost an answer. A message she
    // found nothing specific to say to is also kept, without its author (UnansweredLog).
    private void Count(SocketUserMessage message, ChatPath path, string kind)
    {
        var guildId = (message.Channel as SocketGuildChannel)?.Guild.Id ?? 0;
        if (AnswerKinds.IsUnrecognised(kind))
            _unanswered.Add(guildId, path, kind, message.Content ?? string.Empty, DateTimeOffset.UtcNow);

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ChatAnswerService>().AddAsync(guildId, path, kind);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not count a {Kind} answer.", kind);
            }
        });
    }

    // Quoted words keep their look but ping nobody; the person replied to still gets the reply ping.
    private static readonly AllowedMentions QuotedMentions = new(AllowedMentionTypes.None) { MentionRepliedUser = true };

    // The template is picked first, formatted second: {0} the name, {1} the weekday, {2}/{3} the pick's.
    private string Format(IntentPick pick, string name, string weekday) =>
        string.Format(_picker.Pick(pick.Pool), name, weekday, pick.A, pick.B);

    /// <summary>
    /// The answer to a specific intent (not a generic question or a low-effort word), or false.
    /// Papa only has twins for some intents; for the others this is false and he goes on to his
    /// own pools — except an opinion and a ship, the same verdict whoever asks, and help and
    /// requests, which he gets in his devoted request pool.
    /// </summary>
    private bool TryIntent(ChatIntent intent, SocketUserMessage message, bool isOwner, out IntentPick pick)
    {
        pick = default;
        if (intent is ChatIntent.None or ChatIntent.Question or ChatIntent.Sharing or ChatIntent.LowEffort) return false;

        if (intent == ChatIntent.Opinion) return TryOpinion(message, out pick);
        if (intent == ChatIntent.Ship) return TryShip(message, out pick);

        if (intent == ChatIntent.Help)
        {
            // The command for what they asked about; Papa gets his devoted request pool around it.
            var command = BotResponses.HelpCommands[MessageCues.ReadTopic(message.Content ?? string.Empty)];
            pick = new IntentPick(isOwner ? BotResponses.OwnerRequestLines : BotResponses.HelpLines, command,
                string.Empty, null, AnswerKinds.Intent(intent));
            return true;
        }

        if (intent == ChatIntent.AboutHer && !isOwner)
        {
            var topic = MessageCues.ReadHerTopic(message.Content ?? string.Empty) ?? HerTopic.Favourite;
            var days = topic == HerTopic.Age ? MorningGreeting.DaysOld(DateTimeOffset.UtcNow).ToString() : string.Empty;
            pick = new IntentPick(BotResponses.AboutHerLines[topic], days, string.Empty, null, AnswerKinds.Intent(intent));
            return true;
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
    private static bool IsMean(MessageMood mood, string content) =>
        mood.Emotion == EmotionKind.Mean && MessageCues.ReadIntent(content) != ChatIntent.Disbelief;

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

        var answer = ShipModule.Render(a, b, asker, self, _picker, side => ShipModule.NameOf(side, guild, _client));
        pick = new IntentPick(pool, answer, string.Empty, QuotedMentions, AnswerKinds.Intent(ChatIntent.Ship));
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
    // morning hello by its text, any slash-command result by its interaction metadata.
    private ReplyContext? ReadReplyContext(SocketUserMessage message)
    {
        if (message.ReferencedMessage is not { } replied) return null;
        if (_herMessages.TryGet(replied.Id, out var kind)) return kind;
        if (MorningGreetingService.IsMorningHello(replied)) return ReplyContext.MorningHello;
        if (replied.InteractionMetadata is not null) return ReplyContext.CommandResult;
        return null;
    }

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

    // When the owner is flagged absent and someone (other than the owner) pings
    // him, replies with a formal "unavailable" notice and returns true so the
    // caller stops there. A ping that doesn't target the owner is left alone.
    private async Task<bool> TryHandleOwnerAbsenceAsync(SocketUserMessage message)
    {
        if (!_availability.IsOwnerAbsent) return false;
        if (message.Author.Id == OwnerId) return false;
        if (!message.MentionedUsers.Any(u => u.Id == OwnerId)) return false;

        var name = ResolveName(message.Author);
        _logger.LogInformation("{Name} pinged the absent owner — sending unavailability notice.", name);
        var notice = string.Format(
            _picker.Pick(BotResponses.OwnerAbsentNotices),
            name, CurrentWeekday());
        await ReplyWithTypingAsync(message, notice, "owner-absence notice");

        await NotifyOwnerOfMentionAsync(message, name);
        return true;
    }

    // Discord caps a message at 2000 characters
    private const int MaxQuotedLength = 1200;

    // DMs the owner a transcript of a mention received while he was away: who,
    // where, what they said, and a jump link back to the message.
    private async Task NotifyOwnerOfMentionAsync(SocketUserMessage message, string authorName)
    {
        try
        {
            var owner = await _client.GetUserAsync(OwnerId);
            if (owner is null)
            {
                _logger.LogWarning("Could not resolve the owner to forward an absence mention.");
                return;
            }

            var ts = message.Timestamp.ToUnixTimeSeconds();
            var where = message.Channel is IGuildChannel guildChannel
                ? $"dans <#{message.Channel.Id}> ({guildChannel.Guild.Name})"
                : "en message privé";

            var dm = await owner.CreateDMChannelAsync();
            var notice = await dm.SendMessageAsync(
                $"📬 **{authorName}** t'a mentionné {where} <t:{ts}:R> :\n" +
                $"{QuoteContent(message)}\n" +
                $"[Aller au message]({message.GetJumpUrl()})\n" +
                $"-# Réponds à ce message pour lui répondre directement dans le salon.",
                // The excerpt can contain pings; forwarding it must not re-ping anyone.
                allowedMentions: AllowedMentions.None);

            // Remember where this notice came from, so a reply to it can be routed
            // back to the original message. Only meaningful in a guild.
            if (message.Channel is IGuildChannel)
            {
                _availability.RememberMention(notice.Id, new PendingMention(
                    GuildId: ((IGuildChannel)message.Channel).GuildId,
                    ChannelId: message.Channel.Id,
                    MessageId: message.Id,
                    AuthorName: authorName));
            }
        }
        catch (HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            _logger.LogWarning("Owner has DMs disabled; could not forward the absence mention.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to forward an absence mention to the owner.");
        }
    }

    // Renders the message text as a Markdown blockquote, noting attachments and
    // handling the text-less case (a lone image, a sticker).
    private static string QuoteContent(SocketUserMessage message)
    {
        var content = (message.Content ?? string.Empty).Trim();
        var quoted = content.Length == 0 ? "*(aucun texte)*" : Quote(content);

        if (message.Attachments.Count > 0)
            quoted += $"\n> *({message.Attachments.Count} pièce(s) jointe(s))*";

        return quoted;
    }

    // Truncates to the excerpt cap and renders every line as a blockquote.
    private static string Quote(string text) => MessageFormat.Quote(text, MaxQuotedLength);

    // The owner replying, in DM, to a forwarded absence notice: the text is posted
    // back into the original channel as a reply to whoever pinged him. Returns
    // true when the message was one of those replies, handled or not.
    private async Task<bool> TryRelayOwnerReplyAsync(SocketUserMessage message)
    {
        if (message.Author.Id != OwnerId) return false;
        if (message.Channel is not IDMChannel) return false;
        if (message.ReferencedMessage is not { } notice) return false;
        if (notice.Author.Id != _client.CurrentUser.Id) return false;
        if (!_availability.TryGetMention(notice.Id, out var pending)) return false;

        var reply = (message.Content ?? string.Empty).Trim();
        if (reply.Length == 0)
        {
            // Nothing to relay — an image-only reply would post an empty message.
            await ReplyInDmAsync(message, "Je n'ai rien à transmettre : ton message est vide. ✍️");
            return true;
        }

        try
        {
            var guild = _client.GetGuild(pending.GuildId);
            var channel = guild?.GetTextChannel(pending.ChannelId);
            if (channel is null || await channel.GetMessageAsync(pending.MessageId) is not IUserMessage original)
            {
                await ReplyInDmAsync(message,
                    "Impossible de retrouver le message d'origine — il a peut-être été supprimé. ❌");
                return true;
            }

            var ownerName = guild!.GetUser(OwnerId)?.Nickname ?? "Rodhengard";
            var herald = string.Format(
                _picker.Pick(BotResponses.OwnerReplyHeralds),
                ownerName);

            await original.ReplyAsync(
                $"{herald}\n{Quote(reply)}",
                // Ping the person being answered, and honour any user the owner
                // mentioned — but never @everyone/@here or a role.
                allowedMentions: new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = true });

            _logger.LogInformation("Relayed an owner reply to {Name} in channel {ChannelId}.",
                pending.AuthorName, pending.ChannelId);

            await ReplyInDmAsync(message, $"Transmis à **{pending.AuthorName}**. ✅");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to relay an owner reply to channel {ChannelId}.", pending.ChannelId);
            await ReplyInDmAsync(message, "Je n'ai pas réussi à transmettre ta réponse. ❌");
        }

        return true;
    }

    // Small acknowledgement back in the DM thread; never worth failing over.
    private async Task ReplyInDmAsync(SocketUserMessage message, string text)
    {
        try
        {
            await message.ReplyAsync(text, allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to acknowledge the owner's DM reply.");
        }
    }

    // If the message calls the bot "Inabot", fires back an indignant correction
    // (the bot is SYNCS) and returns true so the caller stops there.
    // Someone threatening to switch her off. The one thing she reacts to more
    // strongly than an insult, and the only behaviour where the favouritism inverts:
    // being able to actually carry the threat out makes it *worse*, not gentler.
    //
    // Three tiers, by how credible the threat is and whether she can appeal to a
    // relationship:
    //   Rodhengard — wrote her, could unplug her, nothing to bargain with → terror.
    //   Tata       — family, but holds the server permissions → pleading and
    //                bargaining, the only tier where she still thinks talking works.
    //   Anyone else — no permissions, pure bluff → fury, and she says so.
    //
    // Checked only on messages aimed at her (reply or mention), never ambiently: the
    // vocabulary overlaps with ordinary talk about restarting a game server, and
    // "faut couper le serveur" said about Minecraft must not terrify her.
    private async Task<bool> TryHandleShutdownThreatAsync(SocketUserMessage message, ChatPath path)
    {
        // The by-name path already matched its own wording (ThreatensShutdownByName).
        if (path != ChatPath.Name && !MessageCues.ThreatensShutdown(message.Content ?? string.Empty)) return false;

        var name = ResolveName(message.Author);

        var (pool, who) = message.Author.Id switch
        {
            OwnerId => (BotResponses.ShutdownThreatOwner, "terrified (owner)"),
            BotResponses.TataId => (BotResponses.ShutdownThreatTata, "bargaining (Tata)"),
            _ => (BotResponses.ShutdownThreatReplies, "furious"),
        };

        _logger.LogInformation("{Name} threatened to shut the bot down — {Reaction}.", name, who);

        var line = string.Format(_picker.Pick(pool), name, CurrentWeekday());
        Count(message, path, AnswerKinds.Shutdown);
        await ReplyWithTypingAsync(message, line, "shutdown-threat reply");
        return true;
    }

    private async Task<bool> TryCorrectMistakenIdentityAsync(SocketUserMessage message, ChatPath path)
    {
        if (!MessageCues.IsMistakenIdentity(message.Content ?? string.Empty)) return false;

        var name = ResolveName(message.Author);
        _logger.LogInformation("{Name} called the bot 'Inabot' — correcting them.", name);
        var line = string.Format(
            _picker.Pick(BotResponses.MistakenIdentityReplies),
            name, CurrentWeekday());
        Count(message, path, AnswerKinds.Inabot);
        await ReplyWithTypingAsync(message, line, "mistaken-identity reply");
        return true;
    }

    // ---- Sending a personality line ---------------------------------------
    // Thin wrappers over Helpers/Core/BotChat, which owns the typing pause, the delay
    // formula and the swallow-and-log. It lives there rather than here because
    // BotFeedbackTracker sends chatter too, and a second copy of the delay clamp —
    // which has to stay inside Discord.Net's 3 s HandlerTimeout — is exactly the
    // kind of constant that drifts apart. `what` only labels the log line. Through
    // GlitchService, so a very rare line comes out corrupted and is quietly fixed.

    private Task ReplyWithTypingAsync(
        SocketUserMessage replyTo, string line, string what, AllowedMentions? allowedMentions = null) =>
        _glitch.ReplyWithTypingAsync(replyTo, line, _logger, what, allowedMentions);

    private Task PostWithTypingAsync(ISocketMessageChannel channel, string line, string what) =>
        _glitch.PostWithTypingAsync(channel, line, _logger, what);

    // The current weekday name in French, for the {1} format placeholder.
    private static string CurrentWeekday() =>
        AppTime.Now.ToString("dddd", CultureInfo.GetCultureInfo("fr-FR"));

    // Whether this message should get Tata's warm pool instead of the usual roast or
    // brush-off. **Rolls the dice**, so call it exactly once per message.
    //
    // A mean message never qualifies: she is warm to her aunt, not a doormat, so an
    // insult from Tata bounces back like anyone else's — and her PersonalComebacks
    // lines are still folded into that roast pool. Unlike Papa, who is exempt from
    // teasing outright, Tata is merely favoured.
    private static bool RollTataWarmth(ulong authorId, MessageMood mood) =>
        authorId == BotResponses.TataId
        && mood.Emotion != EmotionKind.Mean
        && Random.Shared.NextDouble() < TataWarmthChance;

    // Resolves the friendliest display name available: a family override (see
    // BotResponses.FamilyNicknames) first, then server nickname, then global display
    // name, then username.
    private static string ResolveName(IUser user) => BotResponses.DisplayNameFor(user);
}
