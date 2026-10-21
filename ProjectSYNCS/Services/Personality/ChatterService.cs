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
    private readonly ILogger<ChatterService> _logger;

    // Who has been going back and forth with her, per channel (ConversationStreak). In memory, and
    // shared with RobotHeartService, whose answers to her are turns of the same conversation.
    private readonly ConversationStreak _streaks;

    // The channels where she was told « chut » (ChatHush), shared with every service that chats there.
    private readonly ChatHush _hush;

    // Who she is still listening to after a call or an answer (ChatListening): « syncs ? » then
    // « t'es en quelle version ? » is one question in two messages.
    private readonly ChatListening _listening;

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
        ConversationStreak streaks,
        ChatHush hush,
        ChatListening listening,
        ILogger<ChatterService> logger)
    {
        _streaks = streaks;
        _hush = hush;
        _listening = listening;
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

        // Right after « syncs ? » or one of her answers, what that person says next may still be for
        // her without her name. After the talked-about branch: a message naming her is read there.
        if (await TryHandleFollowUpAsync(message)) return;

        // Told « chut » here: the eggs are her own initiative, and she is keeping quiet.
        if (_hush.IsHushed(message.Channel.Id, DateTimeOffset.UtcNow)) return;

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
    //
    // Papa's pet names (« ma framboise », « ma fille ») name her too, from him only. A mean reading
    // or a specific intent wins as with her name; anything else gets the pet name's own pool, ahead
    // of the greeting: being called that is what she reacts to.
    private async Task<bool> TryHandleTalkedAboutAsync(SocketUserMessage message)
    {
        if (message.Channel is not SocketGuildChannel) return false;

        var content = message.Content ?? string.Empty;
        var isOwner = message.Author.Id == OwnerId;
        var petName = isOwner ? MessageCues.ReadPetName(content) : PetName.None;
        if (petName == PetName.None && !MessageCues.NamesHer(content)) return false;
        // Silent, and swallowed: nothing interrupts a breakdown, and a « chut » holds against her name
        // said in passing. Papa is never kept waiting.
        if (_breakdown.IsActive(message.Channel.Id)) return true;
        if (!isOwner && _hush.IsHushed(message.Channel.Id, DateTimeOffset.UtcNow)) return true;
        // « syncs ? », « hé syncs »: a call, so their next message is for her (ChatListening).
        MarkCall(message);

        var mood = MessageCues.Analyze(content);
        var selfId = _client.CurrentUser.Id;
        var addressed = MessageCues.AddressesHer(content, selfId)
                        || petName != PetName.None && MessageCues.AddressesHerByPetName(content, selfId);
        var name = ResolveName(message.Author);
        var intent = ChatIntent.None;
        IntentPick pick;
        string what;

        // Called a liar to her face is teasing (Disbelief, below); behind her back it stays a backbite.
        if (mood.Emotion == EmotionKind.Mean && (IsMean(mood, content) || !addressed))
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
        else if (addressed
                 && TryIntent(intent = MessageCues.ReadIntent(content), message, isOwner, out var intentPick))
        {
            if (TryAnswerFromRecords(intent, message, ChatPath.Name, intentPick, name, CurrentWeekday())) return true;
            what = "named intent";
            pick = intentPick;
        }
        else if (PetNamePick(petName) is { } petPick)
        {
            what = "pet name";
            pick = petPick;
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
        // A bare tag is a call: their next message is for her (ChatListening).
        MarkCall(message);

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
            // (« ça va ? », « bonne nuit »…), then his pet name for her, else the summons greeting
            // as always.
            IntentPick ownerPick;
            string ownerWhat;
            if (unkind)
                (ownerPick, ownerWhat) = (IntentPick.Of(BotResponses.OwnerMeanReplies, AnswerKinds.PapaMean), "and was mean about it");
            else if (TryIntent(MessageCues.ReadIntent(content), message, isOwner: true, out var twin))
                (ownerPick, ownerWhat) = (twin, "his intent twin");
            else if (PetNamePick(MessageCues.ReadPetName(content)) is { } petPick)
                (ownerPick, ownerWhat) = (petPick, "his pet name for her");
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

        if (await HushGateAsync(message, ChatPath.Mention)) return;

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
        {
            if (TryAnswerFromRecords(intent, message, ChatPath.Mention, intentPick, name, weekday)) return;
            (pick, what) = (intentPick, $"mention intent ({intent})");
        }
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

        // Told « chut » in this channel: a sulk reaction, no words, the breakdown included.
        if (await HushGateAsync(message, ChatPath.Reply)) return;

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
        // A bare « oui » / « non » under one of her questions answers it. Not when the post has its own
        // context line (« oui » under the late call is « j'arrive »), which Papa never gets anyway.
        if (!mean && MessageCues.ReadAnswer(content) is { } answer && AskedSomething(message.ReferencedMessage)
            && (isOwner || ReadReplyContext(message) is null))
            intent = answer;

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
            if (TryAnswerFromRecords(intent, message, ChatPath.Reply, intentPick, name, weekday)) return;
            (pick, what) = (intentPick, $"reply intent ({intent})");
        }
        else if (isOwner && PetNamePick(MessageCues.ReadPetName(content)) is { } petPick)
        {
            // Being called that outranks whatever else his reply was: his nice, his greeting, his news.
            (pick, what) = (petPick, "his pet name for her");
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
    // found nothing specific to say to is also kept, without its author, in the same save
    // (UnansweredLog: 30 rolling days).
    private void Count(SocketUserMessage message, ChatPath path, string kind)
    {
        var guildId = (message.Channel as SocketGuildChannel)?.Guild.Id ?? 0;
        var unanswered = AnswerKinds.IsUnrecognised(kind) ? message.Content ?? string.Empty : null;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ChatAnswerService>().AddAsync(guildId, path, kind, unanswered);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not count a {Kind} answer.", kind);
            }
        });
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
        if (intent == ChatIntent.ShutUp) _hush.Hush(message.Channel.Id, DateTimeOffset.UtcNow);

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

    // Every answer keeps her listening to that person for a moment (ChatListening).
    private Task ReplyWithTypingAsync(
        SocketUserMessage replyTo, string line, string what, AllowedMentions? allowedMentions = null)
    {
        if (!replyTo.Author.IsBot) _listening.Answered(replyTo.Channel.Id, replyTo.Author.Id, DateTimeOffset.UtcNow);
        return _glitch.ReplyWithTypingAsync(replyTo, line, _logger, what, allowedMentions);
    }

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
