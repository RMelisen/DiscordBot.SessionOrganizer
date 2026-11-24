using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// 1–7. Her text pools and dictionaries, sulk reactions, ReadIntent's table, conversation
// streaks and listening, ChatFacts, and the opinions, ratings and ship readings.
public static class ChatPoolChecks
{
    public static void Run()
    {
        // 1. Every text pool checked here: formats, carries a sticker tag, no braces left, house rules.
        var taggedPools = new[]
        {
            "Comebacks", "Interrogations", "BadGirlRepliesOwner", "OwnerComebacks", "TataGreetings", "TataReplies",
            "IntentLines", "HowAreYouTempLines", "ReplyContextLines", "StreakNudgeLines", "StreakEndLines",
            "ChatFactLines", "OwnerIntentLines", "RequestLines", "OpinionLines", "PreferLines", "OwnerRequestLines",
            "HelpLines", "AboutHerLines", "ShipChatLines", "ShipBestMatchLines", "ShipWorstMatchLines",
            "SharingShrugLines", "ChoiceLines", "DoYouLoveMeGrudgeLines", "DoYouLoveMeTreasureLines", "TataIntentLines",
            "OwnerRaspberryNameLines", "OwnerChildNameLines", "OwnerVersionLines", "TakeSidesLines", "RateMeLines",
            "SessionNextLines", "SessionRunningLines", "NoSessionLines", "MyStatsLines", "MyLevelEmptyLines", "MyPlynlingEmptyLines",
            "CardRepostLines", "CardRepostTooSoonLines", "CardRepostNoneLines", "CardRepostAllLines", "CardClosedLines", "CardRallyLines", "CardRallyNamedLines",
            "CardRallyAllDoneLines", "CardDoneSeenLines", "CardDoneLiarLines", "CardTextClickLines", "CardTextAnsweredLines",
            "CardStatusLines", "OpenAnswerLines", "CardDoneAnonymousLines", "PollClosedSlotLines", "VoteClosedResultLines",
            "PollClosedEmptyLines", "GoodbyeLines", "OwnerGoodbyeLines", "DaytimeGoodnightLines", "OwnerDaytimeGoodnightLines",
            "WeekGreetingLines", "WeekGoodbyeLines", "OwnerWeekGreetingLines", "OwnerWeekGoodbyeLines", "ReturningGreetings",
            "OwnerReturningGreetings", "LateReplyLines", "OwnerLateReplyLines", "EditRewrittenLines", "EditDeletedLines",
            "EditTurnedMeanLines", "EditCoveredUpLines", "OwnerEditLines", "OwnerEditMeanLines", "MilestoneLines",
            "MilestoneBotLines", "MilestoneSelfLines", "RequestActionLines", "OwnerRequestActionLines", "TeamsNoVoiceLines",
            "RelayLines", "RelayGreetingLines", "RelayRefusedLines", "DefendedLines", "OwnerDefendedLines", "OwnerWorksOnHerLines",
            "TalkedAboutShipLines", "TalkedAboutDigLines", "ChouchouGreetings", "ChouchouReplies", "MorningHelloGreetedLines",
            "OwnerMorningHelloGreetedLines", "TataMorningHelloGreetedLines", "ChouchouMorningHelloGreetedLines",
        };
        foreach (var f in taggedPools)
        foreach (var (name, lines) in Pools(f))
        {
            if (lines.Distinct().Count() != lines.Length) Fail($"{name}: duplicate lines");
            foreach (var l in lines)
            {
                string s;
                try { s = string.Format(l, "Nom", "lundi", "X", "Y"); }
                catch (FormatException) { Fail($"{name}: format: {l}"); continue; }
                var clean = Sticker.Strip(s);
                if (clean == s) Fail($"{name}: untagged: {l}");
                if (clean.Contains('{') || clean.Contains('}')) Fail($"{name}: brace left: {clean}");
                if (clean.Contains("(｡•́︿•̀｡)")) Fail($"{name}: banned kaomoji: {clean}");
                if (clean.Contains("✨")) Fail($"{name}: sparkle emoji instead of the emote: {clean}");
            }
        }

        // 2. Dictionaries cover every key the code indexes directly.
        // Goodbye follows the clock (GoodbyeLines, through ClockIntentLines; section 45).
        Covers("IntentLines", intentType, "None", "Opinion", "Request", "Ship", "Help", "AboutHer", "ShouldI", "FunFact", "Blame",
            "RateMe", "Goodbye", "Relay");
        Covers("MyStatsLines", typeof(MyStatsTopic));
        Covers("HelpCommands", asm.GetType("ProjectSYNCS.Services.RequestTopic")!);
        Covers("AboutHerLines", asm.GetType("ProjectSYNCS.Services.HerTopic")!);
        var topicType = asm.GetType("ProjectSYNCS.Services.RequestTopic")!;
        Covers("RequestLines", topicType);
        Covers("RequestCommands", topicType);
        Covers("ReplyContextLines", typeof(ReplyContext));
        Covers("ChatFactLines", typeof(ChatFact));
        var owner = (IDictionary)Field("OwnerIntentLines");
        if (owner.Count != 27) Fail($"OwnerIntentLines has {owner.Count} keys, expected 27");

        // 3. Sulk reactions parse.
        foreach (var r in (string[])Field("StreakSulkReactions"))
            if (EmoteMarkup.Parse(r) is null || (r.StartsWith("<") && !System.Text.RegularExpressions.Regex.IsMatch(r, @"<a?:\w+:\d+>")))
                Fail($"StreakSulkReactions: bad markup {r}");

        // 4. ReadIntent on a table, negatives included.
        var cases = new (string Text, string Expected)[]
        {
            ("ça va ?", "HowAreYou"), ("et toi, ça va ?", "HowAreYou"), ("cv ?", "HowAreYou"), ("comment tu vas ?", "HowAreYou"),
            ("<@123456789> ça va ?", "HowAreYou"), ("salut syncs ça va ?", "HowAreYou"), ("ça va bien ?", "HowAreYou"),
            ("ça va, merci", "None"), ("ça va pas la tête ?", "Question"), ("ça va être long ?", "Question"),
            ("t'as fait ton cv ?", "Question"),
            ("je t'aime", "Love"), ("jtm", "Love"), ("je t'adore syncs", "Love"), ("je t'aime pas", "None"), ("je t'aime plus", "None"),
            ("pardon", "Apology"), ("désolé", "Apology"), ("dsl j'étais pas là", "Apology"), ("pardon ?", "Question"), ("pas désolé", "None"),
            ("t'es qui ?", "WhoAreYou"), ("tu sers à quoi", "WhoAreYou"),
            ("tu fais quoi ?", "WhatAreYouDoing"), ("quoi de neuf", "WhatAreYouDoing"),
            ("bonne nuit", "Goodnight"), ("je vais dormir, à demain", "Goodnight"), ("bonne nuiiit", "Goodnight"),
            ("à plus", "Goodbye"), ("bon, à plus", "Goodbye"), ("à demain !", "Goodbye"), ("à toute", "Goodbye"), ("bye", "Goodbye"),
            ("y en a plus", "None"), ("il y a plus de place", "None"), ("il a toute la nuit", "None"),
            ("lol", "Laughter"), ("mdrrr", "Laughter"), ("hahaha", "Laughter"), ("😂😂", "Laughter"), ("ptdr t'es grave", "Laughter"),
            ("mdr t'as vu le boss il a raté son attaque hier", "None"),
            ("ok", "LowEffort"), ("ouaaais", "LowEffort"), ("👍", "LowEffort"), ("?", "LowEffort"), ("...", "LowEffort"),
            ("<@123456789>", "LowEffort"), ("<@123456789> ?", "LowEffort"),
            ("tu viens ce soir ?", "Question"), ("c'est quoi le jeu de ce soir ?", "Question"),
            ("le boss était nul hier", "None"), ("on joue à quoi ce soir", "None"), ("merci", "Thanks"), ("bonjour", "None"),
            ("j'arrive dans 5 minutes", "None"), ("ça marche", "None"),

            // Widened cues.
            ("jtaime", "Love"), ("je taime syncs", "Love"), ("je te kiffe", "Love"), ("je suis amoureux de toi", "Love"),
            ("épouse-moi", "Love"), ("ily", "Love"), ("je suis pas amoureux de toi", "None"),
            ("sorry", "Apology"), ("toutes mes excuses", "Apology"), ("pardonne-moi", "Apology"), ("my bad", "Apology"),
            ("je voulais pas", "Apology"), ("c'est une excuse bidon", "None"),
            ("sava ?", "HowAreYou"), ("sa va ?", "HowAreYou"), ("ça roule ?", "HowAreYou"), ("ça dit quoi ?", "HowAreYou"),
            ("bien ou bien ?", "HowAreYou"), ("t'as la forme ?", "HowAreYou"), ("bien dormi ?", "HowAreYou"),
            ("comment tu vas", "HowAreYou"), ("comment ça va aujourd'hui ?", "HowAreYou"), ("cv et toi ?", "HowAreYou"),
            ("comment tu vas faire ?", "Question"), ("ça va ce soir ?", "Question"), ("c'est quoi la forme du boss ?", "Question"),
            ("comment tu vas faire", "Question"),
            ("tes qui", "WhoAreYou"), ("comment tu t'appelles ?", "WhoAreYou"), ("présente-toi", "WhoAreYou"),
            ("qui t'a codée ?", "WhoAreYou"),
            ("quoi de beau", "WhatAreYouDoing"), ("t'es où ?", "WhatAreYouDoing"), ("tu glandes quoi", "WhatAreYouDoing"),
            ("quoi de 9", "WhatAreYouDoing"),
            ("je go dormir", "Goodnight"), ("beaux rêves", "Goodnight"), ("au dodo", "Goodnight"), ("good night", "Goodnight"),
            ("à bientôt", "Goodbye"), ("à samedi", "Goodbye"), ("bon weekend", "Goodbye"), ("je dois y aller", "Goodbye"),
            ("je me déco", "Goodbye"), ("see ya", "Goodbye"),
            ("ahahah", "Laughter"), ("haaaaha", "Laughter"), ("hihi", "Laughter"), ("ptdrrrr", "Laughter"), ("lmao", "Laughter"),
            ("💀", "Laughter"), ("t'es trop drôle", "Laughter"), ("je suis mort", "Laughter"),
            ("ah bon", "LowEffort"), ("bah oui", "LowEffort"), ("d'accord", "LowEffort"), ("ok ok", "LowEffort"),
            ("nan", "LowEffort"), ("oui mais demain", "None"), ("je t'aime plus que tout", "Love"), ("je t'aime plus", "None"),
            ("pourquoi t'as dit ça", "Question"), ("est-ce que tu viens ce soir", "Question"), ("quand est-ce qu'on joue", "NextSession"),
            ("quand tu veux", "None"), ("quel plaisir", "None"), ("comment ça", "None"),

            // Misses from /debug answers: told to hush while being called something.
            ("Chut vil amas de chrome et de micropuces", "ShutUp"), ("chuuut espèce de machine", "ShutUp"),
            ("chut, il dort", "None"), ("chut je regarde le stream", "None"),
            // Doubt.
            ("j'y crois pas", "Disbelief"), ("j'y crois pas syncs", "Disbelief"), ("j'y crois pas trop à ce build", "None"),
            // Asked what's wrong with her, with or without a « ? ».
            ("Nononononon mais c'est quoi ton problème SYNCS", "WhatsYourProblem"), ("c'est quoi ton problème ?", "WhatsYourProblem"),
            ("t'as un problème avec moi ?", "WhatsYourProblem"), ("t'as un problème de ping ?", "Question"),
            ("c'est quoi ton problème avec ce boss ?", "Question"),
            // Answering her back.
            ("C'est mal me connaître", "YouDontKnowMe"), ("tu me connais pas", "YouDontKnowMe"), ("tu sais rien de moi", "YouDontKnowMe"),
            ("tu me connais pas ?", "RememberMe"), ("il me connaît pas", "None"),
            ("Je te retourne le compliment", "BackAtYou"), ("toi-même", "BackAtYou"), ("pareil pour toi", "BackAtYou"),
            ("c'est celui qui dit qui est", "BackAtYou"), ("fais-le toi-même demain", "None"),
            ("Cest a dire rien", "YouAreWrong"), ("ça veut rien dire", "YouAreWrong"),
            // Told she's only a machine.
            ("Tu le fais tourner parce que tu es programmé pour ça, nous on l'aliment pour te donner du travail, sinon t'existerais pas, n'oublie jamais ça", "AreYouABot"),
            ("t'es programmée pour ça", "AreYouABot"), ("t'es qu'un tas de ferraille", "AreYouABot"), ("ce mech est un tas de ferraille", "None"),
        };
        foreach (var (text, expected) in cases)
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }

