using ProjectSYNCS.Commands;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// An answer to something said to her in game that isn't an order: a pool from Discord (made fit for the game by the
/// service, <see cref="McChat.Game"/>), and what its {2} and {3} stand for. <see cref="Remark"/>: she was talked about,
/// not to, so answering is her own initiative, rationed like her remarks. <see cref="Fallback"/>: nothing in the message
/// was understood, only that it was said to her (the streak's nudge replaces it on the third message, as on Discord).
/// <see cref="Kind"/> names the answer for the log.
/// </summary>
public readonly record struct McTalk(string[] Pool, string Kind, string A = "", string B = "", bool Remark = false,
    bool Fallback = false);

/// <summary>
/// Reads chat that names her (or comes right after she spoke with that player) the way Discord does
/// (<c>ChatterService</c>, <c>docs/agents/personality.md</c>): an « Inabot » or a shutdown threat first, then a mean
/// reading, then a specific intent with Papa's and Tata's twins, Papa's pet names, a kind word, a greeting, news, a
/// question, Tata's warmth, and at last a comeback. What only Discord has is left out: her records, a ship, a blame, the
/// pointers to slash commands (a favour she can't do gets <c>McCantDoLines</c> instead). Pure but for the dice.
/// </summary>
public static class McTalkReader
{
    // As on Discord (ChatterService): Tata's warm pool instead of the rest, the interested answer to news, and her real
    // temperature when asked how she is.
    private const double TataWarmthChance = 0.6;
    private const double SharingInterestChance = 0.5;
    private const double TemperatureChance = 0.5;

    // Interjections around a bare call: « hé syncs ? », « oh syncs ».
    private static readonly HashSet<string> CallFillers = new() { "he", "hey", "eh", "oh", "ho", "yo", "oye", "dis", "psst", "allo", "hep" };

    /// <param name="text">The chat message, her in-game name already turned into « syncs » by the caller.</param>
    /// <param name="kin">Who's speaking to her.</param>
    /// <param name="call">McCommand read it as a call (her name at one end, little else): said to her.</param>
    /// <param name="listening">She's just spoken with this player and the message doesn't name her: said to her only if
    /// it says something specific, since they may be talking to someone else.</param>
    /// <param name="whispered">A whisper: said to her, named or not.</param>
    /// <param name="askerId">Who « moi » is in an opinion (/rate's rules).</param>
    /// <param name="milli">The Pi's temperature, for « ça va ? »; null when unknown.</param>
    /// <returns>Null: nothing to say (a bare call gets « oui ? » from the caller; an unnamed message, silence).</returns>
    public static McTalk? Read(string text, McKin kin, bool call, bool listening, bool whispered, ulong askerId, int? milli,
        ResponsePicker picker, Random random)
    {
        var papa = kin == McKin.Papa;
        var petName = papa ? MessageCues.ReadPetName(text) : PetName.None;
        var named = MessageCues.NamesHer(text) || petName != PetName.None;
        if (!named && !listening && !whispered) return null;
        if (named && IsBareCall(text)) return null;

        var addressed = call || whispered || !named
                        || MessageCues.AddressesHer(text, 0)
                        || petName != PetName.None && MessageCues.AddressesHerByPetName(text, 0);
        var mood = MessageCues.Analyze(text);
        var mean = IsMean(mood, text);

        // Talked about, not to: her own initiative, rationed. Mean behind her back gets the not-to-my-face roast.
        if (!addressed)
        {
            if (MessageCues.ThreatensShutdownByName(text)) return Threat(kin);
            if (mood.Emotion == EmotionKind.Mean)
                return papa ? new(BotResponses.OwnerMeanReplies, "papa mean", Remark: true)
                    : new(BotResponses.BackbiteRoasts, "backbite", Remark: true);
            return new(papa ? BotResponses.TalkedAboutOwnerLines : BotResponses.TalkedAboutLines, "talked about", Remark: true);
        }

        if (MessageCues.IsMistakenIdentity(text)) return new(BotResponses.MistakenIdentityReplies, "inabot");
        if (MessageCues.ThreatensShutdown(text) || MessageCues.ThreatensShutdownByName(text)) return Threat(kin);

        // Right after speaking with her, without her name, only what is plainly for her: a specific intent, or Papa's
        // pet name. An insult or a « mdr » may be for someone else.
        var maybeForOthers = listening && !named && !whispered;
        var intent = mean ? ChatIntent.None : MessageCues.ReadIntent(text);
        if (mean && !maybeForOthers)
            return papa ? new(BotResponses.OwnerMeanReplies, "papa mean") : new(BotResponses.Comebacks, "comeback");
        if (Intent(intent, text, kin, askerId, milli, picker, random) is { } answer) return answer;
        if (PetNamePick(petName) is { } pet) return pet;
        if (maybeForOthers) return null;

        if (mood.Emotion == EmotionKind.Nice) return new(BotResponses.NiceReplies, "nice");
        if (mood.IsGreeting && !mean) return new(papa ? BotResponses.OwnerGreetings : BotResponses.Greetings, "greeting");
        if (intent == ChatIntent.Sharing)
            return new(papa ? BotResponses.OwnerIntentLines[ChatIntent.Sharing]
                : random.NextDouble() < SharingInterestChance ? BotResponses.IntentLines[ChatIntent.Sharing]
                : BotResponses.SharingShrugLines, "sharing");
        if (!papa && intent is ChatIntent.Question or ChatIntent.LowEffort)
            return new(BotResponses.IntentLines[intent], intent.ToString());
        if (kin == McKin.Tata && random.NextDouble() < TataWarmthChance) return new(BotResponses.TataReplies, "tata");
        // A call she didn't understand stays « oui ? »: the next message may be the order.
        if (call) return null;
        return papa ? new(BotResponses.OwnerComebacks, "papa comeback", Fallback: true)
            : new(BotResponses.Comebacks, "comeback", Fallback: true);
    }

