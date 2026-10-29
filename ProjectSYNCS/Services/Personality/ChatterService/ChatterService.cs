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
internal sealed partial class ChatterService
{
    private readonly DiscordSocketClient _client;
    private readonly BreakdownService _breakdown;
    private readonly AvailabilityService _availability;
    private readonly GlitchService _glitch;
    private readonly ResponsePicker _picker;
    private readonly HerMessages _herMessages;
    private readonly PiHealthService _health;
    private readonly IServiceProvider _services;
    private readonly ActivityCounter _activity;
    private readonly ILogger<ChatterService> _logger;

    // Secret badges earned through her chat; fire-and-forget, never awaited on the gateway path.
    private readonly BadgeAwarder _badges;

    // Who has been going back and forth with her, per channel (ConversationStreak). In memory, and
    // shared with RobotHeartService, whose answers to her are turns of the same conversation.
    private readonly ConversationStreak _streaks;

    // The channels where she was told « chut » (ChatHush), shared with every service that chats there.
    private readonly ChatHush _hush;

    // Who she is still listening to after a call or an answer (ChatListening): « syncs ? » then
    // « t'es en quelle version ? » is one question in two messages.
    private readonly ChatListening _listening;

    // Replies under her cards (« votez ! », « j'ai voté », « remonte-le »), answered about the card.
    private readonly CardReplyService _cards;

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
        CardReplyService cards,
        BadgeAwarder badges,
        ActivityCounter activity,
        ILogger<ChatterService> logger)
    {
        _activity = activity;
        _badges = badges;
        _cards = cards;
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
        await SendPickAsync(message, pick, name, CurrentWeekday(), what);
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
        if (await ReplyWithTypingAsync(message, _picker.Pick(_tronLines) + " " + Emotes.Sparkle, "tron") is not null)
            Award(message, MemberBadges.Keys.Tron);
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
        if (await ReplyWithTypingAsync(message, answer, "quoicoubeh") is not null)
            Award(message, MemberBadges.Keys.Quoicoubeh);
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
            await SendPickAsync(message, ownerPick, ResolveName(message.Author), weekday, "owner greeting");
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
            Award(message, MemberBadges.Keys.Breakdown);
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
        await SendPickAsync(message, pick, name, weekday, what);
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

        // About one of her cards (« votez ! », « j'ai voté », « remonte-le »): answered about the card.
        if (TryCardReply(message, name)) return;

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
            Award(message, MemberBadges.Keys.Breakdown);
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
        else if (!mean && !isOwner && intent == ChatIntent.None && AskedSomething(message.ReferencedMessage)
                 && MessageCues.ReadShortAnswer(content) is { } said)
        {
            // A word or two under one of her questions (« Java », « 8 jours »): an answer, not a jab.
            // Their words are quoted back, sanitised and muted.
            (pick, what) = (new IntentPick(BotResponses.OpenAnswerLines, Discord.Format.Sanitize(said), string.Empty,
                QuotedMentions, AnswerKinds.OpenAnswer), "answer to her question");
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
        await SendPickAsync(message, pick, name, weekday, what);
    }

    // ---- Sending a personality line ---------------------------------------
    // Thin wrappers over Helpers/Core/BotChat, which owns the typing pause, the delay
    // formula and the swallow-and-log. It lives there rather than here because
    // BotFeedbackTracker sends chatter too, and a second copy of the delay clamp —
    // which has to stay inside Discord.Net's 3 s HandlerTimeout — is exactly the
    // kind of constant that drifts apart. `what` only labels the log line. Through
    // GlitchService, so a very rare line comes out corrupted and is quietly fixed.

    // Every answer keeps her listening to that person for a moment (ChatListening).
    // Null when the send failed (BotChat), which the secret-badge sites check.
    private Task<IUserMessage?> ReplyWithTypingAsync(
        SocketUserMessage replyTo, string line, string what, AllowedMentions? allowedMentions = null)
    {
        if (!replyTo.Author.IsBot) _listening.Answered(replyTo.Channel.Id, replyTo.Author.Id, DateTimeOffset.UtcNow);
        return _glitch.ReplyWithTypingAsync(replyTo, line, _logger, what, allowedMentions);
    }

    // One secret badge for the author of a message, in the guild it was said in (0 in a DM: no award).
    private void Award(SocketUserMessage message, string key) =>
        _badges.Award((message.Channel as SocketGuildChannel)?.Guild.Id ?? 0, message.Author.Id, key);

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