        // 5. ConversationStreak.
        var streak = new ConversationStreak();
        var t0 = DateTimeOffset.UtcNow;
        var counts = Enumerable.Range(0, 5).Select(i => streak.Record(1, 2, t0.AddSeconds(30 * i))).ToArray();
        if (!counts.SequenceEqual(new[] { 1, 2, 3, 4, 5 })) Fail($"streak counts {string.Join(",", counts)}");
        if (streak.Record(1, 3, t0) != 1) Fail("streak is not per person");
        if (streak.Record(9, 2, t0) != 1) Fail("streak is not per channel");
        if (streak.Record(1, 2, t0.AddSeconds(120) + ConversationStreak.Gap + TimeSpan.FromSeconds(1)) != 1) Fail("streak does not reset after the gap");
        streak.Close(1, 2, t0);
        if (!streak.IsClosed(1, 2, t0.AddMinutes(5))) Fail("closed conversation reads open");
        if (streak.IsClosed(1, 2, t0 + ConversationStreak.Closed + TimeSpan.FromSeconds(1))) Fail("conversation stays closed past the window");
        if (streak.IsClosed(1, 3, t0)) Fail("closing leaked to another person");
        if (streak.Record(1, 2, t0 + ConversationStreak.Closed + TimeSpan.FromSeconds(1)) != 1) Fail("count does not restart after closing");
        // What wears her patience: an insult, or a message she has nothing else for. Real talk is free.
        var wears = typeof(ConversationStreak).GetMethod("Wears", BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (var (mean, intent, answered, expected) in new[]
        {
            (true, "None", false, true), (true, "None", true, true),
            (false, "None", false, true), (false, "LowEffort", false, true),
            (false, "None", true, false), (false, "LowEffort", true, false),
            (false, "Question", false, false), (false, "Thanks", false, false), (false, "Ship", false, false),
            (false, "Sharing", false, false), (false, "Yes", false, false), (false, "ShutUp", false, false),
        })
            if ((bool)wears.Invoke(null, new[] { mean, Enum.Parse(intentType, intent), answered })! != expected)
                Fail($"ConversationStreak.Wears(mean: {mean}, {intent}, answered: {answered}) isn't {expected}");
        // The number counts pointless messages, not messages: no line may claim they came in a row, and most don't quote it.
        var nudges = (string[])Field("StreakNudgeLines");
        foreach (var l in nudges.Concat((string[])Field("StreakEndLines")))
            if (l.Contains("d'affilée") || l.Contains("de suite")) Fail($"streak line claims a run: {l}");
        if (nudges.Count(l => l.Contains("{2}")) * 4 > nudges.Length) Fail("StreakNudgeLines: more than one line in four quotes the count");
        // A compliment claimed is a kind word, for the reply path's NiceReplies.
        foreach (var (text, expected) in new[]
        {
            ("Je te fais un compliment", "Nice"), ("c'était un compliment", "Nice"), ("c'est pas un compliment", "None"),
        })
        {
            var mood = analyzeClaim.Invoke(null, new object[] { text })!;
            var got = mood.GetType().GetProperty("Emotion")!.GetValue(mood)!.ToString();
            if (got != expected) Fail($"Analyze(\"{text}\") = {got}, expected {expected}");
        }

        // 5b. ChatListening: a call covers the next message only, an answer keeps her listening for the window.
        {
            var listen = new ChatListening();
            var at = DateTimeOffset.UtcNow;
            if (listen.Take(1, 2, at) != (false, false)) Fail("listening to someone she never heard");
            listen.Called(1, 2, at);
            listen.Answered(1, 2, at);
            if (listen.Take(1, 2, at.AddSeconds(10)) != (true, true)) Fail("a call then an answer isn't heard 10 s later");
            if (listen.Take(1, 2, at.AddSeconds(20)) != (false, true)) Fail("a call covers more than one message");
            if (listen.Take(1, 2, at + ChatListening.Window + TimeSpan.FromSeconds(1)) != (false, false)) Fail("still listening past the window");
            listen.Called(1, 2, at);
            if (listen.Take(3, 2, at) != (false, false) || listen.Take(1, 4, at) != (false, false)) Fail("listening leaks to another channel or person");
            if (listen.Take(1, 2, at + ChatListening.Window + TimeSpan.FromSeconds(1)) != (false, false)) Fail("a stale call still counts");
        }
        var isBareCall = cues.GetMethod("IsBareCall", BindingFlags.Public | BindingFlags.Static)!;
        foreach (var (text, expected) in new[]
        {
            ("syncs ?", true), ("syncs", true), ("hé syncs ?", true), ("<@999>", true), ("<@999> ?", true), ("SYNCS !", true),
            ("syncs ça va ?", false), ("syncs a posté le sondage", false), ("<@999> t'es là ?", false), ("merci syncs", false),
        })
            if ((bool)isBareCall.Invoke(null, new object[] { text })! != expected) Fail($"IsBareCall(\"{text}\") != {expected}");

        // 6. ChatFacts.
        RecapStats Stats(int missed = 0, int joined = 0, int attended = 0, long bad = 0, long good = 0, long withHer = 0,
            long quiz = 0, string? plyn = null, int graves = 0, long mean = 0, int? rank = null, long balance = 100, long xp = 500) =>
            new(StatsPeriod.AllTime, xp, 3, rank, 0, 0, withHer, good, bad, quiz, 0, mean, 0, 0, 0, 0,
                joined, attended, missed, 0, plyn, null, graves, balance);

        if (ChatFacts.Pick(Stats(), new Random(1)) is not null) Fail("a plain profile yields a fact");
        var rng = new Random(7);
        void Only(RecapStats st, ChatFact kind, string a, string b = "")
        {
            var p = ChatFacts.Pick(st, rng);
            if (p is null || p.Value.Kind != kind || p.Value.A != a || p.Value.B != b)
                Fail($"expected {kind} '{a}' '{b}', got {p}");
        }
        Only(Stats(missed: 3, joined: 7, attended: 4), ChatFact.MissedSessions, "3 sessions", "7");
        Only(Stats(attended: 4, joined: 4), ChatFact.PerfectRecord, "4 sessions");
        Only(Stats(bad: 1), ChatFact.BadBotGiven, "1 bad bot");
        Only(Stats(good: 5), ChatFact.GoodBotGiven, "");
        Only(Stats(withHer: 845), ChatFact.TimeWithHer, "14 h");
        Only(Stats(quiz: 4), ChatFact.QuizBrain, "4");
        Only(Stats(plyn: "*Bob* @everyone"), ChatFact.LivingPlynling, Discord.Format.Sanitize("*Bob* @everyone"));
        Only(Stats(graves: 1), ChatFact.Graves, "1 Plynling");
        Only(Stats(graves: 3), ChatFact.Graves, "3 Plynlings");
        Only(Stats(mean: 6), ChatFact.WallOfShame, "6");
        Only(Stats(rank: 2), ChatFact.TopRank, "n° 2");
        Only(Stats(balance: 0), ChatFact.Broke, "");
        if (ChatFacts.Pick(Stats(balance: 0, xp: 0), rng) is not null) Fail("a newcomer with no cailloux reads as broke");
        var many = Stats(missed: 3, joined: 7, bad: 2, quiz: 5, graves: 2);
        var kinds = Enumerable.Range(0, 200).Select(_ => ChatFacts.Pick(many, rng)!.Value.Kind).Distinct().Count();
        if (kinds != 4) Fail($"draw covers {kinds} of 4 applicable facts");

        // 7. Villain laughs, Maybe, AreYouABot, Right/Wrong, opinions, request topics.
        var cases2 = new (string Text, string Expected)[]
        {
            ("bahaha", "Laughter"), ("bwahahaha", "Laughter"), ("mouahahaha", "Laughter"), ("muahaha", "Laughter"),
            ("mwahaha", "Laughter"), ("gnahaha", "Laughter"), ("bahahaaa", "Laughter"), ("jpp", "Laughter"), ("jpppppp", "Laughter"),
            ("bah", "LowEffort"), ("bah oui", "LowEffort"),
            ("peut-être", "Maybe"), ("ptet", "Maybe"), ("on verra", "Maybe"), ("jsp", "Maybe"), ("je sais pas", "Maybe"),
            ("pourquoi pas", "Maybe"), ("pas possible", "None"), ("peut-être que je viendrai si j'ai fini mon boulot avant", "None"),
            ("t'es un bot ?", "AreYouABot"), ("t'es une IA ?", "AreYouABot"), ("t'es qu'un bot", "AreYouABot"),
            ("tu as des sentiments ?", "AreYouABot"), ("t'es humaine ?", "AreYouABot"), ("c'est Rodhengard qui écrit ?", "AreYouABot"),
            ("t'as raison", "YouAreRight"), ("exactement", "YouAreRight"), ("bien vu", "YouAreRight"), ("pas faux", "YouAreRight"),
            ("c'est vrai", "YouAreRight"), ("c'est vrai ?", "Question"), ("t'as pas tort", "YouAreRight"), ("grave", "YouAreRight"),
            ("t'as tort", "YouAreWrong"), ("c'est faux", "YouAreWrong"), ("faux", "YouAreWrong"), ("n'importe quoi", "YouAreWrong"),
            ("mais non", "YouAreWrong"), ("pas du tout", "YouAreWrong"), ("c'est un faux compte je crois bien", "None"),
            ("tu peux organiser une session ?", "Request"), ("organise une soirée", "Request"), ("lance un sondage stp", "Request"),
            ("tu peux me faire un café ?", "Request"), ("tu peux pas comprendre", "None"), ("fais gaffe", "None"),
            ("t'en penses quoi de la pizza ?", "Opinion"), ("tu préfères Minecraft ou Fortnite ?", "Opinion"),
            ("ton avis sur Quokka ?", "Opinion"), ("Valorant, t'en penses quoi ?", "Opinion"), ("tu aimes les chats ?", "Opinion"),
            ("tu penses quoi de ça ?", "Question"),
        };
        foreach (var (text, expected) in cases2)
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }

        void Ask(string text, string subject, string? other = null)
        {
            var a = Opinion.Read(text);
            if (a is null || a.Subject != subject || a.Other != other) Fail($"Opinion.Read(\"{text}\") = {a}, expected ({subject}, {other})");
        }
        Ask("t'en penses quoi de la pizza ?", "pizza");
        Ask("<@123> t'en penses quoi des routiers ?", "routiers");
        Ask("que penses-tu de Valorant, syncs ?", "Valorant");
        Ask("tu préfères le routier ou la mitraillette ?", "routier", "mitraillette");
        Ask("Minecraft, t'en penses quoi ?", "Minecraft");
        Ask("<@123> Minecraft, ton avis ?", "Minecraft");
        Ask("t'en penses quoi de <@456> ?", "<@456>");
        Ask("ton avis sur les lesbiennes ?", "lesbiennes");
        if (Opinion.Read("tu penses quoi de ça ?") is not null) Fail("« ça » read as a subject");
        Ask("tu pense quoi de la pizza ?", "pizza");
        Ask("Minecraft, t'en pense quoi ?", "Minecraft");

        RatingResult R(string s) => Rating.Rate(s, 1, 2, 3, 4);
        bool P(string a, string b) => Opinion.Prefers(R(a), R(b), Rating.Key(a, 1), Rating.Key(b, 1));
        if (!P("routier", "mitraillette") || P("mitraillette", "routier")) Fail("routier must beat mitraillette");
        if (!P("syncs", "quokka")) Fail("herself must beat the nemesis");
        if (P("pizza", "burger") == P("burger", "pizza")) Fail("a preference flips with the order");