    /// <summary>
    /// Who a player is to /rate's rules (« moi » in an opinion) and to the conversation streak: Papa and Tata by their
    /// Discord ids, anyone else by a hash of their name that stays the same across restarts, its top bit set so it is
    /// never a real snowflake.
    /// </summary>
    public static ulong AskerId(string player, McKin kin)
    {
        if (kin == McKin.Papa) return AvailabilityService.OwnerId;
        if (kin == McKin.Tata) return BotResponses.TataId;
        var hash = 14695981039346656037UL;
        foreach (var c in player.ToLowerInvariant())
            hash = (hash ^ c) * 1099511628211UL;
        return hash | (1UL << 63);
    }

    /// <summary>Whether her in-game name is one the chat cues already know (« SYNCS », « sync »).</summary>
    public static bool IsHerName(string name) => Rating.Normalize(name) is "syncs" or "sync";

    /// <summary>Her name and nothing else she could answer (« syncs », « hé syncs ? »): a call, for « oui ? ».</summary>
    public static bool IsBareCall(string text) =>
        Rating.Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .All(w => w is "syncs" or "sync" || CallFillers.Contains(w));

    // ChatterService.IsMean: « menteuse » aimed at her is teasing (Disbelief), and a bad day sworn at nobody isn't an insult.
    private static bool IsMean(MessageMood mood, string text)
    {
        if (mood.Emotion != EmotionKind.Mean) return false;
        var intent = MessageCues.ReadIntent(text);
        return intent != ChatIntent.Disbelief && !(intent == ChatIntent.Down && !MessageCues.SpeaksToSomeone(text));
    }

    private static McTalk Threat(McKin kin) => kin switch
    {
        McKin.Papa => new(BotResponses.ShutdownThreatOwner, "shutdown threat"),
        McKin.Tata => new(BotResponses.ShutdownThreatTata, "shutdown threat"),
        _ => new(BotResponses.ShutdownThreatReplies, "shutdown threat"),
    };