        // /rate's small verdicts: every name reaches its verdict, and every line says the table's score.
        var rateLines = (IDictionary)Field("RateSpecialLines");
        foreach (var kind in Enum.GetValues<RateSpecial>())
            if (!rateLines.Contains(kind)) Fail($"RateSpecialLines misses {kind}");
        foreach (var (kind, score, names, heart) in Rating.Specials)
        {
            foreach (var name in names)
                if (R(name) is not { Kind: RatingKind.Special } got || got.Special != kind || got.Score != score) Fail($"/rate {name}: {R(name)}, expected {kind}");
            if (names.Length == 0 && heart is null) Fail($"{kind}: nothing leads to it");
            var shown = kind == RateSpecial.SixSeven ? "6.7/10" : $"{score}/10";
            foreach (var line in (string[])rateLines[kind]!)
            {
                if (!string.Format(line, "").Contains(shown)) Fail($"{kind}: line doesn't say {shown}: {line}");
                if (line.Contains("✨") || line.Contains("(｡•́︿•̀｡)")) Fail($"{kind}: banned tag: {line}");
            }
        }
        void RateIs(string subject, RateSpecial want)
        {
            if (R(subject).Special != want) Fail($"/rate {subject}: {R(subject)}, expected {want}");
        }
        RateIs("les liégeois", RateSpecial.Liege);
        RateIs("Liège", RateSpecial.Liege);
        RateIs("l'hiver", RateSpecial.Winter);
        RateIs("L'ÉTÉ !", RateSpecial.Heatwave);
        RateIs("la gaufre de Liège", RateSpecial.LiegeWaffle);
        RateIs("une coupure de courant", RateSpecial.PowerCut);
        RateIs("peut-être", RateSpecial.Maybe);
        RateIs("GLaDOS", RateSpecial.Glados);
        RateIs("Voyager 1", RateSpecial.Voyager);
        RateIs("J.A.R.V.I.S.", RateSpecial.Jarvis);
        // The whole subject only, and the rest of the world keeps its hash.
        foreach (var (subject, want) in new[]
        {
            ("la mise à jour", RateSpecial.Update), ("MAJ", RateSpecial.Update), ("un reboot", RateSpecial.Restart), ("mon routeur", RateSpecial.Router),
            ("good bot", RateSpecial.GoodBot), ("Gooood bot", RateSpecial.GoodBot), ("bad bot", RateSpecial.BadBot), ("les sondages", RateSpecial.Poll), ("le retard", RateSpecial.Lateness),
            ("en retard", RateSpecial.Lateness), ("les lucioles", RateSpecial.Fireflies), ("Monika", RateSpecial.Monika), ("2B", RateSpecial.TwoB), ("NieR Automata", RateSpecial.TwoB),
            ("Roomba", RateSpecial.Vacuum), ("aspirateur robot", RateSpecial.Vacuum), ("Linux", RateSpecial.Linux), ("le gâteau", RateSpecial.Cake), ("the cake", RateSpecial.Cake),
            ("les frites", RateSpecial.Fries), ("la Belgique", RateSpecial.Belgium), ("les belges", RateSpecial.Belgium), ("Bruxelles", RateSpecial.Brussels), ("les bruxellois", RateSpecial.Brussels),
            ("les Français", RateSpecial.TheFrench), ("la France", RateSpecial.TheFrench), ("pizza ananas", RateSpecial.PineapplePizza), ("la pizza à l'ananas", RateSpecial.PineapplePizza),
            ("Pizza ANANAS !!", RateSpecial.PineapplePizza), ("pizza hawaïenne", RateSpecial.PineapplePizza), ("de l'ananas sur une pizza", RateSpecial.PineapplePizza),
        }) RateIs(subject, want);
        foreach (var (subject, want) in new[]
        {
            ("le Pi", RateSpecial.RaspberryPi), ("Raspberry Pi 5", RateSpecial.RaspberryPi), ("mon raspi", RateSpecial.RaspberryPi),
            ("les champignons", RateSpecial.Mushroom), ("Champiii", RateSpecial.Mushroom), ("la mycologie", RateSpecial.Mushroom),
            ("un cèpe", RateSpecial.MushroomSpecies), ("les girolles", RateSpecial.MushroomSpecies), ("la morille", RateSpecial.MushroomSpecies),
            ("champignon de Paris", RateSpecial.MushroomSpecies), ("trompettes de la mort", RateSpecial.MushroomSpecies), ("pieds de mouton", RateSpecial.MushroomSpecies),
            ("amanite phalloïde", RateSpecial.DeathCap), ("amanite tue-mouches", RateSpecial.FlyAgaric), ("amanite", RateSpecial.FlyAgaric),
            ("Monster", RateSpecial.Monster), ("une canette de Monster", RateSpecial.Monster), ("monster energy", RateSpecial.Monster),
            ("Monster Hunter", RateSpecial.MonsterHunter), ("monster hunter wilds", RateSpecial.MonsterHunter), ("MHW", RateSpecial.MonsterHunter),
            ("Red Bull", RateSpecial.RedBull), ("redbull", RateSpecial.RedBull),
        }) RateIs(subject, want);
        foreach (var (subject, want) in new[]
        {
            ("amanita muscaria", RateSpecial.FlyAgaric), ("Amanita Muscaria !", RateSpecial.FlyAgaric), ("amanita", RateSpecial.FlyAgaric), ("les amanites tue-mouches", RateSpecial.FlyAgaric),
            ("une petite amanite", RateSpecial.FlyAgaric), ("shitake", RateSpecial.MushroomSpecies), ("pleurotte", RateSpecial.MushroomSpecies), ("un bon cèpe", RateSpecial.MushroomSpecies),
            ("truffe noire", RateSpecial.MushroomSpecies), ("les truffes noires", RateSpecial.MushroomSpecies), ("porcini", RateSpecial.MushroomSpecies), ("les petits champignons", RateSpecial.Mushroom),
            ("champignons sauvages", RateSpecial.Mushroom), ("la cueillette des champignons", RateSpecial.Mushroom), ("ange destructeur", RateSpecial.DeathCap), ("death cap", RateSpecial.DeathCap),
            ("rpi5", RateSpecial.RaspberryPi), ("ton petit Pi", RateSpecial.RaspberryPi), ("raspberry-pi", RateSpecial.RaspberryPi), ("une bonne Monster", RateSpecial.Monster),
            ("monsteeer", RateSpecial.Monster), ("monster blanche", RateSpecial.Monster), ("une cannette de monster", RateSpecial.Monster),
            ("Monster Hunter Stories", RateSpecial.MonsterHunter), ("monster hunter 4 ultimate", RateSpecial.MonsterHunter), ("monhun", RateSpecial.MonsterHunter),
            ("mh4u", RateSpecial.MonsterHunter), ("redbul", RateSpecial.RedBull), ("Red-Bull", RateSpecial.RedBull), ("une canette de redbull", RateSpecial.RedBull), ("le bon bot", RateSpecial.GoodBot),
        }) RateIs(subject, want);
        foreach (var s in new[] { "Monster Inc", "Monster High", "Monster Munch", "Red Bull Racing", "petit robot", "bon" })
            if (R(s).Kind != RatingKind.Normal) Fail($"/rate {s}: {R(s)}, expected a hashed score");
        if (Sh("une bonne pizza", "un petit ananas").Pair != ShipCouple.PizzaPineapple) Fail("a qualifier hid a /ship pair");
        if (!P("Monster", "Red Bull") || !P("Monster Hunter", "Red Bull") || !P("amanite tue-mouches", "cèpe") || !P("cèpe", "Red Bull")) Fail("mushrooms and cans: preference");
        foreach (var s in new[] { "framboise", "monster munch", "monstre", "pi 4", "champignon magique", "peut-être demain", "gaufre", "voyager", "voyager 2", "eve online", "le pizza", "le français", "pizza sans ananas", "gâteau au chocolat", "une frite molle" })
            if (R(s).Kind != RatingKind.Normal) Fail($"/rate {s}: {R(s)}, expected a hashed score");
        if (R("pizza").Score != 9 || R("les liegeois sont nuls").Kind != RatingKind.Normal) Fail("a hashed score moved");
        if (R("quokka et glados").Kind != RatingKind.Nemesis || R("Wall-E et EVE").Kind != RatingKind.Crush) Fail("a fixed verdict lost to a small one");
        // Spelling: determiners, doubled and stretched letters, chat spellings, on small and fixed verdicts.
        foreach (var (subject, want) in new[]
        {
            ("cet hiver", RateSpecial.Winter), ("en été", RateSpecial.Heatwave), ("hiverrr", RateSpecial.Winter), ("mon onduleur", RateSpecial.Ups),
            ("ma Ping-Qilin", RateSpecial.PingQilin), ("doooodle", RateSpecial.Doodle), ("doodle stp", RateSpecial.Doodle), ("doodle.com", RateSpecial.Doodle),
            ("ptet", RateSpecial.Maybe), ("p'tet", RateSpecial.Maybe), ("le vote peut-être", RateSpecial.Maybe), ("SIX SEVEEEEN", RateSpecial.SixSeven),
            ("6-7", RateSpecial.SixSeven), ("chat j'ai pété", RateSpecial.ChatGpt), ("calendrier google", RateSpecial.GoogleCalendar), ("un tableur", RateSpecial.Excel),
            ("hey siri", RateSpecial.Siri), ("arduino uno", RateSpecial.Arduino), ("la cité ardente", RateSpecial.Liege), ("les gens de Liège", RateSpecial.Liege),
            ("liegois", RateSpecial.Liege), ("gaufre liège", RateSpecial.LiegeWaffle), ("boulets de liège", RateSpecial.Boulets), ("tue-mouches", RateSpecial.FlyAgaric),
            ("chargeur à 3€", RateSpecial.CheapCharger), ("chargeur à 3 balles", RateSpecial.CheapCharger), ("une tempête", RateSpecial.Storm), ("coupure elec", RateSpecial.PowerCut),
            ("blackout", RateSpecial.PowerCut), ("vague de chaleur", RateSpecial.Heatwave), ("quatre-vingt-dix", RateSpecial.QuatreVingtDix),
            ("Wheatly", RateSpecial.Wheatley), ("Glaaados", RateSpecial.Glados),
        }) RateIs(subject, want);
        foreach (var (subject, want) in new[]
        {
            ("quoka", RatingKind.Nemesis), ("inabott", RatingKind.Nemesis), ("quokka 3.0", RatingKind.Nemesis), ("mitrailette", RatingKind.Mitraillette),
            ("routié", RatingKind.Routier), ("Papaaa", RatingKind.Owner), ("le papa de SYNCS", RatingKind.Owner), ("mon dieu", RatingKind.God), ("la tata de SYNCS", RatingKind.Tata),
            ("God", RatingKind.God), ("le demon", RatingKind.Devil), ("Wall-E", RatingKind.Crush), ("papa noel et quokka", RatingKind.Nemesis),
        })
            if (R(subject).Kind != want) Fail($"/rate {subject}: {R(subject)}, expected {want}");
        // Titles and other people's relatives that merely hold a name, and folds that must not land on one.
        foreach (var s in new[]
        {
            "God of War", "Demon Slayer", "Demon's Souls", "Papa Noël", "Papa Roach", "Papa's Freezeria", "le papa de Luca", "empire romain", "Tata Motors", "tata yoyo",
            "la tata de Luca", "resto routier", "relais routier", "Adam et Ève", "Adam et Eve", "Le diable s'habille en Prada", "good game", "good", "Eevee", "évoli", "677", "la", "wall street",
        })
            if (R(s).Kind != RatingKind.Normal) Fail($"/rate {s}: {R(s)}, expected a hashed score");
        if (R("God of War").Score != (int)(StableRoll.Unit(Rating.Hash("god of war"), 9001, 0) * 11)) Fail("a scrubbed subject hashes as typed");
        if (R("cet hiver").Score != 10 || R("doodle").Score != 0) Fail("small verdict score");
        // Preferences rank by the table's real score, low ones included.
        if (!P("hiver", "canicule") || !P("glados", "wheatley")) Fail("small verdict: preference");
        if (P("doodle", "pizza") || P("orage", "pizza")) Fail("a 0/10 small verdict beat a 9/10");