    private static McTalk? PetNamePick(PetName petName) => petName switch
    {
        PetName.Raspberry => new(BotResponses.OwnerRaspberryNameLines, "pet name"),
        PetName.Child => new(BotResponses.OwnerChildNameLines, "pet name"),
        _ => null,
    };

    // ChatterService.TryIntent, for the game: no ship, no blame (Discord people), no help list (the owner removed it in
    // game, so « aide-moi » is a call), and a favour she can't do is said so rather than pointed at a slash command.
    private static McTalk? Intent(ChatIntent intent, string text, McKin kin, ulong askerId, int? milli, ResponsePicker picker,
        Random random)
    {
        var kind = intent.ToString();
        switch (intent)
        {
            case ChatIntent.None or ChatIntent.Question or ChatIntent.Sharing or ChatIntent.LowEffort
                or ChatIntent.Ship or ChatIntent.Blame or ChatIntent.Help:
                return null;
            case ChatIntent.Opinion:
                return OpinionOf(text, askerId, picker);
            case ChatIntent.ShouldI:
                var yes = random.Next(2) == 0;
                if (MessageCues.ReadChoice(text) is { } choice)
                {
                    var (chosen, rejected) = yes ? (choice.A, choice.B) : (choice.B, choice.A);
                    return new(BotResponses.ChoiceLines, kind, chosen, rejected);
                }
                return new(yes ? BotResponses.YesLines : BotResponses.NoLines, kind);
            case ChatIntent.Joke:
                return new(BotResponses.IntentLines[ChatIntent.Joke], kind);
            case ChatIntent.FunFact:
                return new(BotResponses.MorningFunFacts, kind);
            case ChatIntent.Request:
                return new(BotResponses.McCantDoLines, kind);
            case ChatIntent.AboutHer when kin != McKin.Papa:
                var topic = MessageCues.ReadHerTopic(text) ?? HerTopic.Favourite;
                var days = topic == HerTopic.Age ? MorningGreeting.DaysOld(DateTimeOffset.UtcNow).ToString() : "";
                return new(BotResponses.AboutHerLines[topic], kind, days);
        }

        if (kin == McKin.Tata && BotResponses.TataIntentLines.TryGetValue(intent, out var tata)) return new(tata, kind);
        if (kin == McKin.Papa)
            return BotResponses.OwnerIntentLines.TryGetValue(intent, out var twin) ? new(twin, kind) : null;
        if (intent == ChatIntent.HowAreYou && milli is { } m && m < PiHealth.HotMilli && random.NextDouble() < TemperatureChance)
            return new(BotResponses.HowAreYouTempLines, kind, PiHealth.Celsius(m));
        return BotResponses.IntentLines.TryGetValue(intent, out var pool) ? new(pool, kind) : null;
    }

    // ChatterService.TryOpinion: /rate's verdict on what they asked about, or the side it ranks higher.
    private static McTalk? OpinionOf(string text, ulong askerId, ResponsePicker picker)
    {
        if (Opinion.Read(text) is not { } ask) return null;
        RatingResult Rate(string subject) =>
            Rating.Rate(subject, askerId, 0, AvailabilityService.OwnerId, BotResponses.TataId);
        if (ask.Other is { } other)
        {
            var aWins = Opinion.Prefers(Rate(ask.Subject), Rate(other), Rating.Key(ask.Subject, askerId),
                Rating.Key(other, askerId));
            var (chosen, rejected) = aWins ? (ask.Subject, other) : (other, ask.Subject);
            return new(BotResponses.PreferLines, "Opinion", chosen, rejected);
        }
        return new(BotResponses.OpinionLines, "Opinion", ask.Subject, McChat.Clean(RateModule.Verdict(Rate(ask.Subject), picker)));
    }
}