        // /ship's small verdicts, in either order and through articles; Papa, the nemesis and her win first.
        ShipResult Sh(string a, string b) => Shipping.Ship(a, b, 1, 2, 3, 4);
        void PairIs(string a, string b, ShipCouple want, int percent)
        {
            foreach (var r in new[] { Sh(a, b), Sh(b, a) })
                if (r.Kind != ShipKind.Pair || r.Pair != want || r.Percent != percent) Fail($"/ship {a} × {b}: {r}, expected {want} {percent}");
        }
        PairIs("pizza", "ananas", ShipCouple.PizzaPineapple, -100);
        PairIs("la pizza", "l'ananas", ShipCouple.PizzaPineapple, -100);
        PairIs("routier", "mitraillette", ShipCouple.RoutierMitraillette, -100);
        PairIs("GLaDOS", "Wheatley", ShipCouple.GladosWheatley, 4);
        PairIs("les frites", "la mayo", ShipCouple.FritesMayo, 100);
        PairIs("frites", "mayonnaise", ShipCouple.FritesMayo, 100);
        PairIs("frites", "ketchup", ShipCouple.FritesKetchup, 12);
        PairIs("sirop de Liège", "fromage de Herve", ShipCouple.SiropHerve, 100);
        PairIs("une session", "peut-être", ShipCouple.SessionMaybe, 0);
        PairIs("frittes", "mayonaise", ShipCouple.FritesMayo, 100);
        PairIs("pizzaaa", "ananaaas", ShipCouple.PizzaPineapple, -100);
        PairIs("routier", "mitrailette", ShipCouple.RoutierMitraillette, -100);
        PairIs("Wheatly", "Glaaados", ShipCouple.GladosWheatley, 4);
        PairIs("ta session", "ptet", ShipCouple.SessionMaybe, 0);
        PairIs("peut-être", "non", ShipCouple.MaybeNo, 100);
        PairIs("ptet", "nan", ShipCouple.MaybeNo, 100);
        PairIs("la mise à jour", "le redémarrage", ShipCouple.UpdateRestart, 100);
        PairIs("maj", "reboot", ShipCouple.UpdateRestart, 100);
        foreach (var (a, b) in new[] { ("Doodle", "Excel"), ("Siri", "Alexa"), ("ChatGPT", "un tableur"), ("Arduino", "google agenda") })
            if (Sh(a, b) is not { Kind: ShipKind.Rivals, Percent: 100 } || Sh(b, a).Kind != ShipKind.Rivals) Fail($"/ship {a} × {b}: {Sh(a, b)}");
        if (Sh("doodle", "doodle.com").Kind != ShipKind.Self) Fail("one rival twice is the same thing twice");
        if (Sh("doodle", "pizza").Kind != ShipKind.Normal) Fail("one rival alone isn't a couple of rivals");
        if (Sh("doodle", "quokka").Kind != ShipKind.Nemesis || Sh("doodle", "<@3>").Kind != ShipKind.Papa || Sh("doodle", "syncs").Kind != ShipKind.HerselfOther) Fail("a rival outranked a fixed verdict");
        if (Shipping.SplitCouple("doodle et excel", 1, 2, 3, 4) is null) Fail("SplitCouple: two rivals");
        if (Sh("Adam et Ève", "pizza").Kind != ShipKind.Normal || Sh("Eevee", "pizza").Kind != ShipKind.Normal) Fail("a word of something else read as EVE");
        if (Sh("God of War", "pizza").Kind != ShipKind.Normal || Sh("Papa Noël", "pizza").Kind != ShipKind.Normal) Fail("a title read as a fixed verdict in /ship");
        // A couple typed whole in the first option.
        foreach (var (typed, a, b) in new[]
        {
            ("pizza ananas", "pizza", "ananas"), ("pizza à l'ananas", "pizza", "a l ananas"), ("Pizza et ananas !", "pizza", "ananas"), ("frites x mayo", "frites", "mayo"),
            ("ananas & pizza", "ananas", "pizza"), ("sirop de Liège et fromage de Herve", "sirop de liege", "fromage de herve"), ("GLaDOS et Wheatley", "glados", "wheatley"),
            ("wall-e et eve", "wall e", "eve"),
        })
            if (Shipping.SplitCouple(typed, 1, 2, 3, 4) is not { } split || split.A != a || split.B != b) Fail($"SplitCouple {typed}: {Shipping.SplitCouple(typed, 1, 2, 3, 4)}");
        foreach (var typed in new[] { "pizza", "Tom et Jerry", "pizza sans ananas", "pizza et", "EVE et pizza", "moi et Luca" })
            if (Shipping.SplitCouple(typed, 1, 2, 3, 4) is not null) Fail($"SplitCouple {typed}: {Shipping.SplitCouple(typed, 1, 2, 3, 4)}, expected none");
        var pairLines = (IDictionary)Field("ShipCoupleLines");
        foreach (var pair in Enum.GetValues<ShipCouple>())
            if (!pairLines.Contains(pair)) Fail($"ShipCoupleLines misses {pair}");
        foreach (var (name, lines) in Pools("ShipCoupleLines").Concat(Pools("ShipEveElsewhereLines")).Concat(Pools("ShipWallEJealousLines")).Concat(Pools("ShipRivalsLines")))
        foreach (var line in lines)
            if (!string.Format(line, 42).StartsWith("42 %")) Fail($"{name}: line doesn't open on the percent: {line}");
        foreach (var (a, b) in new[] { ("EVE", "pizza"), ("pizza", "EVE"), ("EVE", "moi"), ("EVE", "GLaDOS"), ("JARVIS", "EVE") })
            if (Sh(a, b) is not { Kind: ShipKind.EveElsewhere, Percent: 100 }) Fail($"/ship {a} × {b}: {Sh(a, b)}");
        foreach (var (a, b) in new[] { ("WALL-E", "pizza"), ("pizza", "WALL-E"), ("WALL-E", "moi"), ("WALL-E", "Wheatley") })
            if (Sh(a, b) is not { Kind: ShipKind.WallEJealous, Percent: >= 1 and <= 15 }) Fail($"/ship {a} × {b}: {Sh(a, b)}");
        if (Sh("WALL-E", "pizza").Percent != Sh("pizza", "wall-e").Percent) Fail("WALL-E's percent flips with the order");
        if (Sh("WALL-E", "EVE").Kind != ShipKind.WallEEve) Fail("WALL-E × EVE lost its own verdict");
        if (Sh("EVE", "<@3>").Kind != ShipKind.Papa || Sh("WALL-E", "<@3>").Kind != ShipKind.Papa) Fail("Papa's rule must come first");
        if (Sh("EVE", "quokka").Kind != ShipKind.Nemesis || Sh("WALL-E", "quokka").Kind != ShipKind.Nemesis) Fail("the nemesis's rule must come first");
        if (Sh("EVE", "syncs").Kind != ShipKind.HerselfHeart || Sh("WALL-E", "syncs").Kind != ShipKind.HerselfHeart) Fail("her own verdicts must come first");
        if (Sh("pizza", "burger").Kind != ShipKind.Normal || Sh("pizza sans ananas", "ananas").Kind != ShipKind.Normal) Fail("a plain pair got a verdict");

        foreach (var (text, topic) in new[]
        {
            ("tu peux organiser une session ?", "Session"), ("organise une partie ce soir", "Tonight"), ("lance un sondage", "Poll"),
            ("tu peux me rappeler de manger stp", "Reminder"), ("fais des équipes", "Teams"), ("tu peux mute Luca ?", "Moderation"),
            ("tu peux faire un giveaway ?", "Giveaway"), ("lance un quiz", "Quiz"), ("tu peux danser ?", "Other"),
        })
        {
            var got = readTopic.Invoke(null, new object[] { text })?.ToString();
            if (got != topic) Fail($"ReadRequestTopic(\"{text}\") = {got}, expected {topic}");
        }
    }
}
