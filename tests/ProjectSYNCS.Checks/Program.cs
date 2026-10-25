using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

// Checks for how SYNCS reads chat (MessageCues, Opinion, ShipAsk), the pools that answer it, and
// the listening pieces around them. No test framework on purpose: a console that prints only what
// fails plus a few summary lines, and exits 1 on any failure.
//
//     dotnet run --project tests/ProjectSYNCS.Checks
//
// Run it before shipping a change to the cues or the pools. Add a case for every misfire found in
// the wild, next to the section it belongs to. The internals are reached by reflection, so the
// bot needs no InternalsVisibleTo.
var asm = typeof(ChatFacts).Assembly;
var br = asm.GetType("ProjectSYNCS.Services.BotResponses")!;
var cues = asm.GetType("ProjectSYNCS.Services.MessageCues")!;
var intentType = asm.GetType("ProjectSYNCS.Services.ChatIntent")!;
var readIntent = cues.GetMethod("ReadIntent", BindingFlags.Public | BindingFlags.Static)!;
var failures = 0;
void Fail(string m) { failures++; Console.WriteLine("FAIL " + m); }
// The longest name a report can write out: 32 characters (Discord's cap), every one escaped by Sanitize.
string? LongestName(ulong id) => new string('*', 32);

object Field(string n) => br.GetField(n, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

IEnumerable<(string Name, string[] Lines)> Pools(string field)
{
    var v = Field(field);
    if (v is string[] arr) yield return (field, arr);
    else if (v is IDictionary d)
        foreach (DictionaryEntry e in d) yield return ($"{field}[{e.Key}]", (string[])e.Value!);
}

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
    "CardRepostLines", "CardRepostTooSoonLines", "CardClosedLines", "CardRallyLines", "CardRallyNamedLines",
    "CardRallyAllDoneLines", "CardDoneSeenLines", "CardDoneLiarLines", "CardTextClickLines", "CardTextAnsweredLines",
    "CardStatusLines",
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
void Covers(string field, Type enumType, params string[] except)
{
    var d = (IDictionary)Field(field);
    foreach (var v in Enum.GetValues(enumType))
        if (!except.Contains(v.ToString()) && !d.Contains(v)) Fail($"{field} misses {v}");
}
Covers("IntentLines", intentType, "None", "Opinion", "Request", "Ship", "Help", "AboutHer", "ShouldI", "FunFact", "Blame",
    "RateMe");
Covers("MyStatsLines", typeof(MyStatsTopic));
Covers("HelpCommands", asm.GetType("ProjectSYNCS.Services.RequestTopic")!);
Covers("AboutHerLines", asm.GetType("ProjectSYNCS.Services.HerTopic")!);
var topicType = asm.GetType("ProjectSYNCS.Services.RequestTopic")!;
Covers("RequestLines", topicType);
Covers("RequestCommands", topicType);
Covers("ReplyContextLines", typeof(ReplyContext));
Covers("ChatFactLines", typeof(ChatFact));
var owner = (IDictionary)Field("OwnerIntentLines");
if (owner.Count != 24) Fail($"OwnerIntentLines has {owner.Count} keys, expected 24");

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

var readTopic = cues.GetMethod("ReadRequestTopic", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) })!;
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

// 8. /debug answers: keys, the examples log, the embed caps.
var kindsType = typeof(AnswerKinds);
var consts = kindsType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral).Select(f => (string)f.GetValue(null)!).ToList();
var intentKey = kindsType.GetMethod("Intent", BindingFlags.NonPublic | BindingFlags.Static)!;
var allKinds = consts
    .Concat(Enum.GetValues(intentType).Cast<object>().Where(v => v.ToString() != "None").Select(v => (string)intentKey.Invoke(null, new[] { v })!))
    .Concat(Enum.GetValues<ReplyContext>().Select(AnswerKinds.Context))
    .Concat(Enum.GetValues<ChatFact>().Select(AnswerKinds.Fact))
    .Concat(Enum.GetValues(asm.GetType("ProjectSYNCS.Services.CardReplyIntent")!).Cast<object>().Where(v => v.ToString() != "None")
        .Select(v => (string)kindsType.GetMethod("Card", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new[] { v })!))
    .ToList();
if (allKinds.Distinct().Count() != allKinds.Count) Fail("duplicate answer kind keys");
if (!AnswerKinds.IsUnrecognised(AnswerKinds.Comeback) || AnswerKinds.IsUnrecognised(AnswerKinds.Nice)
    || !AnswerKinds.IsUnrecognised(AnswerKinds.Fact(ChatFact.Broke))) Fail("IsUnrecognised misreads");

if (UnansweredLog.Clean("message 149\n  ligne\t") != "message 149 ligne") Fail($"examples text not collapsed: '{UnansweredLog.Clean("message 149\n  ligne\t")}'");
if (UnansweredLog.Clean(new string('x', 500)).Length != UnansweredLog.MaxTextLength) Fail("long text not capped");
if (UnansweredLog.Clean(" \n\t ").Length != 0) Fail("blank text kept");
if (typeof(UnansweredMessage).GetProperties().Concat(typeof(ProjectSYNCS.Models.UnansweredExample).GetProperties())
    .Any(p => p.Name.Contains("User") || p.Name.Contains("Author"))) Fail("an example stores its author");
var examples = Enumerable.Range(0, 150).Reverse()
    .Select(i => new UnansweredMessage(t0.AddSeconds(i), AnswerKinds.Key(ChatPath.Reply), AnswerKinds.Comeback, UnansweredLog.Clean($"message {i}\nligne")))
    .ToList();

var cards = asm.GetType("ProjectSYNCS.Commands.AnswerStatsCards")!;
var buildTypes = cards.GetMethod("BuildTypes")!;
var buildExamples = cards.GetMethod("BuildExamples")!;
var synthetic = allKinds.SelectMany(k => Enum.GetValues<ChatPath>().Select(p => new ChatAnswerCount(AnswerKinds.Key(p), k, 1234))).ToList();
foreach (var period in Enum.GetValues<StatsPeriod>())
{
    var embed = (Discord.Embed)buildTypes.Invoke(null, new object[] { synthetic, period })!;
    if (embed.Length > 6000) Fail($"answers embed is {embed.Length} characters");
    if (embed.Fields.Any(f => f.Value.Length > 1024)) Fail("an answers field exceeds 1024");
    if (period == StatsPeriod.Week) Console.WriteLine($"answers embed: {embed.Length} chars, {embed.Fields.Length} fields");
}
var empty = (Discord.Embed)buildTypes.Invoke(null, new object[] { new List<ChatAnswerCount>(), StatsPeriod.Week })!;
if (empty.Fields.Length != 0) Fail("empty counts render fields");
var file = (string)buildExamples.Invoke(null, new object[] { examples, UnansweredLog.KeptDays })!;
if (!file.Contains("message 149 ligne") || !file.Contains("message 0 ligne")) Fail("examples file misses a line");

// 9. Widened cues, and the misfires they must not cause.
var cases3 = new (string Text, string Expected)[]
{
    // Love
    ("tu veux sortir avec moi ?", "Love"), ("jtd", "Love"), ("je t'apprécie beaucoup", "Love"), ("t'es mon crush", "Love"),
    ("luv u", "Love"), ("on se marie ?", "Love"), ("je t'apprécie pas", "None"),
    // Apology
    ("déso", "Apology"), ("oups", "Apology"), ("oups pardon", "Apology"), ("c'est ma faute", "Apology"),
    ("c'est pas ma faute", "None"), ("j'ai pas fait exprès", "Apology"), ("je voulais pas te vexer", "Apology"),
    ("je voulais pas venir ce soir de toute façon", "Sharing"), ("oups j'ai raté le boss encore une fois", "None"),
    // HowAreYou
    ("salut, ça va ? tu viens ce soir ?", "HowAreYou"), ("tranquille ?", "HowAreYou"), ("tranquille et toi ?", "HowAreYou"),
    ("bien et toi ?", "HowAreYou"), ("ça baigne ?", "HowAreYou"), ("tu vas mieux ?", "HowAreYou"),
    ("comment va la vie ?", "HowAreYou"), ("comment était ta journée ?", "HowAreYou"), ("how are you ?", "HowAreYou"),
    ("ça va depuis ?", "HowAreYou"), ("on fait une partie tranquille ?", "Question"),
    ("c'est quand ta journée de repos ?", "Question"), ("ça va pour demain ?", "Question"), ("comment va ton plynling ?", "Question"),
    // WhoAreYou / AreYouABot / WhatAreYouDoing
    ("c'est qui ton créateur ?", "WhoAreYou"), ("t'es quoi comme bot ?", "WhoAreYou"),
    ("es-tu une IA ?", "AreYouABot"), ("t'es chat gpt ?", "AreYouABot"), ("t'as un cœur ?", "AreYouABot"),
    ("c'est toi qui écris ?", "AreYouABot"), ("are you a bot", "AreYouABot"),
    ("tfq", "WhatAreYouDoing"), ("qu'est-ce que tu fabriques ?", "WhatAreYouDoing"), ("tu t'ennuies ?", "WhatAreYouDoing"),
    ("wyd", "WhatAreYouDoing"), ("quoi de nouveau ?", "WhatAreYouDoing"),
    // Goodnight / Goodbye
    ("gn", "Goodnight"), ("dors bien syncs", "Goodnight"), ("bon je vais roupiller", "Goodnight"), ("go dodo", "Goodnight"),
    ("a+", "Goodbye"), ("A+ tout le monde", "Goodbye"), ("@+", "Goodbye"), ("a2m1", "Goodbye"), ("à tte", "Goodbye"),
    ("bon j'y vais", "Goodbye"), ("allez salut", "Goodbye"), ("je me sauve", "Goodbye"), ("tchuss", "Goodbye"),
    ("bonne continuation", "Goodbye"), ("j'y vais aussi samedi", "None"), ("salut", "None"),
    // Maybe
    ("j'en sais rien", "Maybe"), ("chais pas", "Maybe"), ("qui sait", "Maybe"), ("probablement", "Maybe"),
    ("pas sûr", "Maybe"), ("non je sais pas", "Maybe"), ("idk", "Maybe"), ("pas probable", "None"),
    // YouAreRight
    ("t'as trop raison", "YouAreRight"), ("tellement vrai", "YouAreRight"), ("je confirme", "YouAreRight"),
    ("elle a raison", "YouAreRight"), ("effectivement", "YouAreRight"), ("c'est clair", "YouAreRight"),
    ("c'est clair ?", "Question"), ("en effet", "YouAreRight"), ("fair enough", "YouAreRight"),
    ("j'ai bien vu le film hier avec mon frère et ma sœur", "Sharing"),
    // YouAreWrong
    ("t'es dans le faux", "YouAreWrong"), ("tu délires", "YouAreWrong"), ("nimp", "YouAreWrong"), ("tu mens", "Disbelief"),
    ("jamais de la vie", "YouAreWrong"), ("dans tes rêves", "YouAreWrong"), ("pas vraiment", "YouAreWrong"),
    ("wrong", "YouAreWrong"),
    // Request
    ("tu veux pas lancer un sondage ?", "Request"), ("j'aimerais que tu fasses les équipes", "Request"),
    ("annule la session", "Request"), ("tu peux timeout Luca ?", "Request"), ("réveille-toi", "AreYouThere"),
    ("mets un rappel pour demain", "Request"), ("donne-moi un café", "None"),
    // Laughter
    ("hohoho", "Laughter"), ("kekw", "Laughter"), ("je suis pliée", "Laughter"), ("je pleure de rire", "Laughter"),
    ("bonne blague", "Laughter"), ("niark niark", "Laughter"), ("😁", "Laughter"), ("je pleure", "None"),
    // LowEffort
    ("hein", "LowEffort"), ("pff", "LowEffort"), ("oh là là", "LowEffort"), ("tkt", "LowEffort"), ("yeah", "LowEffort"),
    ("ah ok genre", "LowEffort"),
    // Question
    ("qui vient ce soir", "NextSession"), ("où est le lien", "Question"), ("c'est quand la session", "NextSession"),
    ("quelle heure on commence", "Question"), ("tu sais si Luca vient", "Question"), ("why are you like this", "Question"),
    ("ou alors on joue demain", "None"), ("pkoi tu dis ça", "Question"),
};
foreach (var (text, expected) in cases3)
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
}

// 10. The « discute avec Syncs quand tu peux » misfire.
foreach (var (text, expected) in new[]
{
    ("<@555> Discute avec Syncs quand tu peux. J'ai rework son \"intelligence conversationelle\", donc faut que je trouve les bugs", "None"),
    ("si tu peux", "None"), ("dès que tu peux", "None"), ("je sais que tu peux le faire", "None"),
    ("Syncs, tu penses quoi de <@555> ?", "Opinion"),
    ("est-ce que tu peux lancer un sondage ?", "Request"), ("tu peux lancer un sondage quand tu peux ?", "Request"),
})
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
}
var addresses = cues.GetMethod("AddressesHer", BindingFlags.Public | BindingFlags.Static)!;
foreach (var (text, expected) in new[]
{
    ("syncs, ça va ?", true), ("ça va syncs ?", true), ("salut syncs ça va", true), ("S.Y.N.C.S, ça va ?", true),
    ("<@999> syncs ça va ?", true), ("syncs <@999> tu fais quoi", true),
    ("<@555> Discute avec Syncs quand tu peux", false), ("syncs ça va <@555> ?", false), ("<@&42> syncs ça va", false),
    ("dis à syncs de lancer un sondage", false), ("la sync a planté", false), ("je crois que syncs a bugué", false),
    // A tag that is what she is asked about doesn't take the message away from her.
    ("Syncs, tu penses quoi de <@555> ?", true), ("syncs tu préfères <@555> ou <@666> ?", true),
    ("syncs, <@555> et <@666> ça match ?", true), ("<@555> tu penses quoi de syncs ?", false),
    ("<@555> syncs, tu penses quoi de la pizza ?", false),
})
{
    var got = (bool)addresses.Invoke(null, new object[] { text, 999UL })!;
    if (got != expected) Fail($"AddressesHer(\"{text}\") = {got}, expected {expected}");
}

// 11. The GigaLaugh emote, AreYouThere, AboutHer, Crush, Help, Bored, Thanks, Ship, and their collisions.
var cases5 = new (string Text, string Expected)[]
{
    // GigaLaugh
    ("<:giga_laugh:1482304998871138346>", "Laughter"), ("mdr <:giga_laugh:1482304998871138346>", "Laughter"),
    ("<:giga_laugh:1482304998871138346> <:giga_laugh:1482304998871138346>", "Laughter"),
    // AreYouThere
    ("t'es là ?", "AreYouThere"), ("syncs t'es là ?", "AreYouThere"), ("tu dors ?", "AreYouThere"), ("tu dors ou quoi", "AreYouThere"),
    ("y a quelqu'un ?", "AreYouThere"), ("allô ?", "AreYouThere"), ("réveille-toi", "AreYouThere"), ("syncs ?", "AreYouThere"),
    ("t'es toujours là ?", "AreYouThere"), ("t'es là ce soir ?", "Question"), ("t'es la meilleure", "None"),
    ("tu dors bien la nuit ?", "Question"), ("syncs", "None"), ("tu connais quelqu'un qui joue ?", "Question"),
    // AboutHer
    ("t'as quel âge ?", "AboutHer"), ("quel âge tu as", "AboutHer"), ("c'est quand ton anniv ?", "AboutHer"),
    ("t'es née quand ?", "AboutHer"), ("tu habites où ?", "AboutHer"), ("tu viens d'où ?", "AboutHer"),
    ("c'est quoi ton jeu préféré ?", "AboutHer"), ("ta couleur préférée ?", "AboutHer"), ("tu aimes quoi comme musique ?", "AboutHer"),
    ("tu aimes les chats ?", "Opinion"), ("je préfère Minecraft", "None"),
    // Crush
    ("t'es célibataire ?", "Crush"), ("t'as un crush ?", "Crush"), ("t'es amoureuse de qui ?", "Crush"),
    ("c'est qui ton crush ?", "Crush"), ("t'es mon crush", "Love"), ("je suis amoureux de toi", "Love"),
    ("il te plaît le jeu ?", "Question"), ("t'as quelqu'un pour jouer ce soir ?", "Question"),
    // Help
    ("comment ça marche ?", "Help"), ("tu sais faire quoi ?", "Help"), ("help", "Help"), ("aide-moi", "Help"),
    ("tu peux m'aider ?", "Help"), ("comment ça marche les sondages ?", "Help"), ("je suis perdu", "Help"),
    ("je comprends rien", "Help"), ("j'ai pas besoin d'aide", "Sharing"),
    ("tu peux m'aider à lancer un sondage ?", "Request"), ("aide-moi à organiser une session", "Request"),
    ("je suis perdu dans le donjon", "Sharing"), ("comment on fait pour battre ce boss", "Question"),
    ("tu peux danser ?", "Request"), ("tu sais faire un café ?", "Request"),
    // Bored
    ("je m'ennuie", "Bored"), ("on s'ennuie ici", "Bored"), ("on fait quoi ?", "Bored"), ("y a rien à faire", "Bored"),
    ("je sais pas quoi faire", "Bored"), ("je m'ennuie pas du tout", "Sharing"), ("j'en ai rien à faire", "None"),
    ("on fait quoi pour la session de samedi ?", "Question"), ("tu t'ennuies ?", "WhatAreYouDoing"), ("boring", "Bored"),
    // Thanks
    ("merci", "Thanks"), ("merci syncs", "Thanks"), ("merci beaucoup !", "Thanks"), ("thx", "Thanks"), ("ty", "Thanks"),
    ("c'est gentil", "Thanks"), ("non merci", "None"), ("merci, bonne nuit", "Goodnight"), ("merci à plus", "Goodbye"),
    ("tu peux lancer un sondage stp merci", "Request"), ("merci pour la session d'hier, c'était top", "Thanks"),
    ("ty for the carry yesterday guys", "None"),
    // Ship
    ("ship Luca et Sandra", "Ship"), ("tu peux ship moi et Luca ?", "Ship"), ("tu me ship avec <@123> ?", "Ship"),
    ("Luca et moi, ça match ?", "Ship"), ("toi et moi on irait bien ensemble ?", "Ship"),
    ("la pizza et l'ananas, compatibles ?", "Ship"), ("syncs, Luca et moi ça match ?", "Ship"),
    ("toi et moi on joue ensemble ?", "Question"), ("la relationship est compliquée", "None"),
    ("je pense que Luca et Sandra ça match", "None"),
};
foreach (var (text, expected) in cases5)
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
}
void ShipIs(string text, string? a, string? b)
{
    var p = ShipAsk.Read(text);
    if (p?.A != a || p?.B != b) Fail($"ShipAsk.Read(\"{text}\") = ({p?.A}, {p?.B}), expected ({a}, {b})");
}
ShipIs("ship Luca et Sandra", "Luca", "Sandra");
ShipIs("tu me ship avec <@123> ?", "moi", "<@123>");
ShipIs("<@999> Luca et moi, ça match ?", "Luca", "moi");
ShipIs("syncs, la pizza et l'ananas compatibles ?", "pizza", "ananas");
ShipIs("shippe-moi avec quelqu'un", "moi", "quelqu'un");
// « pense » without its s, as typed in the wild.
ShipIs("et tu pense quoi de <@111> x <@222> ?", "<@111>", "<@222>");
ShipIs("syncs tu pense quoi de <@111> x <@222>", "<@111>", "<@222>");
ShipIs("<@111> et <@222>, t'en pense quoi ?", "<@111>", "<@222>");
var herTopic = cues.GetMethod("ReadHerTopic", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) })!;
foreach (var (text, topic) in new[] { ("t'as quel âge ?", "Age"), ("ton anniv c'est quand", "Birthday"), ("tu vis où ?", "Home"), ("ton plat préféré ?", "Favourite"),
             ("t'es en quelle version ?", "Version"), ("c'est quoi ta version syncs ?", "Version"), ("t'es à jour ?", "Version"),
             ("<@123> tu tournes sur quelle version ?", "Version"), ("t'es en quelle version là ? tu viens ce soir ?", "Version"),
             ("quelle version tu es", "Version"), ("what version are you on?", "Version") })
    if (herTopic.Invoke(null, new object[] { text })?.ToString() != topic) Fail($"ReadHerTopic(\"{text}\") != {topic}");
// Her version, asked; the game's version, or someone's version of the facts, is something else.
foreach (var (text, expected) in new[]
         {
             ("t'es en quelle version ?", "AboutHer"), ("syncs, c'est quoi ta version ?", "AboutHer"), ("t'es à jour ?", "AboutHer"),
             ("t'es en quelle version de Minecraft ?", "Question"), ("t'as quelle version du jeu ?", "Question"),
             ("c'est quoi ta version des faits ?", "Question"), ("t'es à jour sur le patch ?", "Question"),
             ("je préfère ta version", "None"),
         })
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
}
var readTopicAny = cues.GetMethod("ReadTopic", BindingFlags.Public | BindingFlags.Static)!;
if (readTopicAny.Invoke(null, new object[] { "comment ça marche les sondages ?" })!.ToString() != "Poll") Fail("help topic: sondages");
if (MorningGreeting.DaysOld(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero)) != 122) Fail("DaysOld on 16 Oct 2026");

// 12. The soup exchange, Disbelief, the wider ship, and a sweep of ordinary chat.
var cases6 = new (string Text, string Expected)[]
{
    // The exchange as it happened.
    ("<@999> pwet pwet", "None"), ("J'ai mangé de la soupe", "Sharing"), ("Respecte la soupe <:pepe:123>", "None"),
    ("Et Wall-E dans tout ça?", "Question"), ("Ouais ouais fais genre", "Disbelief"),
    // Disbelief, and what must stay something else.
    ("fais genre", "Disbelief"), ("mon œil", "Disbelief"), ("mais oui c'est ça", "Disbelief"), ("avoue", "Disbelief"),
    ("allez avoue", "Disbelief"), ("t'es grillée", "Disbelief"), ("tu rougis là", "Disbelief"), ("on te croit pas", "Disbelief"),
    ("je te crois pas", "Disbelief"), ("mais bien sûr", "Disbelief"), ("avoue que t'as triché", "Disbelief"),
    ("j'avoue", "YouAreRight"), ("j'avoue c'est fort", "YouAreRight"), ("il fait genre il sait jouer", "None"),
    ("oui bien sûr que je viens", "None"), ("je te crois", "None"), ("il a grillé un feu rouge", "None"),
    ("genre t'as gagné ?", "Question"), ("mais non fais genre", "Disbelief"),
    // Ship, wider.
    ("le ship entre Luca et Sandra", "Ship"), ("compatibilité entre Luca et moi ?", "Ship"), ("love calculator Luca et Sandra", "Ship"),
    ("est-ce que Luca et moi on est compatibles ?", "Ship"), ("Luca x Sandra ?", "Ship"), ("on irait bien ensemble ?", "Ship"),
    ("syncs on ferait un beau couple ?", "Ship"), ("tu nous ship ?", "Ship"), ("<@123> on irait bien ensemble ?", "Ship"),
    ("c'est qui mon âme sœur ?", "Ship"), ("avec qui tu me ship ?", "Ship"), ("trouve-moi l'âme sœur", "Ship"),
    ("c'est qui mon pire ship ?", "Ship"), ("avec qui je suis le moins compatible ?", "Ship"), ("shipper Luca avec Sandra", "Ship"),
    ("à ton avis, Luca et Sandra ça match ?", "Ship"), ("Luca et Sandra en couple ?", "Ship"),
    ("et tu pense quoi de <@111> x <@222> ?", "Ship"), ("syncs tu pense quoi de <@111> x <@222>", "Ship"),
    ("vendredi et samedi ensemble ?", "Question"), ("la manette et le PC c'est compatible ?", "Question"),
    ("c'était mon pire match de la saison", "None"), ("mon meilleur match c'était hier", "None"),
    ("on est ensemble ce soir ?", "Question"), ("trouve-moi quelqu'un pour jouer", "None"),
    ("samedi et dimanche, ça marche ?", "Question"), ("2 x 3 ?", "Question"), ("on va jouer ensemble ?", "Question"),
    // Sleep and boredom, tightened.
    ("je vais dormir chez Luca demain", "Sharing"), ("je m'endors devant le film", "Sharing"), ("bon je vais dormir", "Goodnight"),
    ("je vais me coucher, bonne nuit", "Goodnight"), ("je vais dormir là", "Goodnight"), ("dodo", "Goodnight"),
    ("y a rien à faire contre ce boss", "None"), ("y a rien à faire ce soir", "Bored"), ("y a rien à faire", "Bored"),
    ("je m'ennuie de toi", "None"), ("je m'ennuie trop", "Bored"), ("j'ai rien à faire aujourd'hui", "Bored"),
};
foreach (var (text, expected) in cases6)
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
}
foreach (var (text, kind) in new[]
{
    ("on irait bien ensemble ?", ShipAskKind.Us), ("c'est qui mon âme sœur ?", ShipAskKind.BestMatch),
    ("c'est qui mon pire ship ?", ShipAskKind.WorstMatch), ("ship Luca et moi", ShipAskKind.Pair),
})
    if (ShipAsk.Read(text)?.Kind != kind) Fail($"ShipAsk kind of \"{text}\" = {ShipAsk.Read(text)?.Kind}, expected {kind}");
ShipIs("est-ce que Luca et moi on est compatibles ?", "Luca", "moi");
ShipIs("compatibilité entre la pizza et l'ananas", "pizza", "ananas");

// The sweep: everyday chat aimed at her (a reply, a mention) that asks nothing she has a specific
// answer for. Anything specific here is a misfire: it must read None, Question or LowEffort.
var generic = new HashSet<string> { "None", "Question", "Sharing", "LowEffort" };
var neutral = new[]
{
    "je suis en retard de 10 minutes", "le boss final est trop dur", "j'ai fini le donjon hier soir", "qui est chaud pour un raid ?",
    "il est où le lien du serveur ?", "j'arrive dans 5 min", "on se retrouve sur le vocal", "t'as vu le patch notes ?",
    "ils ont nerf mon perso", "ce jeu est trop bien", "j'ai acheté le DLC", "la mise à jour est sortie", "j'ai plus de batterie",
    "je dois manger avant", "Luca est pas encore là", "c'est quoi le code du lobby", "j'ai oublié mon mot de passe",
    "ma connexion est nulle ce soir", "on est combien ce soir ?", "je peux pas venir samedi", "il pleut des cordes ici",
    "mon chat a vomi sur le clavier", "demain je bosse tôt", "qui a pris mon spot ?", "j'ai eu un bug de collision",
    "on fait une partie de Mario Kart après ?", "t'as fait combien de kills ?", "le serveur a crash", "j'ai rage quit",
    "c'est la troisième fois que je meurs", "j'ai trouvé un œuf de Plynling", "mon plynling a faim", "j'ai gagné 200 cailloux",
    "il me manque une pièce pour le craft", "on se fait un morpion ?", "je suis devant le PC", "la soirée était cool hier",
    "vous avez vu le match hier ?", "il reste de la place ?", "y a plus de place", "t'inquiète je gère", "je gère pas ce boss",
    "c'est mort pour ce soir", "c'est calme ce soir", "rien à faire, il est trop fort", "t'as fini ?", "tu dors chez qui ?",
    "tu dors jamais toi", "y a quelqu'un pour un duo ?", "je comprends rien à ce jeu", "comment on fait pour rejoindre ?",
    "tu peux pas savoir", "il est gentil ce boss", "t'es pas croyable", "c'est quoi cette commande ?", "le bot a planté hier soir",
    "j'ai voté pour samedi", "je vote non", "qui organise la soirée ?", "tu sais si Luca vient ?", "t'as pas un chargeur ?",
    "je reviens dans 2 min", "je vais me chercher à boire", "j'ai sommeil mais je reste", "il dort déjà", "elle est là ce soir ?",
    "t'as vu la vidéo ?", "ça va être chaud ce soir", "ça va pas du tout ce patch", "pas du tout mon style de jeu",
    "je te laisse gagner", "possible que je sois en retard", "bonne idée", "tu es la seule à comprendre", "t'es la reine",
    "il est là le boss ?", "je dors chez Luca ce soir", "on lance une game ce soir ?", "il faut que je parte à 22h",
    "le dernier boss m'a tué trois fois", "j'ai mis à jour mes drivers", "on peut jouer à 4 ?", "je joue sur Switch",
    "mon micro marche pas", "tu m'entends pas bien ?", "je capte mal ici", "le jeu est en promo", "il a pris le dernier ticket",
    "j'ai eu la flemme", "je préfère le mode solo", "le niveau 3 est horrible", "je suis bloqué au chapitre 2",
    "il faut farmer encore", "j'ai drop une légendaire", "elle a carry la game", "on a perdu de peu", "c'était serré",
    "la team rouge a gagné", "je suis mort au premier round", "ce perso est cheaté", "faut nerf ce mec",
    "t'as combien de niveaux ?", "j'ai changé de pseudo", "c'est ma première fois sur ce jeu", "je suis nouveau ici",
    "le tournoi commence quand ?", "j'ai pris un abonnement", "le serveur est plein", "ça lag de ouf", "mon PC chauffe",
    "j'ai faim", "j'ai soif", "je suis crevé", "j'ai trop mangé", "il fait chaud ici", "je suis dehors", "je rentre du boulot",
    "je suis dans le train", "je finis mon film et j'arrive", "le film était nul", "la série est trop longue",
    "j'ai vu un écureuil", "mon frère joue aussi", "ma sœur arrive", "on mange quoi ce soir ?", "il est quelle heure ?",
    "ça commence à quelle heure ?", "je mets la musique", "la musique est trop forte", "je baisse le son",
    "j'ai perdu ma manette", "la manette et le PC c'est compatible ?", "samedi et dimanche, ça marche ?",
    "vendredi et samedi ensemble ?", "c'était mon pire match de la saison", "on est ensemble ce soir ?",
    "trouve-moi quelqu'un pour jouer", "je vais dormir chez Luca demain", "je m'endors devant le film",
    "y a rien à faire contre ce boss", "je m'ennuie de toi", "il fait genre il sait jouer", "oui bien sûr que je viens",
    "il a grillé un feu rouge", "je te crois", "c'est la vie", "c'est comme ça", "tant pis", "on verra bien demain soir avec les autres",
    "je l'ai vu hier", "il me l'a dit", "elle m'a envoyé un message", "on s'appelle plus tard", "je t'envoie le lien",
    "je te dis ça demain", "je te montre après", "je t'attends en vocal", "je te rejoins", "je te suis",
};
var misfires = 0;
foreach (var text in neutral)
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString()!;
    if (!generic.Contains(got)) { misfires++; Fail($"sweep: \"{text}\" read as {got}"); }
}
Console.WriteLine($"sweep: {neutral.Length} ordinary messages, {misfires} misfire(s)");

// 13. Second sweep: slang and gaming shorthand, and her name said in passing.
var slang = new[]
{
    "gg", "ez", "rip", "f", "wsh", "frr", "chui là", "j'arrive", "go", "go go go", "let's go", "on y va", "nice try",
    "noob", "gg wp", "osef de lui", "c'est chaud", "c'est ouf", "trop fort lui", "il est fou", "zebi", "wesh alors",
    "t'es sérieux ?", "sérieux ?", "vraiment ?", "ah bon ?", "hein ?", "quoi ?", "comment ça ?", "pourquoi ?",
    "la honte", "c'est la hess", "je suis dead", "chaud chaud", "jsuis grave en retard", "chui en route",
    "attends 2 sec", "deux secondes", "brb", "afk", "re", "back", "dispo ?", "qui est dispo ?", "vous êtes où ?",
    "on attend qui ?", "t'es en game ?", "lance", "invite moi", "accepte", "je t'add", "ajoute moi en ami",
    "c'est quoi ton pseudo ?", "t'as discord ?", "mdr non", "nan mais allô", "allô quoi",
};
var slangMisfires = 0;
foreach (var text in slang)
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString()!;
    if (!generic.Contains(got)) { slangMisfires++; Console.WriteLine($"  slang: \"{text}\" -> {got}"); }
}
Console.WriteLine($"slang sweep: {slang.Length} messages, {slangMisfires} specific");

foreach (var text in new[]
{
    "syncs a posté le sondage", "le sondage de syncs est nul", "demande à syncs", "j'ai parlé avec syncs",
    "c'est syncs qui gère ça", "la sync a planté", "pour syncs c'est facile", "<@555> regarde ce que syncs a dit",
    "sans syncs on serait perdus", "grâce à syncs j'ai pas oublié",
})
    if ((bool)addresses.Invoke(null, new object[] { text, 999UL })!) Fail($"AddressesHer(\"{text}\") is true for her name said in passing");

// 14. Sharing: someone telling her about their day.
foreach (var (text, expected) in new[]
{
    ("J'ai mangé de la soupe", "Sharing"), ("bon, je suis allé au ciné", "Sharing"), ("on a perdu la game", "Sharing"),
    ("hier j'ai fini Elden Ring", "Sharing"), ("du coup je me suis acheté un clavier", "Sharing"), ("chui trop crevé", "Sharing"),
    ("moi j'ai dormi 12 heures", "Sharing"), ("je viens de rentrer du boulot", "Sharing"), ("on est sortis au resto", "Sharing"),
    ("mdr j'ai raté mon bus", "Sharing"),
    // Not sharing: earlier intents, questions, opinions and wishes, things said to the listener.
    ("j'ai mangé de la soupe ?", "Question"), ("je m'ennuie", "Bored"), ("je suis d'accord", "YouAreRight"),
    ("je pense que c'est nul", "None"), ("je veux jouer", "None"), ("je t'envoie le lien", "None"),
    ("je te rejoins", "None"), ("je sais pas", "Maybe"), ("je vais dormir", "Goodnight"), ("j'ai faim", "Sharing"),
    ("je t'aime", "Love"), ("je comprends rien", "Help"), ("on y va", "None"), ("je", "None"),
    ("Luca a mangé de la soupe", "None"), ("on s'appelle plus tard", "None"), ("on s'est fait battre", "Sharing"),
    ("je préfère Minecraft", "None"), ("j'en ai rien à faire", "None"), ("on joue à quoi ce soir", "None"),
})
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
}
var sharedInSweep = neutral.Count(t => readIntent.Invoke(null, new object[] { t })!.ToString() == "Sharing");
Console.WriteLine($"sweep: {sharedInSweep} of {neutral.Length} ordinary messages now read as Sharing");

// 15. Wider Sharing and Ship cues.
foreach (var (text, expected) in new[]
{
    // Sharing
    ("devine quoi, j'ai gagné le tournoi", "Sharing"), ("tu sais quoi, j'ai eu mon permis", "Sharing"),
    ("au fait, je me suis coupé les cheveux", "Sharing"), ("jsors du ciné", "Sharing"), ("jrentre du taf", "Sharing"),
    ("mon chat a vomi sur le clavier", "Sharing"), ("ma sœur m'a offert un jeu", "Sharing"), ("nous avons gagné le raid", "Sharing"),
    ("en vrai j'ai bien dormi", "Sharing"), ("omg j'ai drop une légendaire", "Sharing"), ("on était trop forts hier", "Sharing"),
    ("la semaine dernière j'ai fini le jeu", "Sharing"), ("mes parents sont partis en vacances", "Sharing"),
    ("mon micro marche pas", "None"), ("ma sœur arrive", "None"), ("tu sais quoi ?", "Question"), ("devine quoi", "None"),
    ("mon avis c'est que c'est nul", "None"), ("ton chat a vomi", "None"), ("je préfère le mode solo", "None"),
    // Ship
    ("le couple Luca et Sandra", "Ship"), ("couple Luca / Sandra ?", "Ship"), ("Luca ❤️ Sandra ?", "Ship"), ("Luca <3 Sandra ?", "Ship"),
    ("ship <@1> <@2>", "Ship"), ("<@999> <@1> <@2> compatibles ?", "Ship"), ("on va finir ensemble ?", "Ship"),
    ("tu nous vois en couple ?", "Ship"), ("on est un beau couple ?", "Ship"), ("qui irait bien avec moi ?", "Ship"),
    ("je vais finir avec qui ?", "Ship"), ("avec qui je devrais sortir ?", "Ship"), ("qui va m'épouser ?", "Ship"),
    ("c'est qui mon futur mari ?", "Ship"), ("Luca et Sandra vont ensemble ?", "Ship"), ("la personne faite pour moi ?", "Ship"),
    ("gg ❤️ bien joué", "None"), ("merci ❤️", "Thanks"), ("<@1> <@2> on fait un match ?", "Question"),
    ("un couple de lapins et de chats", "None"), ("on va ensemble au ciné ?", "Question"),
    ("je finis avec qui en équipe ?", "Question"),
})
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
}
ShipIs("ship <@1> <@2>", "<@1>", "<@2>");
ShipIs("<@999> <@1> <@2> compatibles ?", "<@1>", "<@2>");
ShipIs("le couple Luca et Sandra", "Luca", "Sandra");
ShipIs("Luca ❤️ Sandra ?", "Luca", "Sandra");
if (ShipAsk.Read("c'est qui mon futur mari ?")?.Kind != ShipAskKind.BestMatch) Fail("futur mari is not a best-match search");
if (ShipAsk.Read("on va finir ensemble ?")?.Kind != ShipAskKind.Us) Fail("on va finir ensemble is not Us");
ShipIs("syncs   Luca    et   Sandra ,   ça   match ?", "Luca", "Sandra");
ShipIs("Luca　et　Sandra ça match ?", "Luca", "Sandra");
// Crafted whitespace once kept CompatRegex and OpinionBeforePairRegex backtracking for minutes, on
// the gateway task. Every reader of chat text must stay fast on it.
{
    string Rep(string s, int n) => string.Concat(Enumerable.Repeat(s, n));
    var hostile = new[]
    {
        "syncs a" + Rep(" ", 600) + "et" + Rep(" ", 600) + "b" + Rep(" ", 600) + "z",
        "syncs a" + Rep("　", 600) + "avec" + Rep("　", 600) + "b" + Rep("　", 600) + "z",
        "a" + Rep(" ", 600) + "x" + Rep(" ", 600) + "b" + Rep(" ", 600) + "z?",
        "syncs " + Rep("a  et  ", 280) + "b" + Rep(" ", 40) + "!",
        "syncs " + Rep("a et ", 40),
        "syncs " + Rep("a x ", 50) + "z ton avis ?",
    };
    foreach (var text in hostile)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        ShipAsk.Read(text);
        Opinion.Read(text);
        readIntent.Invoke(null, new object[] { text });
        if (watch.ElapsedMilliseconds > 500)
            Fail($"reading {text.Length} hostile chars took {watch.ElapsedMilliseconds} ms");
    }
}
foreach (var pool in new[] { "SharingShrugLines" })
    if (((string[])Field(pool)).Length != 32) Fail($"{pool} has {((string[])Field(pool)).Length} lines, expected 32");
var sharingIntent = ((IDictionary)Field("IntentLines"))[Enum.Parse(intentType, "Sharing")];
if (((string[])sharingIntent!).Length != 32) Fail($"IntentLines[Sharing] has {((string[])sharingIntent).Length} lines, expected 32");

// 16. Every intent under six variants: the reading must not depend on caps, a tag, her name as an
// address at either end, a trailing emote, or emphasis.
var byIntent = new (string Intent, string[] Samples)[]
{
    ("Love", new[] { "je t'aime", "jtm", "je t'adore", "je suis amoureux de toi", "tu veux sortir avec moi ?", "je t'apprécie beaucoup", "love you" }),
    ("Apology", new[] { "pardon", "désolé", "dsl j'étais pas là", "my bad", "c'est ma faute", "j'ai pas fait exprès", "toutes mes excuses" }),
    ("HowAreYou", new[] { "ça va ?", "comment tu vas ?", "tu vas bien ?", "ça roule ?", "bien dormi ?", "comment va la vie ?", "et toi, ça va ?" }),
    ("AreYouThere", new[] { "t'es là ?", "tu dors ?", "y a quelqu'un ?", "t'es toujours là ?", "réveille-toi" }),
    ("WhoAreYou", new[] { "t'es qui ?", "qui es-tu ?", "tu sers à quoi ?", "comment tu t'appelles ?", "c'est qui ton créateur ?", "présente-toi" }),
    ("AboutHer", new[] { "t'as quel âge ?", "c'est quand ton anniv ?", "tu habites où ?", "c'est quoi ton jeu préféré ?", "tu aimes quoi comme musique ?" }),
    ("AreYouABot", new[] { "t'es un bot ?", "t'es une IA ?", "tu as des sentiments ?", "t'es humaine ?", "es-tu une IA ?" }),
    ("Crush", new[] { "t'es célibataire ?", "t'as un crush ?", "t'es amoureuse ?", "c'est qui ton crush ?" }),
    ("WhatAreYouDoing", new[] { "tu fais quoi ?", "quoi de neuf ?", "qu'est-ce que tu fabriques ?", "tu t'ennuies ?", "t'es où ?" }),
    ("Opinion", new[] { "t'en penses quoi de la pizza ?", "ton avis sur Valorant ?", "tu aimes les chats ?", "tu préfères Minecraft ou Fortnite ?" }),
    ("Ship", new[] { "ship Luca et Sandra", "Luca et moi, ça match ?", "on irait bien ensemble ?", "c'est qui mon âme sœur ?", "tu penses quoi de <@111> et <@222> ?", "le couple Luca et Sandra" }),
    ("Help", new[] { "comment ça marche ?", "tu sais faire quoi ?", "aide-moi", "j'ai besoin d'aide", "je comprends rien" }),
    ("Request", new[] { "tu peux organiser une session ?", "organise une soirée", "lance un sondage", "tu peux me rappeler de manger ?", "fais des équipes" }),
    ("Bored", new[] { "je m'ennuie", "on s'ennuie", "on fait quoi ?", "y a rien à faire", "je sais pas quoi faire" }),
    ("Goodnight", new[] { "bonne nuit", "je vais dormir", "dodo", "je vais me coucher", "dors bien", "beaux rêves" }),
    ("Goodbye", new[] { "à plus", "à demain", "bye", "je dois y aller", "bon j'y vais", "allez salut", "a+" }),
    ("Thanks", new[] { "merci", "merci beaucoup", "c'est gentil", "thx", "je te remercie" }),
    ("Disbelief", new[] { "fais genre", "mon œil", "mais oui c'est ça", "avoue", "t'es grillée", "on te croit" }),
    ("Maybe", new[] { "peut-être", "on verra", "jsp", "je sais pas", "pourquoi pas", "probablement" }),
    ("YouAreRight", new[] { "t'as raison", "exactement", "bien vu", "je confirme", "j'avoue", "tellement vrai" }),
    ("YouAreWrong", new[] { "t'as tort", "c'est faux", "n'importe quoi", "tu délires", "pas du tout" }),
    ("Laughter", new[] { "mdr", "ptdr", "hahaha", "😂", "jpp", "je suis mort de rire", "t'es trop drôle" }),
    ("Question", new[] { "c'est quoi le jeu de ce soir ?", "pourquoi t'as dit ça", "c'est quoi le code du lobby ?", "il est où le lien ?" }),
    ("DemandApology", new[] { "excuse-toi", "dis pardon", "tu me dois des excuses", "demande pardon", "tu t'excuses ?", "je veux des excuses" }),
    ("ShutUp", new[] { "chut", "tais-toi", "silence", "la ferme", "arrête de parler", "laisse-nous tranquille", "chuuut" }),
    ("MyStats", new[] { "je suis quel niveau ?", "j'ai combien de cailloux ?", "comment va mon plynling ?", "mon niveau ?", "mes stats ?" }),
    ("TakeSides", new[] { "qui a raison, moi ou Luca ?", "qui a raison entre Luca et Sandra ?", "qui a raison ?", "dis-lui que j'ai raison" }),
    ("RateMe", new[] { "note-moi", "tu me notes ?", "je vaux combien ?", "je suis beau ?", "tu me trouves comment ?" }),
    ("NextSession", new[] { "c'est quand la prochaine session ?", "y a une session ce soir ?", "on joue ce soir ?", "qui vient ce soir ?", "y a quoi de prévu ?" }),
    ("Roll", new[] { "lance un dé", "1d20", "lance les dés", "choisis un nombre entre 1 et 10", "un nombre au hasard ?", "jette deux dés" }),
    ("Sharing", new[] { "j'ai mangé de la soupe", "on a perdu la game", "je viens de rentrer du boulot", "devine quoi, j'ai gagné le tournoi", "mon chat a vomi sur le clavier" }),
    ("LowEffort", new[] { "ok", "ah bon", "bah oui", "d'accord", "hein" }),
    ("Down", new[] { "je suis triste", "j'ai passé une sale journée", "ça va pas fort", "j'en peux plus", "j'ai pas le moral", "je me sens vraiment mal" }),
    ("ShouldI", new[] { "je devrais aller dormir ?", "je l'achète ?", "Valo ou LoL ?", "je joue ou je dors ?", "j'y vais ou pas ?" }),
    ("Joke", new[] { "raconte une blague", "fais-moi rire", "t'as une blague ?", "une blague ?", "tu peux me raconter une blague ?" }),
    ("FunFact", new[] { "un fun fact ?", "dis-moi un truc", "apprends-moi un truc", "raconte-moi quelque chose" }),
    ("Blame", new[] { "c'est la faute de qui ?", "c'est qui le coupable ?", "à qui la faute ?" }),
    ("DoYouLoveMe", new[] { "tu m'aimes ?", "tu m'aimes bien ?", "tu me détestes ?", "tu m'en veux ?", "je suis ton préféré ?" }),
    ("RememberMe", new[] { "tu te souviens de moi ?", "comment je m'appelle ?", "tu me connais ?", "tu sais qui je suis ?", "c'est quoi mon pseudo ?" }),
    ("AreYouUpset", new[] { "t'es fâchée ?", "tu boudes ?", "t'es triste ?", "t'es jalouse ?", "tu me fais la tête ?" }),
};

string WithTrailingName(string s) =>
    s.EndsWith('?') ? s[..^1].TrimEnd() + " syncs ?" : s + " syncs";

var variants = new (string Name, Func<string, string> Make)[]
{
    ("as is", s => s),
    ("caps", s => s.ToUpperInvariant()),
    ("tagged", s => "<@999> " + s),
    ("name first", s => "syncs, " + s),
    ("name last", WithTrailingName),
    ("emote", s => s + " 🙂"),
    ("emphasis", s => s.EndsWith('?') ? s : s + " !!"),
};

var variantFailures = new List<string>();
var checkedCount = 0;
foreach (var (intent, samples) in byIntent)
foreach (var sample in samples)
foreach (var (name, make) in variants)
{
    var text = make(sample);
    var got = readIntent.Invoke(null, new object[] { text })!.ToString()!;
    checkedCount++;
    if (got != intent) variantFailures.Add($"{intent,-15} {name,-11} \"{text}\" -> {got}");
}
Console.WriteLine($"variants: {checkedCount} readings, {variantFailures.Count} off");
foreach (var f in variantFailures) Console.WriteLine("  " + f);
if (variantFailures.Count > 0) Fail($"{variantFailures.Count} variant readings off");

// Every intent the enum has is covered by the table above, so a new one can't ship untested. Yes and
// No are read by ReadAnswer instead (section 21).
var covered = byIntent.Select(b => b.Intent).Append("Yes").Append("No").ToHashSet();
foreach (var v in Enum.GetNames(intentType).Where(n => n != "None"))
    if (!covered.Contains(v)) Fail($"intent {v} has no samples in the variant table");

// Two intents in one message: which one wins.
foreach (var (text, expected) in new[]
{
    ("merci, bonne nuit", "Goodnight"), ("merci et à demain", "Goodbye"), ("désolé, je t'aime", "Love"), ("pardon, ça va ?", "Apology"),
    ("mdr t'as raison", "YouAreRight"), ("ok merci", "Thanks"), ("merci t'es là ?", "AreYouThere"), ("bonne nuit, je t'aime", "Love"),
    ("t'es là ? je m'ennuie", "AreYouThere"), ("je m'ennuie, on fait quoi ?", "Bored"), ("peut-être, je sais pas", "Maybe"),
    ("fais genre, t'as tort", "Disbelief"), ("avoue, t'as un crush ?", "Crush"), ("ça va ? tu fais quoi ?", "HowAreYou"),
    ("tu fais quoi ? je m'ennuie", "WhatAreYouDoing"), ("t'es qui ? t'es un bot ?", "WhoAreYou"),
    ("tu peux ship Luca et Sandra ?", "Ship"), ("tu peux m'aider ?", "Help"), ("tu peux m'aider à lancer un sondage ?", "Request"),
    ("je suis d'accord, merci", "YouAreRight"), ("haha merci", "Thanks"), ("dsl j'ai mangé de la soupe", "Apology"),
    ("j'ai mangé de la soupe, et toi ça va ?", "HowAreYou"), ("on fait quoi ce soir ? je m'ennuie", "Bored"),
})
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"priority: \"{text}\" = {got}, expected {expected}");
}

// A third sweep: longer, multi-sentence messages about other things, quotes and third parties.
var longNeutral = new[]
{
    "hier on a joué jusqu'à 3h du matin avec Luca, c'était n'importe quoi mais on a fini le donjon",
    "Luca m'a dit qu'il viendrait mais il a toujours pas répondu, il est vraiment pas fiable",
    "le patch de ce soir change tout le méta, les tanks sont devenus inutiles et les soigneurs trop forts",
    "je sais pas si je pourrai venir samedi, ça dépend de mon boulot, je vous redis",
    "« t'es qui toi ? » qu'il m'a dit, j'ai rien compris",
    "il a écrit « je t'aime » à sa copine dans le mauvais salon, mdr",
    "elle a demandé « ça va ? » à tout le monde sauf à moi",
    "Sandra a dit bonne nuit à 20h, elle est malade je crois",
    "on devrait faire un tournoi le mois prochain avec des équipes de quatre",
    "quelqu'un a vu mon casque ? je le trouve plus nulle part depuis hier",
    "la musique du boss final est incroyable, je l'écoute en boucle",
    "j'ai demandé à Luca s'il était célibataire, il a rougi",
    "le bot de musique a encore planté pendant la soirée",
    "faut que je pense à acheter des piles pour la manette demain",
    "si vous voulez on peut décaler la session à dimanche, moi ça me va",
};
foreach (var text in longNeutral)
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString()!;
    if (!generic.Contains(got)) Fail($"long sweep: \"{text}\" read as {got}");
}

// 17. « Tu penses quoi de @x et @Y ? », quotes, and trailing decoration.
foreach (var (text, expected) in new[]
{
    ("Tu penses quoi de <@111> et <@222> ?", "Ship"), ("<@999> tu penses quoi de <@111> et <@222> ?", "Ship"),
    ("<@111> et <@222>, t'en penses quoi ?", "Ship"), ("tu penses quoi de moi et Luca ?", "Ship"),
    ("t'en penses quoi de Luca et Sandra ensemble ?", "Ship"), ("<@111> et <@222> ça match ?", "Ship"),
    ("t'en penses quoi de la pizza et des pâtes ?", "Opinion"), ("t'en penses quoi de Minecraft et Fortnite ?", "Opinion"),
    ("tu penses quoi de moi et de mes cheveux ?", "Opinion"), ("t'en penses quoi de <@111> ?", "Opinion"),
    ("il a écrit « je t'aime » à sa copine, mdr", "None"), ("« je t'aime »", "Love"), ("\"je t'aime\"", "Love"),
    ("> ça va ?\nil a dit ça hier", "None"), ("elle a dit \"bonne nuit\" à 20h", "None"),
    ("syncs", "None"), ("syncs ?", "AreYouThere"), ("syncs, 😂", "Laughter"), ("syncs a raison", "YouAreRight"),
    ("syncs, organise une session", "Request"), ("merci syncs", "Thanks"), ("allez salut syncs", "Goodbye"),
})
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
}
ShipIs("Tu penses quoi de <@111> et <@222> ?", "<@111>", "<@222>");
ShipIs("<@999> <@111> et <@222> ça match ?", "<@111>", "<@222>");
ShipIs("Luca et moi, ça match syncs ? 🙂", "Luca", "moi");
if (Opinion.Read("t'en penses quoi de <@111> et <@222> ?") is { } pairOpinion && pairOpinion.Subject == "et")
    Fail("an opinion of two tags still reads « et »");
if (Opinion.Read("t'en penses quoi de la pizza ? 🙂")?.Subject != "pizza") Fail($"trailing emoji kept in the subject: {Opinion.Read("t'en penses quoi de la pizza ? 🙂")?.Subject}");
if (Opinion.Read("t'en penses quoi de la pizza syncs ?")?.Subject != "pizza") Fail("trailing name kept in the subject");

// 18. Called a liar: Disbelief, even though « menteuse » reads as mean on its own.
foreach (var (text, expected) in new[]
{
    ("t'es une menteuse", "Disbelief"), ("tu mens", "Disbelief"), ("pourquoi tu mens ?", "Disbelief"), ("menteuse !", "Disbelief"),
    ("sale menteuse", "Disbelief"), ("t'as menti", "Disbelief"), ("arrête de mentir", "Disbelief"), ("mytho", "Disbelief"),
    ("t'es une mytho", "Disbelief"), ("c'est du pipeau", "Disbelief"), ("tu racontes des bobards", "Disbelief"),
    ("syncs, t'es une menteuse", "Disbelief"), ("<@999> tu mens", "Disbelief"), ("non, t'es une menteuse", "Disbelief"),
    ("tu mens pas", "None"), ("t'es pas une menteuse", "None"), ("il ment tout le temps", "None"), ("il est un menteur", "None"),
    ("Luca est un vrai mytho", "None"), ("t'es une menteuse et une idiote en plus", "None"),
    ("le mensonge c'est mal", "None"),
})
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
}

// The mood still reads « menteuse » as mean (ReactionService, the backbite roast); ChatterService
// lets Disbelief through only when the intent says so. Pin both halves.
var analyze = cues.GetMethod("Analyze", BindingFlags.Public | BindingFlags.Static)!;
string Mood(string s) { var m = analyze.Invoke(null, new object[] { s })!; return m.GetType().GetProperty("Emotion")!.GetValue(m)!.ToString()!; }
if (Mood("t'es une menteuse") != "Mean") Fail("« t'es une menteuse » no longer reads mean: ChatterService.IsMean relies on it");
if (Mood("t'es une menteuse et une idiote en plus") != "Mean") Fail("a long insult with « menteuse » no longer reads mean");
// A hush is not an insult: a mean mood reads no intent, so « tais-toi » would be roasted, not obeyed.
foreach (var hush in new[] { "tais-toi", "TAIS-TOI !", "tais-toi syncs", "oh tais-toi un peu", "taisez-vous", "chut !", "la ferme", "ferme-la", "ferme ta bouche" })
    if (Mood(hush) == "Mean") Fail($"« {hush} » reads mean: it must reach the ShutUp intent");
if (Mood("tais-toi, t'es nulle") != "Mean") Fail("« tais-toi, t'es nulle » no longer reads mean: the insult beside the hush must still count");
if ((bool)addresses.Invoke(null, new object[] { "syncs est une menteuse", 999UL })!) Fail("« syncs est une menteuse » reads as said to her (it is a backbite)");

// 19. The weakest intents widened: AreYouThere, AboutHer, Crush, Help, Disbelief, and the game talk
// each new cue must leave alone.
foreach (var (text, expected) in new[]
{
    // AreYouThere: her silence noticed.
    ("tu m'ignores ?", "AreYouThere"), ("tu m'ignores maintenant ?", "AreYouThere"), ("tu réponds plus ?", "AreYouThere"),
    ("tu me réponds pas ?", "AreYouThere"), ("pourquoi tu me réponds pas ?", "AreYouThere"), ("réponds-moi", "AreYouThere"),
    ("t'es morte ?", "AreYouThere"), ("t'es morte ou quoi", "AreYouThere"), ("t'es plus là ?", "AreYouThere"),
    ("tu réponds pas à ma question ?", "Question"), ("t'es morte de rire ?", "Laughter"),
    // AboutHer: her taste asked with « quel », hobbies and passions.
    ("t'aimes quel jeu ?", "AboutHer"), ("tu aimes quelle musique ?", "AboutHer"), ("tu kiffes quel genre de film ?", "AboutHer"),
    ("tu préfères quoi comme musique ?", "AboutHer"), ("t'as des hobbies ?", "AboutHer"), ("tu as des passions ?", "AboutHer"),
    ("c'est quoi tes passions ?", "AboutHer"),
    ("tu préfères quel jeu, Minecraft ou Fortnite ?", "Opinion"), ("tu préfères Minecraft ou Fortnite ?", "Opinion"),
    // Crush: asked short, closing the message.
    ("tu kiffes qui ?", "Crush"), ("t'as quelqu'un ?", "Crush"), ("t'aimes bien qui sur le serveur ?", "Crush"),
    ("tu aimes qui syncs ?", "Crush"), ("t'aimes qui toi ?", "Crush"), ("t'as quelqu'un en vue ?", "Crush"),
    ("qui te fait craquer ?", "Crush"),
    ("tu aimes qui comme perso ?", "Question"), ("tu kiffes qui en jungle ?", "Question"),
    ("t'as quelqu'un qui peut me carry ?", "Question"), ("tu aimes qui dans ce jeu ?", "Question"),
    // Help: how to use one of her features, named.
    ("comment on fait un sondage ?", "Help"), ("comment je fais pour créer une session ?", "Help"),
    ("comment lancer un giveaway ?", "Help"), ("comment on met un rappel ?", "Help"), ("comment faire un quiz ?", "Help"),
    ("t'as des commandes ?", "Help"), ("je sais pas comment t'utiliser", "Help"),
    ("comment on fait pour finir le raid ?", "Question"), ("comment je fais une partie classée ?", "Question"),
    ("comment on fait des équipes sur Fortnite ?", "Question"), ("comment je fais pour changer d'équipe ?", "Question"),
    // Disbelief: doubt, asked.
    ("t'es sûre ?", "Disbelief"), ("mais t'es sûre ?", "Disbelief"), ("tu es sûre de toi ?", "Disbelief"), ("t'es sûr ?", "Disbelief"),
    ("sérieux ?", "Disbelief"), ("sérieusement ?", "Disbelief"), ("sérieux quoi ?", "Disbelief"), ("srx ?", "Disbelief"),
    ("ça m'étonnerait", "Disbelief"), ("j'en doute", "Disbelief"), ("j'en doute fort", "Disbelief"),
    ("t'es sûre que tu viens ?", "Question"), ("t'es sûre ou pas pour ce soir ?", "Question"), ("il est sérieux ?", "Question"),
    ("sérieux, le boss a 3 phases ?", "Question"), ("sérieux il a encore raté", "None"), ("t'es sur quoi ?", "Question"),
    ("t'es sur le serveur ?", "Question"), ("ça m'étonnerait pas", "None"), ("ça m'étonnerait qu'il vienne", "None"),
    ("j'en doute pas", "None"),
})
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
}
var preferQuel = Opinion.Read("tu préfères quel jeu, Minecraft ou Fortnite ?");
if (preferQuel?.Subject != "Minecraft" || preferQuel?.Other != "Fortnite") Fail($"« tu préfères quel jeu, X ou Y » sides: ({preferQuel?.Subject}, {preferQuel?.Other})");
if (Opinion.Read("tu aimes qui comme perso ?") is not null) Fail("« tu aimes qui comme perso ? » rates « qui comme perso »");
foreach (var (text, topic) in new[] { ("comment on fait un sondage ?", "Poll"), ("comment je fais pour créer une session ?", "Session"), ("comment on met un rappel ?", "Reminder") })
    if (readTopicAny.Invoke(null, new object[] { text })!.ToString() != topic) Fail($"help topic: \"{text}\" != {topic}");

// 20. Down, ShouldI, Joke, FunFact, Blame, and missing her (Love), with the chat each must leave alone.
foreach (var (text, expected) in new[]
{
    // Down: about themselves, said.
    ("je suis triste", "Down"), ("je suis trop triste", "Down"), ("je me sens vraiment mal", "Down"), ("jsuis deg", "Down"),
    ("je suis tellement déprimée", "Down"), ("j'ai passé une sale journée", "Down"), ("journée de merde", "Down"),
    ("ça va pas fort", "Down"), ("ça va pas du tout", "Down"), ("ça va pas fort en ce moment", "Down"),
    ("je pleure", "None"), ("ça va pas du tout ce patch", "None"), ("j'en peux plus", "Down"), ("j'en ai marre de ma vie", "Down"),
    ("j'ai pas le moral", "Down"), ("coup de mou", "Down"), ("je suis à bout", "Down"), ("je suis au bout du rouleau", "Down"),
    ("je me sens seule", "Down"), ("j'ai envie de pleurer", "Down"), ("je craque", "Down"), ("j'ai le seum", "Down"),
    ("journée de merde, je vais dormir", "Down"), ("syncs, je suis triste", "Down"), ("je suis nul", "Down"),
    ("je suis pas triste", "Sharing"), ("j'en peux plus mdr", "Laughter"), ("je pleure de rire", "Laughter"), ("j'en peux plus 😂", "Laughter"),
    ("ça va pas la tête ?", "Question"), ("ça va pas ce soir, désolé", "None"), ("je craque pour ce skin", "Sharing"),
    ("je suis seul en vocal", "Sharing"), ("je suis mal placé sur la map", "Sharing"), ("t'as passé une sale journée ?", "Question"),
    ("il y a un cafard dans la cuisine", "None"), ("il a le seum", "None"), ("je suis pas mal", "Sharing"),
    ("c'est triste ce film", "None"),
    // ShouldI: yes or no, or a choice.
    ("je devrais aller dormir ?", "ShouldI"), ("est-ce que je devrais acheter ce jeu ?", "ShouldI"), ("je l'achète ?", "ShouldI"),
    ("tu crois que je devrais lui écrire ?", "ShouldI"), ("oui ou non ?", "ShouldI"), ("j'y vais ou pas ?", "ShouldI"),
    ("on le fait ou pas ?", "ShouldI"), ("je joue ou je dors ?", "ShouldI"), ("Valo ou LoL ?", "ShouldI"),
    ("pizza ou sushi ?", "ShouldI"), ("pile ou face ?", "ShouldI"), ("pile ou face", "ShouldI"), ("syncs, samedi ou dimanche ?", "ShouldI"),
    ("je devrais dormir", "Sharing"), ("je lui dis quoi ?", "Question"), ("je le fais comment ?", "Question"),
    ("tu viens ou pas ?", "Question"), ("tu dors ou quoi", "AreYouThere"), ("c'est vrai ou faux ?", "Question"),
    ("tu préfères Valo ou LoL ?", "Opinion"), ("on se fait un Valo ou un LoL ce soir avec les autres ?", "Question"),
    ("ça va ou quoi ?", "HowAreYou"), ("je devrais faire quoi ?", "Question"),
    // Joke and FunFact.
    ("raconte une blague", "Joke"), ("raconte-moi une blague", "Joke"), ("fais-moi rire", "Joke"), ("une blague ?", "Joke"),
    ("t'as une blague ?", "Joke"), ("tu peux me raconter une blague ?", "Joke"), ("tu connais des blagues ?", "Joke"),
    ("c'est une blague ?", "Question"), ("sans blague", "None"), ("pas de blague ce soir", "None"),
    ("un fun fact ?", "FunFact"), ("dis-moi un truc", "FunFact"), ("raconte-moi quelque chose", "FunFact"),
    ("apprends-moi un truc", "FunFact"), ("dis moi un truc intéressant", "FunFact"),
    ("dis-moi un truc, tu viens ce soir ?", "Question"), ("dis quelque chose à Luca de ma part", "None"),
    // Blame.
    ("c'est la faute de qui ?", "Blame"), ("c'est qui le coupable ?", "Blame"), ("à qui la faute ?", "Blame"),
    ("c'est la faute de qui", "Blame"), ("c'est la faute à qui cette défaite ?", "Blame"),
    ("c'est la faute de Luca", "None"), ("c'est de ta faute", "None"),
    // Missing her is Love.
    ("tu m'as manqué", "Love"), ("tu me manques", "Love"), ("t'étais où, tu m'as manqué", "Love"), ("tu me manques pas", "None"),
})
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
}
var readChoice = cues.GetMethod("ReadChoice", BindingFlags.Public | BindingFlags.Static)!;
foreach (var (text, a, b) in new[] { ("je joue ou je dors ?", "je joue", "je dors"), ("Valo ou LoL ? 🙂", "Valo", "LoL"), ("pile ou face", "pile", "face"), ("syncs, pizza ou sushi ?", "pizza", "sushi") })
{
    var got = readChoice.Invoke(null, new object[] { text });
    var expectedPair = ((string, string)?)(a, b);
    if (!Equals(got, expectedPair)) Fail($"ReadChoice(\"{text}\") = {got}, expected ({a}, {b})");
}
var speaks = cues.GetMethod("SpeaksToSomeone", BindingFlags.Public | BindingFlags.Static)!;
if ((bool)speaks.Invoke(null, new object[] { "journée de merde" })!) Fail("« journée de merde » speaks to someone");
if (!(bool)speaks.Invoke(null, new object[] { "j'en ai marre de toi" })!) Fail("« j'en ai marre de toi » speaks to nobody");
// The pools ChatterService formats the /blame lines into keep {0} out of them.
foreach (var f in new[] { "BlameLines", "BlameSelfLines", "BlameQuokkaLines", "BlamePapaDodgeLines", "BlamePapaDodgeQuokkaLines", "BlameNobodyLines" })
    foreach (var l in (string[])Field(f))
        try { string.Format(l.Replace("{0}", "{2}"), "Nom", "lundi", "<@1>", ""); } catch (FormatException) { Fail($"{f}: {l}"); }
if (!((string[])Field("BlamePapaDodgeLines")).Any(l => l.StartsWith("~~"))) Fail("no struck-through Papa dodge line left for chat");
if (!((string[])Field("BlamePapaDodgeQuokkaLines")).Any(l => l.StartsWith("~~"))) Fail("no struck-through Papa-Quokka dodge line left for chat");

// 21. Yes / No under her questions, « tu m'aimes ? », « tu te souviens de moi ? », « t'es fâchée ? »,
// with the chat each must leave alone.
foreach (var (text, expected) in new[]
{
    // DoYouLoveMe.
    ("tu m'aimes ?", "DoYouLoveMe"), ("tu m'aimes", "DoYouLoveMe"), ("tu m'aimes pas ?", "DoYouLoveMe"),
    ("tu m'aimes un peu ?", "DoYouLoveMe"), ("est-ce que tu m'aimes ?", "DoYouLoveMe"), ("tu me détestes ou quoi ?", "DoYouLoveMe"),
    ("tu m'en veux encore ?", "DoYouLoveMe"), ("tu me kiffes ?", "DoYouLoveMe"), ("tu m'apprécies ?", "DoYouLoveMe"),
    ("je suis ta préférée ?", "DoYouLoveMe"), ("tu me supportes plus ?", "DoYouLoveMe"),
    ("je t'aime, tu m'aimes ?", "Love"), ("désolé, tu m'en veux ?", "Apology"), ("tu m'en veux si je pars ?", "Question"),
    ("tu me supportes en bot lane ?", "Question"), ("il m'aime pas", "None"), ("tu m'aimes plus que Luca ?", "DoYouLoveMe"),
    // RememberMe.
    ("tu te souviens de moi ?", "RememberMe"), ("tu te souviens pas de moi ?", "RememberMe"), ("tu te rappelles de moi", "RememberMe"),
    ("comment je m'appelle ?", "RememberMe"), ("comment je m'appelle déjà ?", "RememberMe"), ("c'est quoi mon prénom ?", "RememberMe"),
    ("tu me reconnais ?", "RememberMe"), ("tu sais qui je suis ?", "RememberMe"), ("je suis qui ?", "RememberMe"),
    ("tu m'as oublié ?", "RememberMe"), ("qui suis-je ?", "RememberMe"), ("tu connais mon nom ?", "RememberMe"),
    ("tu me connais, je suis toujours en retard", "None"), ("je suis qui pour juger", "Sharing"),
    ("tu te souviens de moi en vocal hier ?", "Question"), ("tu sais qui je suis allé voir ?", "Question"),
    ("c'est quoi mon pseudo sur Steam ?", "Question"), ("comment tu t'appelles ?", "WhoAreYou"), ("c'est quoi ton nom ?", "WhoAreYou"),
    ("tu m'as oublié pour la session ?", "Question"),
    // AreYouUpset.
    ("t'es fâchée ?", "AreYouUpset"), ("t'es fâché ?", "AreYouUpset"), ("t'es fâchée contre moi ?", "AreYouUpset"),
    ("tu boudes ?", "AreYouUpset"), ("tu boudes encore ?", "AreYouUpset"), ("t'es pas fâchée ?", "AreYouUpset"),
    ("t'es jalouse de lui ?", "AreYouUpset"), ("t'es triste là ?", "AreYouUpset"), ("t'es en colère ?", "AreYouUpset"),
    ("t'es vexée ?", "AreYouUpset"), ("ça va ? t'es fâchée ?", "HowAreYou"),
    ("t'es fâchée", "None"), ("t'es triste ce soir ou tu viens ?", "Question"),
})
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
}

var readAnswer = cues.GetMethod("ReadAnswer", BindingFlags.Public | BindingFlags.Static)!;
foreach (var (text, expected) in new[]
{
    ("oui", "Yes"), ("ouiii", "Yes"), ("ouais", "Yes"), ("bah oui", "Yes"), ("oui oui", "Yes"), ("carrément", "Yes"),
    ("grave", "Yes"), ("bien sûr", "Yes"), ("évidemment !", "Yes"), ("oui syncs", "Yes"), ("<@999> oui", "Yes"), ("oui 😂", "Yes"),
    ("tout à fait", "Yes"), ("si", "Yes"), ("oui merci", "Yes"), ("yes", "Yes"),
    ("non", "No"), ("nan", "No"), ("nooon", "No"), ("bah non", "No"), ("mais non", "No"), ("non non non", "No"),
    ("pas du tout", "No"), ("jamais", "No"), ("nope", "No"), ("non merci", "No"), ("absolument pas", "No"),
    ("oui ?", ""), ("non ?", ""), ("oui et non", ""), ("oui mais demain", ""), ("ok", ""), ("peut-être", ""),
    ("exactement", ""), ("merci", ""), ("oui je viens ce soir à la session", ""), ("", ""),
})
{
    var got = readAnswer.Invoke(null, new object[] { text })?.ToString() ?? "";
    if (got != expected) Fail($"ReadAnswer(\"{text}\") = {(got == "" ? "null" : got)}, expected {(expected == "" ? "null" : expected)}");
}

// The records pools: every plain RememberMe line and every opener says the name (the answer to
// « comment je m'appelle ? »); the openers carry no tag, the fact line after them does; the grudge
// quotes its count, the treasure never shows one.
foreach (var l in (string[])((IDictionary)Field("IntentLines"))[Enum.Parse(intentType, "RememberMe")]!)
    if (!l.Contains("{0}")) Fail($"IntentLines[RememberMe] doesn't say the name: {l}");
foreach (var l in (string[])Field("RememberMeOpeners"))
{
    if (!l.Contains("{0}")) Fail($"RememberMeOpeners doesn't say the name: {l}");
    try
    {
        var s = string.Format(l, "Nom");
        if (Sticker.Strip(s) != s) Fail($"RememberMeOpeners is tagged: {l}");
    }
    catch (FormatException) { Fail($"RememberMeOpeners: format: {l}"); }
}
foreach (var l in (string[])Field("DoYouLoveMeGrudgeLines"))
    if (!l.Contains("{2}")) Fail($"DoYouLoveMeGrudgeLines doesn't quote the count: {l}");
foreach (var l in (string[])Field("DoYouLoveMeTreasureLines"))
    if (l.Contains("{2}") || l.Replace("{0}", "").Replace("{1}", "").Any(char.IsDigit)) Fail($"DoYouLoveMeTreasureLines shows a number: {l}");

// 21b. The schedule, their numbers, dice, « chut », « qui a raison ? », « note-moi », « excuse-toi »,
// with the chat each must leave alone.
foreach (var (text, expected) in new[]
{
    // DemandApology, read before Apology: « dis pardon » asks for one.
    ("excuse-toi", "DemandApology"), ("excuses-toi syncs", "DemandApology"), ("dis pardon", "DemandApology"),
    ("dis désolée", "DemandApology"), ("tu me dois des excuses", "DemandApology"), ("j'attends tes excuses", "DemandApology"),
    ("tu t'excuses ?", "DemandApology"), ("tu t'excuses pas ?", "DemandApology"),
    ("excuse-moi", "Apology"), ("pardon", "Apology"), ("je m'excuse", "Apology"), ("c'est une excuse bidon", "None"),
    ("pas besoin de t'excuser", "None"),
    // ShutUp: short, or the whole message.
    ("chut", "ShutUp"), ("chuuut", "ShutUp"), ("chut syncs", "ShutUp"), ("tais-toi", "ShutUp"), ("oh tais-toi un peu", "ShutUp"),
    ("silence", "ShutUp"), ("la ferme", "ShutUp"), ("ferme-la", "ShutUp"), ("arrête de parler", "ShutUp"),
    ("laisse-nous tranquille", "ShutUp"), ("on t'a rien demandé", "ShutUp"), ("shut up", "ShutUp"),
    ("chut il dort", "None"), ("ferme la porte", "None"), ("reste à la ferme", "None"), ("silence radio depuis hier", "None"),
    ("ta gueule", "None"),
    // MyStats: asked.
    ("je suis quel niveau ?", "MyStats"), ("je suis quel niveau", "MyStats"), ("j'ai combien de cailloux ?", "MyStats"),
    ("combien j'ai de cailloux", "MyStats"), ("comment va mon plynling ?", "MyStats"), ("mon niveau ?", "MyStats"),
    ("mes cailloux ?", "MyStats"), ("mes stats ?", "MyStats"), ("c'est quoi mon rang ?", "MyStats"), ("je suis riche ?", "MyStats"),
    ("mon plynling a faim", "Sharing"), ("j'ai gagné 200 cailloux", "Sharing"), ("le niveau 3 est horrible", "None"),
    ("t'as combien de niveaux ?", "Question"), ("je suis riche", "Sharing"), ("t'as vu mon niveau en ranked ?", "Question"),
    // TakeSides.
    ("qui a raison, moi ou Luca ?", "TakeSides"), ("qui a raison entre Luca et Sandra ?", "TakeSides"),
    ("moi ou <@1>, qui a raison ?", "TakeSides"), ("qui a raison ?", "TakeSides"), ("<@1> qui a raison ?", "TakeSides"),
    ("dis-lui que j'ai raison", "TakeSides"), ("dis-lui qu'il a tort", "TakeSides"), ("c'est qui qui a raison ?", "TakeSides"),
    ("t'as raison", "YouAreRight"), ("elle a raison", "YouAreRight"), ("qui a raison et qui a tort ?", "Question"),
    ("qui a raison dans ce débat ?", "Question"),
    // RateMe: asked, the adjective closing.
    ("note-moi", "RateMe"), ("tu me notes ?", "RateMe"), ("je vaux combien ?", "RateMe"), ("je suis beau ?", "RateMe"),
    ("je suis belle ?", "RateMe"), ("je suis vraiment mignonne ?", "RateMe"), ("tu me trouves comment ?", "RateMe"),
    ("tu me trouves drôle ?", "RateMe"), ("je suis comment ?", "RateMe"),
    ("je suis nul ?", "Down"), ("je suis beau", "Sharing"), ("je suis beau gosse en jeu ?", "Question"), ("tu me trouves où ?", "Question"),
    ("tu penses quoi de moi ?", "Opinion"),
    // NextSession: asked, before ShouldI and Request.
    ("c'est quand la prochaine session ?", "NextSession"), ("c'est quand la prochaine session", "NextSession"),
    ("y a une session ce soir ?", "NextSession"), ("on joue ce soir ?", "NextSession"), ("on joue quand ?", "NextSession"),
    ("qui vient ce soir ?", "NextSession"), ("y a quoi de prévu ?", "NextSession"), ("on joue ce soir ou pas ?", "NextSession"),
    ("tu peux me dire c'est quand la prochaine session ?", "NextSession"), ("y a pas de session ce soir ?", "NextSession"),
    ("on lance une game ce soir ?", "Question"), ("on joue à quoi ce soir", "None"), ("tu viens ce soir ?", "Question"),
    ("qui est chaud pour un raid ?", "Question"), ("la prochaine game tu joues support ?", "Question"), ("on joue ce soir", "None"),
    ("tu peux organiser une session ?", "Request"), ("la prochaine session je viens", "None"),
    // Roll.
    ("lance un dé", "Roll"), ("lance un dé stp", "Roll"), ("jette deux dés", "Roll"), ("lance les dés", "Roll"), ("1d20", "Roll"),
    ("roll 2d6", "Roll"), ("d20 ?", "Roll"), ("tu peux lancer un dé ?", "Roll"), ("choisis un nombre entre 1 et 10", "Roll"),
    ("un nombre entre 1 et 100 ?", "Roll"), ("donne-moi un chiffre au hasard", "Roll"), ("lance un dé à 20 faces", "Roll"),
    ("j'ai lancé un dé", "Sharing"), ("lance une partie de Valo", "Request"), ("on fait du d4 ce soir ?", "Question"),
    ("le nombre de joueurs entre 4 et 8", "None"), ("donne-moi le nombre de joueurs", "None"), ("un nombre de joueurs limité", "None"),
    ("lance un dé de tes sorts", "None"),

    // Refined: wider forms, and the chat each must leave alone.
    ("tu peux t'excuser ?", "DemandApology"), ("je veux que tu t'excuses", "DemandApology"), ("et mes excuses ?", "DemandApology"),
    ("des excuses ?", "DemandApology"), ("dis que t'es désolée", "DemandApology"), ("tu dois me demander pardon", "DemandApology"),
    // Not asked of her: the « pardon » in them reads as the plain Apology it always did.
    ("je lui dis pardon", "Apology"), ("je demande pardon", "Apology"), ("il m'a demandé pardon", "Apology"), ("toutes mes excuses", "Apology"),
    ("ferme ta bouche", "ShutUp"), ("arrête de jacasser", "ShutUp"), ("mute-toi", "ShutUp"), ("boucle-la", "ShutUp"),
    ("tu parles trop", "ShutUp"), ("tu parles trop syncs", "ShutUp"), ("laisse-nous", "ShutUp"), ("silence s'il te plaît", "ShutUp"),
    ("un peu de silence", "ShutUp"), ("tu parles trop vite", "None"), ("boucle la ceinture", "None"),
    ("je suis level combien ?", "MyStats"), ("mon level ?", "MyStats"), ("j'ai combien de sous ?", "MyStats"), ("mes sous ?", "MyStats"),
    ("mon plynling est vivant ?", "MyStats"), ("il devient quoi mon plynling ?", "MyStats"), ("donne-moi mes stats", "MyStats"),
    ("c'est quoi ma place au classement ?", "MyStats"), ("il me reste combien de temps ?", "Question"),
    ("tranche entre moi et Luca", "TakeSides"), ("départage-nous", "TakeSides"), ("à ton avis, qui a raison ?", "TakeSides"),
    ("qui a raison selon toi, moi ou Luca ?", "TakeSides"), ("moi ou Luca, qui a raison selon toi ?", "TakeSides"),
    ("qui a le plus raison entre Luca et Sandra ?", "TakeSides"), ("une tranche de pizza", "None"),
    ("évalue-moi", "RateMe"), ("tu me mettrais combien ?", "RateMe"), ("je suis jolie ?", "RateMe"), ("je suis chiant ?", "RateMe"),
    ("je suis beau en vrai ?", "RateMe"), ("je suis comment physiquement ?", "RateMe"), ("je suis fort en maths ?", "Question"),
    ("je suis fou de joie", "Sharing"),
    ("c'est quand la soirée ?", "NextSession"), ("session ce soir ?", "NextSession"), ("c'est prévu pour quand ?", "NextSession"),
    ("on rejoue quand ?", "NextSession"), ("qui sera là ce soir ?", "NextSession"), ("y a du monde ce soir ?", "NextSession"),
    ("un jet de d20", "Roll"), ("fais un jet de dés", "Roll"), ("random entre 1 et 100", "Roll"), ("lancez les dés", "Roll"),
    ("fais un jet de sauvegarde", "None"), ("le jet d'eau est cassé", "None"),
})
{
    var got = readIntent.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
}

var readMyStats = cues.GetMethod("ReadMyStatsTopic", BindingFlags.Public | BindingFlags.Static, [typeof(string)])!;
foreach (var (text, topic) in new[]
{
    ("je suis quel niveau ?", MyStatsTopic.Level), ("c'est quoi mon rang ?", MyStatsTopic.Level), ("mon xp ?", MyStatsTopic.Level),
    ("j'ai combien de cailloux ?", MyStatsTopic.Balance), ("mes cailloux ?", MyStatsTopic.Balance),
    ("comment va mon plynling ?", MyStatsTopic.Plynling), ("mes stats ?", MyStatsTopic.All),
})
    if (!Equals(readMyStats.Invoke(null, new object[] { text }), topic)) Fail($"ReadMyStatsTopic(\"{text}\") != {topic}");

// Dice: what is read, what is rolled, and the caps.
foreach (var (text, count, faces) in new[] { ("1d20", 1, 20), ("lance les dés", 2, 6), ("jette 3 dés à 20 faces", 3, 20), ("roll d8", 1, 8) })
    if (Dice.Read(text) is not { IsRange: false } d || d.Count != count || d.Faces != faces) Fail($"Dice.Read(\"{text}\") = {Dice.Read(text)}");
if (Dice.Read("choisis un nombre entre 10 et 1") is not { IsRange: true, Min: 1, Max: 10 }) Fail("a reversed range is not 1..10");
if (Dice.Read("un nombre au hasard") is not { IsRange: true, Min: 1, Max: 100 }) Fail("an open range is not 1..100");
foreach (var text in new[] { "25d6", "1d1", "lance 0 dé", "1d5000" })
    if (Dice.Read(text) is not null) Fail($"Dice.Read(\"{text}\") rolls past the caps");
{
    var diceRng = new Random(3);
    for (var i = 0; i < 200; i++)
    {
        var range = Dice.Roll(new DiceAsk(0, 0, 1, 6, true), diceRng);
        if (!int.TryParse(range.Result, out var n) || n is < 1 or > 6) Fail($"range roll out of bounds: {range.Result}");
    }
    var two = Dice.Roll(new DiceAsk(2, 6, 0, 0, false), diceRng);
    if (two.Spec != "2d6" || !System.Text.RegularExpressions.Regex.IsMatch(two.Result, @"^[1-6] \+ [1-6] = \d+$")) Fail($"2d6 rolled as {two}");
    if (Dice.Roll(new DiceAsk(0, 0, 1, 100, true), diceRng).Spec != "un nombre entre 1 et 100") Fail("range spec wording");
}

// TakeSides: the sides as written, or the asker alone.
foreach (var (text, a, b) in new (string, string, string?)[]
{
    ("qui a raison, moi ou Luca ?", "moi", "Luca"), ("qui a raison entre Luca et Sandra ?", "Luca", "Sandra"),
    ("moi ou <@1>, qui a raison ?", "moi", "<@1>"), ("syncs, qui a raison ? 🙂", "moi", null), ("dis-lui qu'il a tort", "moi", null),
})
    if (TakeSides.Read(text) is not { } s || s.A != a || s.B != b) Fail($"TakeSides.Read(\"{text}\") = {TakeSides.Read(text)}");

// SessionRoster: the count and the names in finished words.
foreach (var (names, maybes, expected) in new (string[], int, string)[]
{
    ([], 0, "aucune inscription"), (["Luca"], 0, "1 inscription (Luca)"), (["Luca"], 1, "1 inscription (Luca), plus un « peut-être »"),
    (["A", "B", "C"], 2, "3 inscriptions (A, B et C), plus 2 « peut-être »"),
    (["A", "B", "C", "D", "E", "F", "G"], 0, "7 inscriptions (A, B, C, D, E et 2 autres)"),
    (["A", "B", "C", "D", "E", "F"], 0, "6 inscriptions (A, B, C, D, E et 1 autre)"),
})
    if (SessionRoster.Describe(names, maybes) != expected) Fail($"SessionRoster = {SessionRoster.Describe(names, maybes)}, expected {expected}");

// MyStats: each topic's words, and nothing to say when there is nothing.
{
    RecapStats Mine(long xp, int? level, int? rank, long balance, string? plyn, TimeSpan? age) =>
        new(StatsPeriod.AllTime, xp, level, rank, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, plyn, age, 0, balance);
    var full = Mine(900, 12, 4, 1250, "*Pix*", TimeSpan.FromDays(12.5));
    if (MyStats.Describe(full, MyStatsTopic.Level) is not ("niveau 12", "n° 4 du classement")) Fail($"level: {MyStats.Describe(full, MyStatsTopic.Level)}");
    if (MyStats.Describe(full, MyStatsTopic.Balance)?.A != 1250.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("fr-FR")) + " cailloux")
        Fail($"balance: {MyStats.Describe(full, MyStatsTopic.Balance)}");
    if (MyStats.Describe(full, MyStatsTopic.Plynling) is not { } p || p.A != Discord.Format.Sanitize("*Pix*") || p.B != "12 jours")
        Fail($"plynling: {MyStats.Describe(full, MyStatsTopic.Plynling)}");
    var blank = Mine(0, null, null, 0, null, null);
    if (MyStats.Describe(blank, MyStatsTopic.Level) is not null) Fail("no XP still has a level to show");
    if (MyStats.Describe(blank, MyStatsTopic.Plynling) is not null) Fail("no Plynling still has one to show");
    if (MyStats.Describe(blank, MyStatsTopic.Balance)?.A != "zéro caillou") Fail("an empty purse is not « zéro caillou »");
    if (MyStats.Describe(blank, MyStatsTopic.All)?.A != "zéro caillou") Fail($"all, empty: {MyStats.Describe(blank, MyStatsTopic.All)}");
    if (MyStats.Describe(Mine(10, 1, null, 1, null, TimeSpan.FromHours(3)), MyStatsTopic.Level)?.B != "hors classement") Fail("unranked wording");
}
// The pools that quote the numbers say them: every SessionNextLines line both {2} and {3}, every
// TakeSidesLines line both sides, every Roll line the result.
foreach (var l in (string[])Field("SessionNextLines"))
    if (!l.Contains("{2}") || !l.Contains("{3}")) Fail($"SessionNextLines drops a placeholder: {l}");
foreach (var l in (string[])Field("TakeSidesLines"))
    if (!l.Contains("{2}") || !l.Contains("{3}")) Fail($"TakeSidesLines drops a side: {l}");
foreach (var l in (string[])((IDictionary)Field("IntentLines"))[Enum.Parse(intentType, "Roll")]!)
    if (!l.Contains("{2}") || !l.Contains("{3}")) Fail($"IntentLines[Roll] drops the roll: {l}");

// 22. Papa's pet names: which family, whether said to her, and what they must leave alone.
var readPetName = cues.GetMethod("ReadPetName", BindingFlags.Public | BindingFlags.Static)!;
foreach (var (text, expected) in new[]
{
    ("ma framboise", "Raspberry"), ("Ma Framboise, ça va ?", "Raspberry"), ("salut petite framboise", "Raspberry"),
    ("bonne nuit ma petite framboise", "Raspberry"), ("ma framboiiise ♡", "Raspberry"),
    ("ma fille", "Child"), ("bravo ma fille !", "Child"), ("ma fiiille", "Child"), ("mon enfant, viens là", "Child"),
    ("ma petite fille est passée", "None"), ("j'ai mangé des framboises", "None"), ("une framboise", "None"),
    ("mes framboises", "None"), ("ma famille", "None"), ("mon enfance", "None"), ("syncs", "None"), ("", "None"),
    ("<:ma_framboise:123> lol", "None"), ("https://example.com/ma-framboise", "None"),
})
{
    var got = readPetName.Invoke(null, new object[] { text })!.ToString();
    if (got != expected) Fail($"ReadPetName(\"{text}\") = {got}, expected {expected}");
}

var namesHerFrom = cues.GetMethod("NamesHerFrom", BindingFlags.Public | BindingFlags.Static)!;
foreach (var (text, fromOwner, expected) in new[]
{
    ("ma framboise", true, true), ("ma framboise", false, false), ("ma fille", false, false),
    ("syncs", false, true), ("syncs", true, true), ("rien à voir", true, false),
})
{
    var got = (bool)namesHerFrom.Invoke(null, new object[] { text, fromOwner })!;
    if (got != expected) Fail($"NamesHerFrom(\"{text}\", {fromOwner}) = {got}, expected {expected}");
}

var addressesByPet = cues.GetMethod("AddressesHerByPetName", BindingFlags.Public | BindingFlags.Static)!;
foreach (var (text, expected) in new[]
{
    ("ma framboise, ça va ?", true), ("ça va ma fille ?", true), ("salut ma petite framboise", true),
    ("<@999> mon enfant, t'en penses quoi ?", true), ("petite framboise tu fais quoi", true),
    ("ma fille a planté le bot", false), ("je joue avec ma fille", false), ("c'est ma fille", false),
    ("<@555> ma framboise ça va", false), ("dis à ma framboise de lancer un sondage", false),
})
{
    var got = (bool)addressesByPet.Invoke(null, new object[] { text, 999UL })!;
    if (got != expected) Fail($"AddressesHerByPetName(\"{text}\") = {got}, expected {expected}");
}

// 23. Reports (/report, the monthly digest): their windows, when the digest is due, the session
// rules shared with /recap, and every embed measured at its worst against Discord's caps.
{
    var noon = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
    void WindowIs(ReportWindow? w, int from, int to, string what)
    {
        if (w is null || w.FromDay != from || w.ToDay != to) Fail($"{what}: {w?.FromDay}..{w?.ToDay}, expected {from}..{to}");
    }
    var week = ReportWindow.From(StatsPeriod.Week, noon);
    WindowIs(week, 20261013, 20261019, "week");
    WindowIs(week.Previous, 20261006, 20261012, "week before");
    var month = ReportWindow.From(StatsPeriod.Month, noon);
    WindowIs(month, 20260920, 20261019, "30 days");
    WindowIs(month.Previous, 20260821, 20260919, "30 days before");
    if (ReportWindow.From(StatsPeriod.AllTime, noon).Previous is not null) Fail("all time has a previous window");
    var feb = ReportWindow.CalendarMonth(2026, 2);
    WindowIs(feb, 20260201, 20260228, "February");
    WindowIs(feb.Previous, 20260101, 20260131, "the month before February");
    if (feb.Label != "février 2026") Fail($"month label '{feb.Label}'");
    if (ReportWindow.CalendarMonth(2026, 10).Days(0).Count != 31) Fail("October (clocks change) is not 31 days");
    // 22:30 UTC on the 19th is already the 20th in Paris.
    if (week.Contains(new DateTimeOffset(2026, 10, 19, 22, 30, 0, TimeSpan.Zero))) Fail("window ignores the Paris day");

    void DueIs(DateTimeOffset now, int last, (int, int)? expected)
    {
        var got = MonthlyDigest.Due(now, last);
        if (got != expected) Fail($"digest due at {now:u} after {last}: {got}, expected {expected}");
    }
    DueIs(new DateTimeOffset(2026, 11, 1, 7, 30, 0, TimeSpan.Zero), 202609, null);          // 08:30 Paris: too early
    DueIs(new DateTimeOffset(2026, 11, 1, 8, 30, 0, TimeSpan.Zero), 202609, (2026, 10));
    DueIs(new DateTimeOffset(2026, 11, 1, 8, 30, 0, TimeSpan.Zero), 202610, null);          // already sent
    DueIs(new DateTimeOffset(2026, 11, 15, 20, 0, 0, TimeSpan.Zero), 202609, (2026, 10));   // she was down on the 1st
    DueIs(new DateTimeOffset(2027, 1, 1, 10, 0, 0, TimeSpan.Zero), 202611, (2026, 12));

    // One roll call: on time, late, a no-show, a « peut-être » who never came, one who declined.
    var start = new DateTimeOffset(2026, 10, 15, 19, 0, 0, TimeSpan.Zero);
    ProjectSYNCS.Models.Participant Person(ulong id, ProjectSYNCS.Models.ParticipantStatus status, int? seenAfter, int dayOffset = 0) => new()
    {
        UserId = id, Status = status, FirstSeenInVoiceAt = seenAfter is { } m ? start.AddDays(dayOffset).AddMinutes(m) : null,
    };
    ProjectSYNCS.Models.SessionEvent Session(DateTimeOffset at, bool recap, bool cancelled, params ProjectSYNCS.Models.Participant[] people) => new()
    {
        GuildId = 1, OrganizerId = 9, ScheduledAt = at, RecapSent = recap, IsCancelled = cancelled, MaxPlayers = 4,
        Category = ProjectSYNCS.Models.SessionCategory.Game, Participants = people.ToList(),
    };
    var J = ProjectSYNCS.Models.ParticipantStatus.Joined;
    var M = ProjectSYNCS.Models.ParticipantStatus.Maybe;
    var sessions = new[]
    {
        Session(start, true, false, Person(1, J, 5), Person(2, J, 20), Person(3, J, null), Person(4, M, null),
            Person(5, ProjectSYNCS.Models.ParticipantStatus.Declined, null)),
        Session(start.AddDays(1), false, true, Person(1, J, null)),       // cancelled
        Session(start.AddDays(2), false, false, Person(6, J, null)),      // no roll called: not a no-show
        Session(start.AddDays(-1), true, false, Person(7, J, 10, -1)),    // exactly +10 min: on time
        Session(noon.AddHours(5), false, false, Person(8, J, null)),      // later today: not held yet
    };
    var sr = SessionReport.Compute(sessions, week, noon);
    if (sr.Held != 3 || sr.Cancelled != 1) Fail($"sessions held {sr.Held}, cancelled {sr.Cancelled}");
    if (sr.RollCalls != 2 || sr.Expected != 4 || sr.Came != 3) Fail($"roll {sr.RollCalls}, came {sr.Came}/{sr.Expected}");
    if (sr.Missed != 1 || sr.NoShows.Single().UserId != 3) Fail($"no-shows {sr.Missed}: {string.Join(",", sr.NoShows.Select(x => x.UserId))}");
    if (sr.Late != 1 || sr.Latecomers.Single().UserId != 2 || sr.MedianLateMinutes != 20) Fail($"late {sr.Late}, median {sr.MedianLateMinutes}");
    if (sr.Maybes != 1) Fail($"maybes {sr.Maybes}");

    // Her Wednesday poll that became a session, a dead poll, and a /tonight that did too.
    const ulong botId = 42;
    var weekendPoll = new ProjectSYNCS.Models.Poll
    {
        Id = 1, OrganizerId = botId, Kind = ProjectSYNCS.Models.PollKind.DateSlots, IsClosed = true, CreatedAt = start.AddDays(-2),
        Options = { new() { ScheduledAt = start, Votes = { new() { UserId = 1 }, new() { UserId = 2 } } } },
    };
    var deadPoll = new ProjectSYNCS.Models.Poll { Id = 2, OrganizerId = 1, Kind = ProjectSYNCS.Models.PollKind.Text, IsClosed = true, CreatedAt = start, Options = { new() } };
    var call = new ProjectSYNCS.Models.TonightCall
    {
        OrganizerId = 1, CreatedAt = start, ThresholdAnnounced = true, SessionEventId = 5,
        Responses = { new() { UserId = 1, Status = ProjectSYNCS.Models.TonightStatus.In }, new() { UserId = 2, Status = ProjectSYNCS.Models.TonightStatus.In } },
    };
    var pr = PollReport.Compute(new[] { weekendPoll, deadPoll }, new[] { call }, sessions, week, botId);
    if (pr.WeekendPosted != 1 || pr.WeekendWon != 1 || pr.WeekendBecameSession != 1) Fail($"weekend poll {pr.WeekendPosted}/{pr.WeekendWon}/{pr.WeekendBecameSession}");
    if (pr.Dead != 1) Fail($"dead polls {pr.Dead}");
    if (pr.TonightReached != 1 || pr.TonightBecameSession != 1 || pr.Keen.Single().UserId != 2) Fail("tonight tallies (the organizer must not count as keen)");

    // Worst case for every embed: full lists of the longest ids, long emote markup, both windows.
    const ulong big = 1_234_567_890_123_456_789;
    var people = Enumerable.Range(0, SessionReport.ListSize).Select(i => new PersonTally(big + (ulong)i, 999, 9999)).ToList();
    var slots = Enumerable.Range(0, 7).Select(i => new SlotTally(i, 99, 999, 999)).ToList();
    var sFull = new SessionReportData(999, 999,
        Enum.GetValues<ProjectSYNCS.Models.SessionCategory>().Select(c => (c, 999)).ToList(),
        9999, 9999, 0.87, 999, 9999, 9999, 999, 999, 123, slots, slots.Take(5).ToList(), people, people, people, people);
    var pFull = new PollReportData(999, 999, 99, 99, 12.5, 9, 9, 9, 999, 999, 999, 7.5, people, people);
    var xpBy = Enumerable.Range(0, 30).ToDictionary(i => big + (ulong)i, i => 1_000_000L + i);
    var emotes = Enumerable.Range(0, CommunityReport.EmoteCount)
        .Select(i => ($"<a:a_very_long_custom_emote_name_{i}:{big}>", 9_999_999L)).ToList();
    var cFull = new CommunityReportData(999, Enumerable.Repeat(50L, 30).ToList(), 99_999_999, 9_999_999, 9_999_999, 999, 999,
        emotes, 99, 99, 99, xpBy);
    var cFading = cFull with { XpByUser = new Dictionary<ulong, long>() };
    var outageKinds = Enum.GetValues<ProjectSYNCS.Models.UptimeEventKind>().Select(k => (k, 999)).ToList();
    var yFull = new SyncsReportData(30, 65_432, 81_234, 20261015, 9999, Enumerable.Repeat(70_000L, 30).ToList(), outageKinds,
        TimeSpan.FromHours(99), 0.987, 9_999_999, 999_999, 99_999, 99_999, 9_999_999, people);

    void Measure(string what, Discord.Embed embed, bool names)
    {
        if (embed.Length > 6000) Fail($"{what} embed is {embed.Length} characters");
        if (embed.Fields.Any(f => f.Value.Length > 1024 || f.Value.Length == 0)) Fail($"a {what} field is empty or exceeds 1024");
        if (embed.Fields.Length > 25) Fail($"{what} has {embed.Fields.Length} fields");
        if (!names && (embed.Description + string.Concat(embed.Fields.Select(f => f.Value))).Contains("<@")) Fail($"{what} names someone without names");
    }
    var digest = ReportWindow.CalendarMonth(2026, 10);
    foreach (var names in new[] { true, false })
    foreach (var w in new[] { month, digest, ReportWindow.From(StatsPeriod.AllTime, noon) })
    {
        Measure("sessions", ProjectSYNCS.Commands.ReportCards.BuildSessions(new(sFull, w.IsAllTime ? null : sFull), w, names, LongestName), names);
        Measure("polls", ProjectSYNCS.Commands.ReportCards.BuildPolls(new(pFull, w.IsAllTime ? null : pFull), w, names, LongestName), names);
        Measure("community", ProjectSYNCS.Commands.ReportCards.BuildCommunity(new(cFading, w.IsAllTime ? null : cFull), w, names, LongestName), names);
        Measure("community", ProjectSYNCS.Commands.ReportCards.BuildCommunity(new(cFull, w.IsAllTime ? null : cFull), w, names, LongestName), names);
        Measure("syncs", ProjectSYNCS.Commands.ReportCards.BuildSyncs(new(yFull, w.IsAllTime ? null : yFull), w, (20260801, 84_000), names, LongestName), names);
    }
    var widest = new[]
    {
        ProjectSYNCS.Commands.ReportCards.BuildSessions(new(sFull, sFull), month, true, LongestName).Length,
        ProjectSYNCS.Commands.ReportCards.BuildPolls(new(pFull, pFull), month, true, LongestName).Length,
        ProjectSYNCS.Commands.ReportCards.BuildCommunity(new(cFading, cFull), month, true, LongestName).Length,
        ProjectSYNCS.Commands.ReportCards.BuildSyncs(new(yFull, yFull), month, (20260801, 84_000), true, LongestName).Length,
    };
    Console.WriteLine($"report embeds at worst: {string.Join(" · ", widest)} chars (sessions, polls, community, syncs)");

    // /help lists /report next to /admin, close to the caps: both pages measured.
    var help = asm.GetType("ProjectSYNCS.Commands.HelpModule")!.GetMethod("BuildEmbed", BindingFlags.Public | BindingFlags.Static)!;
    foreach (var page in new[] { 0, 1 })
    {
        var embed = (Discord.Embed)help.Invoke(null, new object[] { page })!;
        if (embed.Length > 6000) Fail($"/help page {page} is {embed.Length} characters");
        if (embed.Fields.FirstOrDefault(f => f.Value.Length > 1024) is { Name: { } field }) Fail($"/help field '{field}' exceeds 1024");
        Console.WriteLine($"/help page {page}: {embed.Length} chars, longest field {embed.Fields.Max(f => f.Value.Length)}");
    }

    // /plynling help is close to the caps too, and the shop card lists every item it sells with its icon.
    foreach (var page in new[] { 0, 1 })
    {
        var embed = ProjectSYNCS.Commands.PlynlingModule.BuildHelpEmbed(page);
        if (embed.Length > 6000) Fail($"/plynling help page {page} is {embed.Length} characters");
        if (embed.Fields.FirstOrDefault(f => f.Value.Length > 1024) is { Name: { } field }) Fail($"/plynling help field '{field}' exceeds 1024");
        Console.WriteLine($"/plynling help page {page}: {embed.Length} chars, longest field {embed.Fields.Max(f => f.Value.Length)}");
    }
    {
        var held = ProjectSYNCS.Helpers.ItemCatalog.Shop.ToDictionary(s => s.Item.Key, _ => 9999);
        var (shop, shopComponents) = ProjectSYNCS.Commands.ShopCards.BuildShop(held, 1_000_000, ProjectSYNCS.Helpers.ItemCatalog.SoapKey,
            new string('x', 200));
        if (shop.Length > 6000) Fail($"/inventory shop is {shop.Length} characters");
        if (shop.Fields.FirstOrDefault(f => f.Value.Length > 1024) is { Name: { } shopField }) Fail($"/inventory shop field '{shopField}' exceeds 1024");
        if (shopComponents.Components.OfType<Discord.ActionRowComponent>().SelectMany(r => r.Components).Count() != 4) Fail("/inventory shop should show its select and three quantity buttons");
        Console.WriteLine($"/inventory shop: {shop.Length} chars, longest field {shop.Fields.Max(f => f.Value.Length)}");
    }

    // Collection tiers: Bronze at 1, Argent 2, Or 3, Platine 5, Diamant 10.
    {
        var expected = new (int Count, ProjectSYNCS.Helpers.CollectionTier Tier)[]
        {
            (0, ProjectSYNCS.Helpers.CollectionTier.None), (1, ProjectSYNCS.Helpers.CollectionTier.Bronze),
            (2, ProjectSYNCS.Helpers.CollectionTier.Silver), (3, ProjectSYNCS.Helpers.CollectionTier.Gold),
            (4, ProjectSYNCS.Helpers.CollectionTier.Gold), (5, ProjectSYNCS.Helpers.CollectionTier.Platinum),
            (9, ProjectSYNCS.Helpers.CollectionTier.Platinum), (10, ProjectSYNCS.Helpers.CollectionTier.Diamond),
            (25, ProjectSYNCS.Helpers.CollectionTier.Diamond),
        };
        foreach (var (count, tier) in expected)
            if (ProjectSYNCS.Helpers.CollectionTiers.TierFor(count) != tier) Fail($"{count} completions should be {tier}, got {ProjectSYNCS.Helpers.CollectionTiers.TierFor(count)}");
        if (ProjectSYNCS.Helpers.CollectionTiers.Next(4) != (ProjectSYNCS.Helpers.CollectionTier.Platinum, 5)) Fail("after 4 completions the next tier is Platine at 5");
        if (ProjectSYNCS.Helpers.CollectionTiers.Next(10) is not null) Fail("Diamant has no next tier");
        if (ProjectSYNCS.Helpers.CollectionTiers.Next(0) != (ProjectSYNCS.Helpers.CollectionTier.Bronze, 1)) Fail("with no completion the next tier is Bronze at 1");
        foreach (var s in ProjectSYNCS.Helpers.ItemCatalog.Sets)
        foreach (var t in Enum.GetValues<ProjectSYNCS.Helpers.CollectionTier>().Skip(1))
        {
            var key = ProjectSYNCS.Helpers.ItemEmojis.BadgeKey(s.Key, t);
            var name = ProjectSYNCS.Helpers.ItemEmojis.EmojiName(key);
            if (!ProjectSYNCS.Helpers.CollectionTiers.IsBadgeKey(key)) Fail($"{key} is not recognised as a badge icon");
            if (name is null || name.Length > 32 || !name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')) Fail($"badge emoji name '{name}' is not a valid Discord emoji name");
        }
        if (ProjectSYNCS.Helpers.CollectionTiers.IsBadgeKey("badge.nope_gold")) Fail("a badge key for an unknown set passed");
    }

    // The collection book at its widest: every icon uploaded (custom markup ~35 chars), every item
    // held, every set at Diamant — the overview and every set page under every filter.
    {
        var fake = "<:a_custom_emoji_name_xx:123456789012345678>";
        foreach (var i in ProjectSYNCS.Helpers.ItemCatalog.All) ProjectSYNCS.Helpers.ItemEmojis.Set(ProjectSYNCS.Helpers.ItemCatalog.PictureKey(i.Key), fake);
        foreach (var s in ProjectSYNCS.Helpers.ItemCatalog.Sets)
        {
            ProjectSYNCS.Helpers.ItemEmojis.Set(ProjectSYNCS.Helpers.ItemEmojis.SetKey(s.Key), fake);
            foreach (var t in Enum.GetValues<ProjectSYNCS.Helpers.CollectionTier>().Skip(1))
                ProjectSYNCS.Helpers.ItemEmojis.Set(ProjectSYNCS.Helpers.ItemEmojis.BadgeKey(s.Key, t), fake);
        }
        var heldAll = ProjectSYNCS.Helpers.ItemCatalog.Collectibles
            .Select(i => new ProjectSYNCS.Models.InventoryItem { Key = i.Key, Quantity = 9999 }).ToList();
        var allDone = ProjectSYNCS.Helpers.ItemCatalog.Sets
            .Select(s => new ProjectSYNCS.Models.CollectionCompletion { SetKey = s.Key, Count = 99 }).ToList();
        var widestBook = 0;
        foreach (var page in ProjectSYNCS.Helpers.ItemCatalog.Sets.Select(s => s.Key).Prepend(ProjectSYNCS.Commands.InventoryModule.OverviewPage))
        foreach (var filter in Enum.GetValues<ProjectSYNCS.Commands.CollectionFilter>())
        foreach (var done in new[] { allDone, new List<ProjectSYNCS.Models.CollectionCompletion>() })
        {
            var (book, bookComponents) = ProjectSYNCS.Commands.InventoryModule.BuildCollectionPage(big, heldAll, done, page, filter);
            Measure($"collection book '{page}'", book, names: true);
            if (book.Description.Length > 4096) Fail($"collection book '{page}' description is {book.Description.Length}");
            widestBook = Math.Max(widestBook, book.Length);
            var ids = bookComponents.Components.OfType<Discord.ActionRowComponent>().SelectMany(r => r.Components)
                .Select(c => c switch { Discord.ButtonComponent b => b.CustomId, Discord.SelectMenuComponent m => m.CustomId, _ => null }).ToList();
            if (ids.Count != ids.Distinct().Count()) Fail($"collection book '{page}' repeats a custom-id");
        }
        var noFacts = new MemberBadgeFacts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, null, new Dictionary<string, StoredBadge>());
        var noStandings = MemberBadges.Evaluate(noFacts, new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero));
        if (ProjectSYNCS.Commands.MemberBadgeCards.CardLine(allDone, noStandings) is not { } badgeLine || badgeLine.Length > 4000) Fail("/level badge line missing or too long");
        if (ProjectSYNCS.Commands.MemberBadgeCards.CardLine(new List<ProjectSYNCS.Models.CollectionCompletion>(), noStandings) is not null) Fail("/level shows a badge line with no completion");
        ProjectSYNCS.Helpers.ItemEmojis.Clear();
        Console.WriteLine($"collection book at worst: {widestBook} chars");
    }

    // Every digest line takes the month and nothing else.
    foreach (var line in (string[])br.GetField("OwnerDigestLines")!.GetValue(null)!)
    {
        try { if (!string.Format(line, "octobre 2026").Contains("octobre 2026")) Fail($"digest line without the month: {line}"); }
        catch (FormatException) { Fail($"digest line breaks string.Format: {line}"); }
    }
}

// 24. Activity keys and command paths (/report activity and commands).
{
    // 23:30 UTC on 19 Oct 2026 (CEST, +2) is 01:30 on the 20th in Paris.
    if (ActivityKeys.DayHour(new DateTimeOffset(2026, 10, 19, 23, 30, 0, TimeSpan.Zero)) != (20261020, 1)) Fail("DayHour misses the Paris day");
    // Clocks go back on 25 Oct 2026 at 01:00 UTC: 01:30 UTC is 02:30 CET.
    if (ActivityKeys.DayHour(new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero)) != (20261025, 2)) Fail("DayHour across the clock change");
    // Winter (CET, +1): 23:30 UTC on 1 Dec is 00:30 on the 2nd.
    if (ActivityKeys.DayHour(new DateTimeOffset(2026, 12, 1, 23, 30, 0, TimeSpan.Zero)) != (20261202, 0)) Fail("DayHour in winter");

    if (CommandPath.Join(new[] { "pl" }, "feed") != "plynling feed") Fail("/pl is not counted as /plynling");
    if (CommandPath.Join(new[] { "config", "excluded-channels" }, "add") != "config excluded-channels add") Fail("nested groups");
    if (CommandPath.Join(Array.Empty<string>(), "help") != "help") Fail("top-level command");
    if (CommandPath.Shortcut(ShortcutKind.Work) != ";work" || CommandPath.Shortcut(ShortcutKind.Visit) != ";pl visit") Fail("shortcut keys");
}

// 25. ActivityCounter: counts, Drain empties, Restore merges a failed flush back.
{
    var t2 = new ActivityCounter();
    var t2At = new DateTimeOffset(2026, 10, 19, 18, 0, 0, TimeSpan.Zero); // 20:00 in Paris
    t2.CountMessage(1, 10, t2At);
    t2.CountMessage(1, 10, t2At);
    t2.CountMessage(1, 11, t2At.AddHours(1));
    t2.CountVoice(1, 20, 3, 1, t2At);
    t2.CountVoice(1, 20, 5, 1, t2At);
    t2.CountVoice(1, 20, 0, 1, t2At);                 // an empty room adds nothing
    t2.CountCommand(1, "report sessions", true, t2At);
    t2.CountCommand(1, "report sessions", false, t2At);
    var t2Batch = t2.Drain();
    if (t2Batch.Hours[new HourKey(1, 20261019, 20)] != new HourTally(2, 8)) Fail("hour tally");
    if (t2Batch.Channels[new ChannelKey(1, 20, 20261019)] != new ChannelTally(0, 8, 5)) Fail("voice minutes or peak");
    if (t2Batch.Commands[new CommandKey(1, 20261019, "report sessions")] != new CommandTally(2, 1)) Fail("command tally");
    if (!t2.Drain().IsEmpty) Fail("Drain left counts behind");
    if (!t2Batch.For(2).IsEmpty || t2Batch.GuildIds.Single() != 1) Fail("batch split by guild");
    t2.CountMessage(1, 10, t2At);
    t2.Restore(t2Batch);
    if (t2.Drain().Channels[new ChannelKey(1, 10, 20261019)] != new ChannelTally(3, 0, 0)) Fail("Restore did not merge");
}

// The widest embed of each batch-2 report, printed once their sections have run.
var batch2Widest = new Dictionary<string, int>();

// 26. Activity and command arithmetic.
{
    ProjectSYNCS.Models.ActivityHourStat T5Hour(int day, int hour, long messages, long voice) =>
        new() { GuildId = 1, Day = day, Hour = hour, Messages = messages, VoiceMinutes = voice };
    ProjectSYNCS.Models.ChannelDailyStat T5Chan(ulong id, long messages, long voice, int peak) =>
        new() { GuildId = 1, ChannelId = id, Day = 20261019, Messages = messages, VoiceMinutes = voice, VoicePeak = peak };
    var t5Guild = new[] { new ChannelInfo(10, false, false), new ChannelInfo(11, false, true), new ChannelInfo(12, false, false),
                          new ChannelInfo(20, true, false), new ChannelInfo(21, true, false) };
    var t5 = ActivityReport.Compute(
        new[] { T5Hour(20261019, 21, 50, 30), T5Hour(20261019, 14, 5, 0), T5Hour(20261018, 21, 10, 0) },  // 19 Oct 2026 is a Monday
        new[] { T5Chan(10, 40, 0, 0), T5Chan(11, 15, 0, 0), T5Chan(20, 0, 30, 3), T5Chan(99, 10, 0, 0) },  // 99: deleted channel
        t5Guild);
    if (t5.Messages != 65 || t5.VoiceMinutes != 30) Fail($"activity totals {t5.Messages}/{t5.VoiceMinutes}");
    if (t5.MessagesByHour.Count != 24 || t5.MessagesByHour[21] != 60 || t5.BusiestHours[0] != 21) Fail("hour profile");
    if (t5.BusiestWeekday != DayOfWeek.Monday) Fail($"busiest weekday {t5.BusiestWeekday}");
    if (t5.TopText[0].ChannelId != 10 || !t5.TopText.Single(l => l.ChannelId == 11).Excluded) Fail("text ranking or excluded mark");
    if (!t5.TopText.Any(l => l.ChannelId == 99)) Fail("a deleted channel with counts must still be listed");
    if (t5.SilentText.Single().ChannelId != 12) Fail("silent channels");
    if (t5.TopVoice.Single().Peak != 3 || t5.EmptyVoice.Single().ChannelId != 21) Fail("voice rooms");
    var t5Empty = ActivityReport.Compute(Array.Empty<ProjectSYNCS.Models.ActivityHourStat>(), Array.Empty<ProjectSYNCS.Models.ChannelDailyStat>(), Array.Empty<ChannelInfo>());
    if (t5Empty.Messages != 0 || t5Empty.BusiestWeekday is not null || t5Empty.SilentText.Count != 0) Fail("empty activity (guild unavailable)");

    ProjectSYNCS.Models.CommandDailyStat T5Cmd(string command, long uses, long failures) =>
        new() { GuildId = 1, Day = 20261019, Command = command, Uses = uses, Failures = failures };
    var t5c = CommandReport.Compute(
        new[] { T5Cmd("report sessions", 3, 1), T5Cmd("report sessions", 2, 0), T5Cmd("help", 9, 0), T5Cmd(";work", 4, 0), T5Cmd("old name", 1, 0) },
        new[] { "help", "report sessions", "report polls", "plynling feed" });
    if (t5c.Uses != 19 || t5c.Failures != 1) Fail("command totals");
    if (t5c.Top[0].Command != "help" || t5c.Top.Single(l => l.Command == "report sessions").Uses != 5) Fail("command ranking");
    if (t5c.Shortcuts.Single().Command != ";work" || t5c.Top.Any(l => l.Command.StartsWith(';'))) Fail("shortcuts kept apart");
    if (!t5c.NeverUsed.SequenceEqual(new[] { "plynling feed", "report polls" })) Fail($"never used: {string.Join(",", t5c.NeverUsed)}");
    if (!t5c.Top.Any(l => l.Command == "old name")) Fail("a renamed command keeps its old rows");
}

// 27. /report activity and commands: every card measured at its worst.
{
    void T6Fits(string what, Discord.Embed e, bool names)
    {
        batch2Widest[what] = Math.Max(batch2Widest.GetValueOrDefault(what), e.Length);
        if (e.Length > 6000) Fail($"{what} embed is {e.Length} characters");
        if (e.Fields.Length > 25 || e.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"{what} has an empty, oversized or 26th field");
        if (!names && (e.Description + string.Concat(e.Fields.Select(f => f.Value))).Contains("<@")) Fail($"{what} names someone");
    }
    const ulong t6Big = 1_234_567_890_123_456_789;
    var t6Lines = Enumerable.Range(0, ActivityReport.TopChannels).Select(i => new ChannelLine(t6Big + (ulong)i, 99_999_999, 99, i % 2 == 0)).ToList();
    var t6Many = Enumerable.Range(0, 60).Select(i => new ChannelLine(t6Big + 100 + (ulong)i, 0, 0, true)).ToList();
    var t6Act = new ActivityReportData(99_999_999, 99_999_999, Enumerable.Repeat(9_999L, 24).ToList(), Enumerable.Repeat(9_999L, 24).ToList(),
        new[] { 21, 22, 23 }, DayOfWeek.Wednesday, t6Lines, t6Many, t6Lines, t6Many, t6Lines.ToDictionary(l => l.ChannelId, l => 5L));
    var t6Cmd = new CommandReportData(9_999_999, 99_999,
        Enumerable.Range(0, CommandReport.TopCommands).Select(i => new CommandLine($"plynling some-long-subcommand-{i}", 999_999, 999)).ToList(),
        new[] { new CommandLine(";pl", 9999, 0), new CommandLine(";work", 9999, 0), new CommandLine(";pl visit", 9999, 0) },
        Enumerable.Range(0, 150).Select(i => $"config some-group-name sub-{i}").ToList(),
        new Dictionary<string, long>(), Array.Empty<CommandLine>());
    var t6Month = ReportWindow.From(StatsPeriod.Month, DateTimeOffset.UtcNow);
    foreach (var t6Names in new[] { true, false })
    {
        T6Fits("activity", ProjectSYNCS.Commands.ReportCards.BuildActivity(new(t6Act, t6Act), t6Month, t6Names), t6Names);
        T6Fits("commands", ProjectSYNCS.Commands.ReportCards.BuildCommands(new(t6Cmd, t6Cmd), t6Month, t6Names), t6Names);
    }
    var t6Quiet = ActivityReport.Compute(Array.Empty<ProjectSYNCS.Models.ActivityHourStat>(), Array.Empty<ProjectSYNCS.Models.ChannelDailyStat>(), Array.Empty<ChannelInfo>());
    T6Fits("empty activity", ProjectSYNCS.Commands.ReportCards.BuildActivity(new(t6Quiet, null), ReportWindow.From(StatsPeriod.AllTime, DateTimeOffset.UtcNow), true), true);
    if (!ReportKinds.Digest(DigestRecipient.Owner).SequenceEqual(new[] { ReportKind.Syncs, ReportKind.Commands, ReportKind.Quiz })
        || !ReportKinds.Digest(DigestRecipient.Tata).SequenceEqual(new[] { ReportKind.Community, ReportKind.Sessions, ReportKind.Activity, ReportKind.Sleeping }))
        Fail("digest lists");
}

// 28. /report quiz: posted, found, unanswered; a round whose send failed is not a question.
{
    var t7At = new DateTimeOffset(2026, 10, 19, 18, 0, 0, TimeSpan.Zero);
    var t7Q0 = QuizBank.All[0];
    var t7Q1 = QuizBank.All[1];
    var t7Rounds = new List<ProjectSYNCS.Models.QuizRound>
    {
        new() { GuildId = 1, MessageId = 5, QuestionKey = t7Q0.Key, PostedAt = t7At, Closed = true, WinnerId = 7, WonAt = t7At.AddSeconds(42), Day = 20261019 },
        new() { GuildId = 1, MessageId = 6, QuestionKey = t7Q1.Key, PostedAt = t7At, Closed = true, Day = 20261019 },
        new() { GuildId = 1, MessageId = 0, QuestionKey = t7Q1.Key, PostedAt = t7At, Closed = true, Day = 20261019 },   // send failed
    };
    var t7 = QuizReport.Compute(t7Rounds);
    if (t7.Posted != 2 || t7.Found != 1 || t7.Unanswered != 1) Fail($"quiz {t7.Posted}/{t7.Found}/{t7.Unanswered}");
    if (t7.MedianSeconds != 42 || t7.Fastest?.UserId != 7) Fail("quiz timing");
    if (!t7.UnfoundPrompts.SequenceEqual(new[] { t7Q1.Prompt })) Fail("unfound prompts");
    if (t7.ByCategory.Sum(l => l.Posted) != 2) Fail("quiz by category");
    var t7Worst = new QuizReportData(999, 999, 999,
        Enum.GetValues<QuizCategory>().Select(x => new QuizGroupLine(QuizBank.Label(x), 999, 999)).ToList(),
        Enum.GetValues<QuizDifficulty>().Select(x => new QuizGroupLine(QuizBank.Label(x), 999, 999)).ToList(),
        999, new PersonTally(1_234_567_890_123_456_789, 999_999, 0),
        Enumerable.Repeat(new string('Q', 400), QuizReport.ListedPrompts).ToList(),
        Enumerable.Range(0, 5).Select(i => new PersonTally(1_234_567_890_123_456_789 + (ulong)i, 999, 999)).ToList());
    var t7Embed = ProjectSYNCS.Commands.ReportCards.BuildQuiz(new(t7Worst, t7Worst), ReportWindow.From(StatsPeriod.Month, t7At), true, LongestName);
    batch2Widest["quiz"] = t7Embed.Length;
    if (t7Embed.Length > 6000 || t7Embed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"quiz embed {t7Embed.Length}");
    // The community report counts the same posted rounds.
    var t7Community = CommunityReport.Compute(Array.Empty<ProjectSYNCS.Models.MemberDailyStat>(), Array.Empty<ProjectSYNCS.Models.MemberFlowDailyStat>(),
        Array.Empty<ProjectSYNCS.Models.EmoteDailyStat>(), new Dictionary<(ulong, string), string>(), t7Rounds, ReportWindow.From(StatsPeriod.Month, t7At));
    if (t7Community.QuizRounds != 2) Fail($"community counts {t7Community.QuizRounds} quiz rounds, a failed send included");
}

// 29. /report plynlings: births, deaths, the living's state; the mascot never counts.
{
    var t8Now = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
    var t8Window = ReportWindow.From(StatsPeriod.Week, t8Now);
    ProjectSYNCS.Models.Plynling T8Pet(int id, ulong owner, int adoptedDaysAgo)
    {
        var p = PlynlingLife.Create(1, owner, $"Pet{id}", ProjectSYNCS.Models.PlynlingSpecies.Amanite, ProjectSYNCS.Models.PlynlingGender.Male, t8Now.AddDays(-adoptedDaysAgo));
        p.Id = id;
        p.Hunger = 0.9;
        p.NeedsAsOf = t8Now;
        return p;
    }
    var t8Healthy = T8Pet(1, 101, 30);
    var t8Starving = T8Pet(2, 102, 2);
    t8Starving.Hunger = 0.1;
    var t8Sick = T8Pet(3, 103, 40);
    t8Sick.SickSince = t8Now.AddHours(-3);
    var t8Dead = T8Pet(4, 104, 20);
    t8Dead.DiedAt = t8Now.AddDays(-1);
    t8Dead.DeathCause = ProjectSYNCS.Models.DeathCause.Illness;
    var t8Mascot = T8Pet(5, 999, 1);
    PlynlingMascot.Bind(999);
    var t8 = PlynlingReport.Compute(new[] { t8Healthy, t8Starving, t8Sick, t8Dead, t8Mascot },
        new[] { new ProjectSYNCS.Models.PlynlingEventInstance { PlynlingId = 1, EventKey = "nope", ResolvedAt = t8Now.AddDays(-2), DecidedAlone = true, ChallengeSucceeded = true } },
        Array.Empty<ProjectSYNCS.Models.PlynlingRelation>(), Array.Empty<ProjectSYNCS.Models.PlynlingBadge>(), Array.Empty<ProjectSYNCS.Models.PlynlingTrait>(),
        t8Window, t8Now);
    PlynlingMascot.Bind(0);
    if (t8.Living != 3) Fail($"living {t8.Living} (the mascot must not count)");
    if (t8.Born != 1) Fail($"born {t8.Born}");
    if (t8.Deaths.Single() != (ProjectSYNCS.Models.DeathCause.Illness, 1)) Fail("deaths by cause");
    if (t8.Starving != 1 || t8.Sick != 1) Fail($"starving {t8.Starving}, sick {t8.Sick}");
    if (t8.Watch.Count != 2 || !t8.Watch[0].Sick) Fail("watch list (sick first)");
    if (t8.EventsResolved != 1 || t8.DecidedAlone != 1 || t8.ChallengesWon != 1 || t8.TopEvents.Single().Title != "nope") Fail("events (an unknown key shows its key)");

    var t8Worst = t8 with
    {
        TopEvents = Enumerable.Range(0, PlynlingReport.TopEventCount).Select(i => (new string('E', 80), 999)).ToList(),
        Watch = Enumerable.Range(0, PlynlingReport.WatchCount).Select(i => new PlynlingWatch(1_234_567_890_123_456_789 + (ulong)i, "**@everyone** " + new string('N', 30), true, true)).ToList(),
    };
    var t8Embed = ProjectSYNCS.Commands.ReportCards.BuildPlynlings(new(t8Worst, t8Worst), t8Window, true, LongestName);
    batch2Widest["plynlings"] = t8Embed.Length;
    if (t8Embed.Length > 6000 || t8Embed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"plynlings embed {t8Embed.Length}");
    if (t8Embed.Fields.Any(f => f.Value.Contains("**@everyone**"))) Fail("a Plynling name was not sanitized");
}

// 30. /report emotes: own, foreign and Unicode; never-used server emotes; movers.
{
    ProjectSYNCS.Models.EmoteDailyStat T9Row(ulong id, string unicode, long written, long reacted) =>
        new() { GuildId = 1, EmoteId = id, Unicode = unicode, Day = 20261019, WrittenCount = written, ReactedCount = reacted };
    var t9Server = new[] { new ServerEmote(1, "<:one:1>"), new ServerEmote(2, "<:two:2>"), new ServerEmote(3, "<:three:3>") };
    var t9Markup = new Dictionary<(ulong, string), string> { [(50, "")] = "<:elsewhere:50>" };
    var t9 = EmoteReport.Compute(new[] { T9Row(1, "", 5, 5), T9Row(2, "", 1, 0), T9Row(50, "", 4, 0), T9Row(77, "", 1, 0), T9Row(0, "😂", 9, 0) }, t9Server, t9Markup);
    if (t9.Own != 11 || t9.Foreign != 5 || t9.Unicode != 9) Fail($"emote split {t9.Own}/{t9.Foreign}/{t9.Unicode}");
    if (t9.TopOwn[0].Markup != "<:one:1>" || !t9.UnusedOwn.SequenceEqual(new[] { "<:three:3>" })) Fail("own ranking or never used");
    if (!t9.TopForeign.Any(l => l.Markup.Contains("77"))) Fail("a foreign emote without a stat row must still show");
    var t9Before = EmoteReport.Compute(new[] { T9Row(2, "", 8, 0) }, t9Server, t9Markup);
    if (EmoteReport.Movers(t9, t9Before, rising: true)[0].Markup != "<:one:1>" || EmoteReport.Movers(t9, t9Before, rising: false)[0].Markup != "<:two:2>") Fail("movers");
    var t9NoGuild = EmoteReport.Compute(new[] { T9Row(1, "", 5, 5) }, Array.Empty<ServerEmote>(), t9Markup);
    if (t9NoGuild.Own != 0 || t9NoGuild.UnusedOwn.Count != 0) Fail("guild unavailable: no emote counts as the server's");

    var t9ManyServer = Enumerable.Range(0, 80).Select(i => new ServerEmote(1000 + (ulong)i, $"<a:a_long_emote_name_{i}:{1_234_567_890_123_456_789 + (ulong)i}>")).ToList();
    var t9Worst = EmoteReport.Compute(t9ManyServer.Take(40).Select(e => T9Row(e.Id, "", 999_999, 999_999)).ToList(), t9ManyServer, t9Markup);
    var t9Embed = ProjectSYNCS.Commands.ReportCards.BuildEmotes(new(t9Worst, t9Before), ReportWindow.From(StatsPeriod.Month, DateTimeOffset.UtcNow), true);
    batch2Widest["emotes"] = t9Embed.Length;
    if (t9Embed.Length > 6000 || t9Embed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"emotes embed {t9Embed.Length}");
    // Every emote that opens must close: a cut one leaves a « <:name » with no full « <:name:id> ».
    if (t9Embed.Fields.Any(f => System.Text.RegularExpressions.Regex.Matches(f.Value, "<a?:").Count
                                != System.Text.RegularExpressions.Regex.Matches(f.Value, @"<a?:\w+:\d+>").Count)) Fail("an emote was cut in half");
}

// 31. /report sleeping: who has been silent N days; newcomers and bots never listed.
{
    var t10Now = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
    var t10Members = new[]
    {
        new MemberSnapshot(1, t10Now.AddDays(-200), false),   // active 5 days ago
        new MemberSnapshot(2, t10Now.AddDays(-200), false),   // last seen 40 days ago
        new MemberSnapshot(3, t10Now.AddDays(-200), false),   // never active
        new MemberSnapshot(4, t10Now.AddDays(-10), false),    // joined recently: never listed
        new MemberSnapshot(5, null, false),                    // unknown join date: an old member
        new MemberSnapshot(6, t10Now.AddDays(-200), true),    // a bot
    };
    var t10Last = new Dictionary<ulong, int> { [1] = AppTime.DayKey(t10Now.AddDays(-5)), [2] = AppTime.DayKey(t10Now.AddDays(-40)) };
    var t10 = SleepingReport.Compute(t10Members, t10Last, 30, t10Now);
    if (t10.Members != 5) Fail($"members {t10.Members} (bots excluded)");
    if (!t10.Sleepers.Select(s => s.UserId).SequenceEqual(new ulong[] { 3, 5, 2 })) Fail($"sleepers {string.Join(",", t10.Sleepers.Select(s => s.UserId))} (never-active first)");
    if (SleepingReport.Compute(t10Members, t10Last, 60, t10Now).Sleepers.Any(s => s.UserId == 2)) Fail("40 days silent is not asleep at 60");
    if (SleepingReport.Compute(Array.Empty<MemberSnapshot>(), t10Last, 30, t10Now).Sleepers.Count != 0) Fail("guild unavailable");

    var t10Many = Enumerable.Range(0, 300).Select(i => new Sleeper(1_234_567_890_123_456_789 + (ulong)i, i % 2 == 0 ? null : 20260901)).ToList();
    var t10Embed = ProjectSYNCS.Commands.ReportCards.BuildSleeping(new SleepingReportData(90, 400, t10Many), LongestName);
    batch2Widest["sleeping"] = t10Embed.Length;
    if (t10Embed.Length > 6000 || t10Embed.Fields.Length > 2 || t10Embed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"sleeping embed {t10Embed.Length}, {t10Embed.Fields.Length} fields");
    if (!t10Embed.Footer!.Value.Text.Contains("autres")) Fail("the sleepers that don't fit must be counted");
}

// 32. /report's line on top of the card: whose pool, untagged, and never past the caps.
{
    var t11Tata = (ulong)Field("TataId");
    if (!ReferenceEquals(ProjectSYNCS.Commands.ReportCards.DutyPool(AvailabilityService.OwnerId), Field("OwnerReportDutyLines"))
        || !ReferenceEquals(ProjectSYNCS.Commands.ReportCards.DutyPool(t11Tata), Field("TataReportLines"))
        || !ReferenceEquals(ProjectSYNCS.Commands.ReportCards.DutyPool(42), Field("StaffReportLines")))
        Fail("report line: wrong pool for Papa, Tata or other staff");
    var t11Card = new Discord.EmbedBuilder().WithTitle("t").WithDescription("figures").Build();
    var t11Full = new Discord.EmbedBuilder().WithTitle("t").WithDescription(new string('x', 4090)).Build();
    foreach (var t11Pool in new[] { "OwnerReportDutyLines", "TataReportLines", "StaffReportLines" })
        foreach (var l in (string[])Field(t11Pool))
        {
            if (Sticker.Strip(l) != l || l.Contains('{') || l.Contains('}')) Fail($"{t11Pool}: tagged or braced: {l}");
            if (l.Length > 200) Fail($"{t11Pool}: {l.Length} characters: {l}");
            if (!ProjectSYNCS.Commands.ReportCards.WithHeadline(t11Card, l).Description.StartsWith(l + "\n\nfigures")) Fail($"{t11Pool}: not on top: {l}");
            if (ProjectSYNCS.Commands.ReportCards.WithHeadline(t11Full, l).Description.Length > 4096) Fail("report line pushed a description past 4096");
        }
}

// 33. The monthly digest's « Le mois en bref »: what each version flags, and both cards at their worst.
{
    var t12Month = ReportWindow.CalendarMonth(2026, 11);
    ProjectSYNCS.Helpers.SyncsReportData T12Syncs(int days, int? max, long answers, long unrecognised, params (ProjectSYNCS.Models.UptimeEventKind, int)[] outages) =>
        new(days, 50_000, max, max is null ? null : 20261110, 0, new List<long>(), outages.ToList(), TimeSpan.Zero, 0.99,
            answers, unrecognised, 0, 0, 0, Array.Empty<PersonTally>());
    CommandReportData T12Cmd(params CommandLine[] failing) => new(failing.Sum(c => c.Uses), failing.Sum(c => c.Failures),
        failing, Array.Empty<CommandLine>(), new[] { "never" }, new Dictionary<string, long>(), failing);
    QuizReportData T12Quiz(params QuizGroupLine[] byDifficulty) => new(byDifficulty.Sum(l => l.Posted), byDifficulty.Sum(l => l.Found), 0,
        Array.Empty<QuizGroupLine>(), byDifficulty, null, null, Array.Empty<string>(), Array.Empty<PersonTally>());

    // Papa: a failing command (3/20) but not a rare one (1/1) nor a robust one (2/100); a power cut;
    // a heat record only against a measured month; unanswered rising 10 points; « difficile » too easy.
    var t12Alerts = DigestBrief.Owner(
        new(T12Cmd(new CommandLine("report sessions", 20, 3), new CommandLine("help", 1, 1), new CommandLine("plynling feed", 100, 2)), null),
        new(T12Syncs(30, 70_000, 200, 60, (ProjectSYNCS.Models.UptimeEventKind.PowerCut, 1)), T12Syncs(30, 60_000, 200, 40)),
        (20261110, 70_000),
        new(T12Quiz(new QuizGroupLine(QuizBank.Label(QuizDifficulty.Hard), 10, 9), new QuizGroupLine(QuizBank.Label(QuizDifficulty.Easy), 10, 8)), null),
        t12Month);
    var t12Kinds = t12Alerts.Select(a => a.Kind).ToList();
    if (!t12Kinds.SequenceEqual(new[] { DigestAlertKind.FailingCommand, DigestAlertKind.PowerCuts, DigestAlertKind.HeatRecord,
            DigestAlertKind.UnansweredRising, DigestAlertKind.QuizTooEasy }))
        Fail($"owner alerts: {string.Join(", ", t12Kinds)}");
    if (t12Alerts[0].Subject != "report sessions") Fail("the failing command is not the one at 3/20");
    // The first month of a counter: no comparison, so no record and no « rising ».
    var t12First = DigestBrief.Owner(new(T12Cmd(), null), new(T12Syncs(12, 70_000, 200, 90), null), (20261110, 70_000),
        new(T12Quiz(), null), t12Month);
    if (t12First.Count != 0) Fail($"first month raised {string.Join(", ", t12First.Select(a => a.Kind))}");

    // Tata: active members down 30 %, more leaves than joins, 3 sleepers of 20.
    CommunityReportData T12Community(int active, int joins, int leaves) => new(active, new List<long>(), 0, 0, 0, joins, leaves,
        Array.Empty<(string, long)>(), 0, 0, 0, new Dictionary<ulong, long> { [1] = 10, [2] = 30, [3] = 20, [4] = 5 });
    var t12Sleeping = new SleepingReportData(30, 20, Enumerable.Range(0, 3).Select(i => new Sleeper((ulong)i, null)).ToList());
    var t12Tata = DigestBrief.Tata(new(T12Community(7, 1, 2), T12Community(10, 0, 0)), t12Sleeping);
    if (!t12Tata.Select(a => a.Kind).SequenceEqual(new[] { DigestAlertKind.ActiveDrop, DigestAlertKind.MoreLeaves, DigestAlertKind.ManySleepers }))
        Fail($"tata alerts: {string.Join(", ", t12Tata.Select(a => a.Kind))}");
    if (DigestBrief.Tata(new(T12Community(7, 2, 1), T12Community(8, 0, 0)), t12Sleeping with { Sleepers = new[] { new Sleeper(1, null) } }).Count != 0)
        Fail("a calm month for Tata raised something");

    var t12Slots = new[] { new SlotTally(1, 3, 10, 6), new SlotTally(5, 2, 10, 9), new SlotTally(6, 1, 5, 5) };
    var t12Sessions = new SessionReportData(6, 0, Array.Empty<(ProjectSYNCS.Models.SessionCategory, int)>(), 0, 0, null, 6, 25, 20, 5, 0, null,
        t12Slots, Array.Empty<SlotTally>(), Array.Empty<PersonTally>(), Array.Empty<PersonTally>(), Array.Empty<PersonTally>(), Array.Empty<PersonTally>());
    if (DigestBrief.BestSessionDay(t12Sessions)?.Slot != 5) Fail("best session day (a single session doesn't count)");

    // Both cards at their worst: every alert at once, the longest command keys.
    var t12Many = Enumerable.Range(0, DigestBrief.FailingListed).Select(i => new DigestAlert(DigestAlertKind.FailingCommand, 99_999, 999_999, $"config some-long-group sub-command-{i}"))
        .Concat(Enum.GetValues<DigestAlertKind>().Select(k => new DigestAlert(k, 99_999, 999_999, "x"))).ToList();
    var t12Owner = ProjectSYNCS.Commands.ReportCards.BuildOwnerBrief(
        new(T12Cmd(new CommandLine("help", 999_999, 99_999)), T12Cmd()), new(T12Syncs(30, 81_234, 9_999_999, 999_999, (ProjectSYNCS.Models.UptimeEventKind.Crash, 99)), T12Syncs(30, 1, 1, 1)),
        new(T12Quiz(Enum.GetValues<QuizDifficulty>().Select(d => new QuizGroupLine(QuizBank.Label(d), 999, 999)).ToArray()), null),
        t12Many, t12Month, "calme");
    var t12Activity = ActivityReport.Compute(Array.Empty<ProjectSYNCS.Models.ActivityHourStat>(), Array.Empty<ProjectSYNCS.Models.ChannelDailyStat>(),
        Enumerable.Range(0, 60).Select(i => new ChannelInfo((ulong)i, false, false)).ToList());
    var t12TataCard = ProjectSYNCS.Commands.ReportCards.BuildTataBrief(new(T12Community(999, 999, 999), null), new(t12Sessions, null),
        t12Activity, t12Sleeping, 99, t12Many, t12Month, "calme", LongestName);
    foreach (var (t12What, t12Embed) in new[] { ("owner brief", t12Owner), ("tata brief", t12TataCard) })
    {
        if (t12Embed.Length > 6000 || t12Embed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"{t12What}: {t12Embed.Length} characters");
        batch2Widest[t12What] = t12Embed.Length;
    }
    var t12Top = string.Join(" · ", new ulong[] { 2, 3, 1 }.Select(i => ProjectSYNCS.Commands.ReportCards.Who(i, LongestName)));
    if (!t12TataCard.Fields.Any(f => f.Value.Contains(t12Top))) Fail("tata brief: the most active, by XP");

    // A person in a report: the name written out (a mention alone shows a raw id in a DM), sanitized,
    // with the mention kept; the mention alone when no name is known.
    if (ProjectSYNCS.Commands.ReportCards.Who(7, _ => "Bob") != "**Bob** (<@7>)") Fail("report name: name then mention");
    if (ProjectSYNCS.Commands.ReportCards.Who(7, _ => "**@everyone**").Contains("**@everyone**")) Fail("report name: not sanitized");
    if (ProjectSYNCS.Commands.ReportCards.Who(7, _ => null) != "<@7>" || ProjectSYNCS.Commands.ReportCards.Who(7, null) != "<@7>")
        Fail("report name: no name known must keep the mention alone");
    if (!ProjectSYNCS.Commands.ReportCards.BuildSleeping(new SleepingReportData(30, 20, new[] { new Sleeper(7, null) }), _ => "Bob")
            .Fields.Any(f => f.Value.Contains("**Bob** (<@7>)")))
        Fail("sleeping report: the sleeper's name is not written out");
    if (t12Owner.Fields.Last().Value.Contains("calme") || !ProjectSYNCS.Commands.ReportCards.BuildOwnerBrief(new(T12Cmd(), null), new(T12Syncs(0, null, 0, 0), null),
            new(T12Quiz(), null), Array.Empty<DigestAlert>(), t12Month, "calme").Fields.Last().Value.Equals("calme"))
        Fail("« À regarder » shows the alerts, or her quiet line when there are none");
}

Console.WriteLine($"batch 2 report embeds at worst: {string.Join(" · ", batch2Widest.Select(x => $"{x.Key} {x.Value}"))} chars");

// 13. Minecraft: when she joins and leaves (McPresence), how she reads deaths (McDeath), and her
// in-game pools (plain text under Minecraft's 256-character chat cap).
{
    var mcT0 = new DateTimeOffset(2026, 10, 19, 20, 0, 0, TimeSpan.Zero);
    string[] Names(int n, params string[] extra) => Enumerable.Range(1, n).Select(i => $"p{i}").Concat(extra).ToArray();
    void Presence(string what, McPresenceVerdict got, bool inGame, bool fewRunning)
    {
        if (got.InGame != inGame || (got.FewSince is not null) != fewRunning)
            Fail($"presence: {what}: got InGame={got.InGame}, FewSince={got.FewSince}");
    }

    Presence("joins at 4", McPresence.Decide(true, Names(4), 4, "Papa", false, null, mcT0), true, false);
    Presence("no join at 3", McPresence.Decide(true, Names(3), 3, "Papa", false, null, mcT0), false, false);
    Presence("joins for the owner alone", McPresence.Decide(true, Names(0, "papa"), 1, "Papa", false, null, mcT0), true, false);
    Presence("no owner configured", McPresence.Decide(true, Names(1), 1, "", false, null, mcT0), false, false);
    Presence("count beyond the sample", McPresence.Decide(true, Names(2), 5, "Papa", false, null, mcT0), true, false);
    Presence("stays at 3", McPresence.Decide(true, Names(3), 3, "Papa", true, null, mcT0), true, false);
    Presence("≤2 starts the clock", McPresence.Decide(true, Names(2), 2, "Papa", true, null, mcT0), true, true);
    Presence("≤2 at 3 min stays", McPresence.Decide(true, Names(2), 2, "Papa", true, mcT0, mcT0 + McPresence.Linger), true, true);
    Presence("≤2 past 3 min leaves", McPresence.Decide(true, Names(1), 1, "Papa", true, mcT0, mcT0 + McPresence.Linger + TimeSpan.FromSeconds(1)), false, true);
    Presence("owner on: ≤2 doesn't count", McPresence.Decide(true, Names(1, "Papa"), 2, "Papa", true, mcT0, mcT0 + TimeSpan.FromHours(1)), true, false);
    Presence("back to 3 resets the clock", McPresence.Decide(true, Names(3), 3, "Papa", true, mcT0, mcT0 + TimeSpan.FromMinutes(2)), true, false);
    Presence("server down", McPresence.Decide(false, Names(8, "Papa"), 9, "Papa", true, null, mcT0), false, false);

    var mcPlayers = new[] { "Alice", "Bob" };
    McArg McP(string n) => new(n, null);
    void Death(string key, McArg[] args, McDeathKind? kind)
    {
        var got = McDeath.Classify(key, args, mcPlayers, "SYNCS")?.Kind;
        if (got != kind) Fail($"death {key}: got {got?.ToString() ?? "null"}, expected {kind?.ToString() ?? "null"}");
    }
    Death("death.attack.explosion.player", new[] { McP("Alice"), new McArg("Creeper", "entity.minecraft.creeper") }, McDeathKind.Creeper);
    Death("death.fell.accident.generic", new[] { McP("Alice") }, McDeathKind.Fall);
    Death("death.attack.fall", new[] { McP("Alice") }, McDeathKind.Fall);
    Death("death.attack.lava", new[] { McP("Bob") }, McDeathKind.Fire);
    Death("death.attack.inFire.player", new[] { McP("Bob"), new McArg("Zombie", "entity.minecraft.zombie") }, McDeathKind.Fire);
    Death("death.attack.drown", new[] { McP("Bob") }, McDeathKind.Drown);
    Death("death.attack.mob", new[] { McP("Bob"), new McArg("Zombie", "entity.minecraft.zombie") }, McDeathKind.Mob);
    Death("death.attack.arrow", new[] { McP("Bob"), new McArg("Skeleton", "entity.minecraft.skeleton") }, McDeathKind.Mob);
    Death("death.attack.player", new[] { McP("Bob"), McP("Alice") }, McDeathKind.Player);
    Death("death.attack.arrow.item", new[] { McP("Bob"), McP("Alice"), new McArg("Bow", null) }, McDeathKind.Player);
    Death("death.attack.outOfWorld", new[] { McP("Alice") }, McDeathKind.Other);
    Death("death.attack.mob", new[] { McP("SYNCS"), new McArg("Zombie", "entity.minecraft.zombie") }, McDeathKind.Self);
    Death("death.attack.mob", new[] { McP("Rex"), new McArg("Zombie", "entity.minecraft.zombie") }, null); // a named pet
    Death("chat.type.advancement.task", new[] { McP("Alice"), McP("Stone Age") }, null);
    if (McDeath.AdvancementEarner("chat.type.advancement.task", new[] { McP("Alice"), McP("Stone Age") }, mcPlayers) != "Alice")
        Fail("advancement earner");
    if (McDeath.AdvancementEarner("chat.type.advancement.goal", new[] { McP("Rex"), McP("x") }, mcPlayers) is not null)
        Fail("advancement earner: not a player");
    Covers("McDeathLines", typeof(McDeathKind));
    Covers("McResourceNames", typeof(McResource));
    Covers("McCropNames", typeof(McCrop));

    // In-game orders (McCommand): her name is needed, a refusal is not an order, staying wins.
    var mcNames = new[] { "SYNCS" }.Concat(McCommand.HerNames).ToArray();
    void Order(string text, McOrder expected)
    {
        var got = McCommand.Read(text, mcNames);
        if (got != expected) Fail($"order « {text} »: got {got}, expected {expected}");
    }
    Order("syncs viens", McOrder.Come);
    Order("SYNCS, viens ici !", McOrder.Come);
    Order("viens syncs", McOrder.Come);
    Order("syncs tu viens ?", McOrder.Come);
    Order("Syncs rejoins-moi", McOrder.Come);
    Order("syncs par ici", McOrder.Come);
    Order("syncs suis-moi", McOrder.Follow);
    Order("Suis moi SYNCS", McOrder.Follow);
    Order("syncs follow me", McOrder.Follow);
    Order("syncs accompagne-moi", McOrder.Follow);
    Order("syncs reste là", McOrder.Stay);
    Order("syncs reste ici", McOrder.Stay);
    Order("syncs stop", McOrder.Stay);
    Order("syncs arrête-toi", McOrder.Stay);
    Order("syncs bouge pas", McOrder.Stay);
    Order("syncs attends", McOrder.Stay);
    Order("syncs viens pas", McOrder.None);
    Order("syncs ne me suis pas", McOrder.None);
    Order("viens voir", McOrder.None);
    Order("suis moi", McOrder.None);
    Order("je suis là syncs", McOrder.Unknown);
    Order("syncs", McOrder.Unknown);
    Order("syncs ?", McOrder.Unknown);
    Order("hé syncs", McOrder.Unknown);
    // A compliment or a thank-you is not a call: « gg syncs » once got « oui ? ».
    Order("gg syncs", McOrder.Praise);
    Order("bravo syncs", McOrder.Praise);
    Order("syncs bien joué !", McOrder.Praise);
    Order("t'es la meilleure syncs", McOrder.Praise);
    Order("syncs t'es géniale", McOrder.Praise);
    Order("syncs c'est génial", McOrder.Unknown);
    Order("merci syncs", McOrder.Thanks);
    Order("Merci beaucoup Syncs !", McOrder.Thanks);
    Order("thx syncs", McOrder.Thanks);
    Order("gg syncs viens", McOrder.Come);
    Order("merci syncs bonne nuit", McOrder.Bye);
    Order("j'ai dit merci à syncs pour le bois tout à l'heure", McOrder.None);
    Order("je crois que syncs est un peu nulle franchement", McOrder.None);
    Order("syncs est trop forte en vrai de vrai de vrai", McOrder.None);
    Order("suisse", McOrder.None);
    Order("syncs dodo", McOrder.Sleep);
    Order("syncs va dormir", McOrder.Sleep);
    Order("syncs protège-moi", McOrder.Guard);
    Order("Syncs défends-moi !", McOrder.Guard);
    Order("syncs ramène-toi", McOrder.Come);
    Order("syncs va chercher", McOrder.Unknown);
    Order("syncs va chercher plus de bois", McOrder.Collect); // « plus de » is more, not a refusal

    // Variants of the orders she knows: familiar French, game-chat spellings, a little English.
    foreach (var (text, expected) in new (string, McOrder)[]
    {
        ("syncs vien", McOrder.Come), ("syncs vi1 stp", McOrder.Come), ("syncs ramène ta fraise", McOrder.Come),
        ("syncs rapplique", McOrder.Come), ("syncs radine-toi", McOrder.Come), ("syncs viens me voir", McOrder.Come),
        ("syncs rapproche-toi", McOrder.Come), ("syncs au pied", McOrder.Come), ("syncs come here", McOrder.Come),
        ("c'est pas grave, syncs viens", McOrder.Come), ("sync viens", McOrder.Come),
        ("syncs viens avec moi", McOrder.Follow), ("syncs reste avec moi", McOrder.Follow),
        ("syncs colle-moi", McOrder.Follow), ("syncs suis-moi stp", McOrder.Follow), ("syncs follow me", McOrder.Follow),
        ("syncs fige-toi", McOrder.Stay), ("syncs annule", McOrder.Stay), ("syncs laisse tomber", McOrder.Stay),
        ("syncs ça suffit", McOrder.Stay), ("syncs arrête de me suivre", McOrder.Stay), ("syncs sur place", McOrder.Stay),
        ("syncs bouge pu", McOrder.Stay), ("syncs wait", McOrder.Stay),
        ("syncs au dodo", McOrder.Sleep), ("syncs va au lit", McOrder.Sleep), ("syncs pionce", McOrder.Sleep),
        ("syncs au secours", McOrder.Guard), ("syncs à l'aide !", McOrder.Guard), ("syncs couvre-moi", McOrder.Guard),
        ("syncs tue les monstres", McOrder.Guard), ("syncs escorte-moi", McOrder.Guard), ("syncs help me", McOrder.Guard),
        ("syncs fais du bois", McOrder.Collect), ("syncs j'ai besoin de fer", McOrder.Collect),
        ("syncs il me faut 20 pierres", McOrder.Collect), ("syncs ramasse du sable", McOrder.Collect),
        ("syncs abats 5 arbres", McOrder.Collect), ("syncs trouve-moi des diams", McOrder.Collect),
        ("syncs file-moi du charbon", McOrder.Collect), ("syncs chop some wood", McOrder.Collect),
        ("syncs direction 100 64 -200", McOrder.GoTo), ("syncs rdv 100 -200", McOrder.GoTo),
        ("syncs rends-toi en 100 64 -200", McOrder.GoTo),
        // Refusals, read next to the verb only.
        ("syncs ne viens pas", McOrder.None), ("syncs viens plus", McOrder.None), ("syncs ne va pas chercher de bois", McOrder.None),
        ("syncs reste pas avec moi", McOrder.Stay), // « don't stay with me »: she stops following, which is what was meant
        ("syncs protège-moi pas", McOrder.None),
        // Not orders: words that look like one in other sentences.
        ("syncs help", McOrder.Unknown), ("syncs t'es assez forte", McOrder.Unknown), ("c'est à moi syncs", McOrder.Unknown),
        ("syncs tu dors ?", McOrder.Sleep), // she takes it as an invitation; accepted
    })
        Order(text, expected);
    Request("syncs il me faut 20 pierres", McOrder.Collect, resource: McResource.Stone, count: 20);
    Request("syncs abats 5 arbres", McOrder.Collect, resource: McResource.Wood, count: 5);
    Request("syncs trouve-moi des diams", McOrder.Collect, resource: McResource.Diamond, count: McCommand.DefaultCount);
    Request("syncs rdv 100 -200", McOrder.GoTo, "100 -200");

    // How people type in game chat: stretched letters, phonetic spellings, one slip on a long verb.
    foreach (var (text, expected) in new (string, McOrder)[]
    {
        ("syncs viiiiens", McOrder.Come), ("syncs stoooop", McOrder.Stay), ("syncs suis mwa", McOrder.Follow),
        ("syncs viens vers moa", McOrder.Come), ("syncs protge-moi", McOrder.Guard), ("syncs va chercer du bois", McOrder.Collect),
        ("syncs rapplque", McOrder.Come), ("sycns viens", McOrder.Come), ("syncsss viens", McOrder.Come),
        ("chui là syncs", McOrder.Unknown), ("jsuis ici syncs", McOrder.Come), // « je suis ici »: here, so come
        ("syncs viens stp", McOrder.Come), ("syncs suis moi steuplait", McOrder.Follow),
        // Questions and infinitives.
        ("syncs tu peux venir ?", McOrder.Come), ("syncs tu peux me suivre ?", McOrder.Follow),
        ("syncs tu pourrais me protéger ?", McOrder.Guard), ("syncs tu peux rester là ?", McOrder.Stay),
        ("syncs tu peux aller dormir ?", McOrder.Sleep), ("syncs tu peux aller chercher du fer ?", McOrder.Collect),
        ("syncs tu veux bien me rejoindre", McOrder.Come), ("syncs tu peux rester avec moi", McOrder.Follow),
        ("syncs tu peux pas venir ?", McOrder.None),
        // Traps: short words stay exact, so one slip doesn't make another order.
        ("syncs les liens", McOrder.Unknown), ("syncs t'es teste", McOrder.Unknown),
    })
        Order(text, expected);

    // Quantities in words, read only right before the resource.
    Request("syncs va chercher dix bûches", McOrder.Collect, resource: McResource.Wood, count: 10);
    Request("syncs ramène une stack de pierre", McOrder.Collect, resource: McResource.Stone, count: 64);
    Request("syncs il me faut une dizaine de bûches", McOrder.Collect, resource: McResource.Wood, count: 10);
    Request("syncs ramasse un peu de sable", McOrder.Collect, resource: McResource.Sand, count: 5);
    Request("syncs va chercher trois diamants", McOrder.Collect, resource: McResource.Diamond, count: 3);
    Request("syncs va chercher x20 charbon", McOrder.Collect, resource: McResource.Coal, count: 20);
    Request("syncs va chercher 15x fer", McOrder.Collect, resource: McResource.Iron, count: 15);
    Request("syncs va chercher du bois pour une maison", McOrder.Collect, resource: McResource.Wood, count: McCommand.DefaultCount);
    // Coordinates as Minecraft shows them: F3 decimals (floored to the block), x:/z: labels, brackets.
    Request("syncs va XYZ: 120.523 / 64.000 / -300.700", McOrder.GoTo, "120 64 -301");
    Request("syncs va x:120 z:-300", McOrder.GoTo, "120 -300");
    Request("syncs va en (120, 64, -300)", McOrder.GoTo, "120 64 -300");

    void Request(string text, McOrder order, string? coords = null, McResource? resource = null, int count = 0)
    {
        var got = McCommand.Parse(text, mcNames);
        var gotCoords = got.Coordinates is null ? null : string.Join(" ", got.Coordinates);
        if (got.Order != order || gotCoords != coords || got.Resource != resource || (count != 0 && got.Count != count))
            Fail($"request « {text} »: got {got.Order} [{gotCoords}] {got.Resource} x{got.Count}");
    }
    Request("syncs va 120 64 -300", McOrder.GoTo, "120 64 -300");
    Request("syncs viens en 120, 64, -300", McOrder.GoTo, "120 64 -300");
    Request("syncs go -45 812", McOrder.GoTo, "-45 812");
    Request("syncs 120 64 -300", McOrder.GoTo, "120 64 -300");
    Request("syncs tp 0 70 0", McOrder.GoTo, "0 70 0");
    Request("Bob123 syncs viens", McOrder.Come);
    Request("syncs va 120", McOrder.Unknown);
    Request("syncs va chercher 10 bois", McOrder.Collect, resource: McResource.Wood, count: 10);
    Request("syncs ramène-moi 5 bûches", McOrder.Collect, resource: McResource.Wood, count: 5);
    Request("syncs va chercher du fer", McOrder.Collect, resource: McResource.Iron, count: McCommand.DefaultCount);
    Request("syncs mine 200 charbon", McOrder.Collect, resource: McResource.Coal, count: McCommand.MaxCount);
    Request("syncs va chercher de l'or", McOrder.Collect, resource: McResource.Gold);
    Request("syncs récolte du sable", McOrder.Collect, resource: McResource.Sand);

    // In two steps: « syncs », then the order without her name. The service sets `called` for the caller's next
    // message only; with nothing she understands it is let go, never read as another call.
    foreach (var (text, expected) in new (string, McOrder)[]
    {
        ("suis moi", McOrder.Follow), ("viens", McOrder.Come), ("stop", McOrder.Stay), ("protège-moi", McOrder.Guard),
        ("dodo", McOrder.Sleep), ("tu peux venir ?", McOrder.Come), ("120 64 -300", McOrder.GoTo),
        ("va chercher du bois", McOrder.Collect), ("viens pas", McOrder.None), ("mdr t'es nulle", McOrder.None),
        ("ok", McOrder.None), ("je suis là", McOrder.None), ("syncs ?", McOrder.Unknown), ("syncs viens", McOrder.Come),
        ("merci", McOrder.Thanks), ("gg", McOrder.Praise),
    })
    {
        var got = McCommand.Parse(text, mcNames, called: true).Order;
        if (got != expected) Fail($"order after a call « {text} »: got {got}, expected {expected}");
    }
    if (McCommand.Parse("va chercher 5 fer", mcNames, called: true) is not { Order: McOrder.Collect, Resource: McResource.Iron, Count: 5 })
        Fail("order after a call: collect with a count");

    // Crops and farms: a crop word with any gathering verb, « pommes de terre » before « terre » (dirt), a farm before
    // « reste » (stay) and « garde » (guard), and the noun « la récolte » is not an order.
    void Crop(string text, McOrder order, McCrop? crop = null)
    {
        var got = McCommand.Parse(text, mcNames);
        if (got.Order != order || (crop is not null && got.Crop != crop))
            Fail($"crop « {text} »: got {got.Order} {got.Crop}, expected {order} {crop}");
    }
    Crop("syncs récolte le blé", McOrder.Harvest, McCrop.Wheat);
    Crop("syncs va chercher des carottes", McOrder.Harvest, McCrop.Carrot);
    Crop("syncs ramasse les pommes de terre", McOrder.Harvest, McCrop.Potato);
    Crop("syncs il me faut des patates", McOrder.Harvest, McCrop.Potato);
    Crop("syncs cueille les melons", McOrder.Harvest, McCrop.Melon);
    Crop("syncs récolte les citrouilles", McOrder.Harvest, McCrop.Pumpkin);
    Crop("syncs coupe le bambou", McOrder.Harvest, McCrop.Bamboo);
    Crop("syncs récolte la canne à sucre", McOrder.Harvest, McCrop.SugarCane);
    Crop("syncs ramasse les cactus", McOrder.Harvest, McCrop.Cactus);
    Crop("syncs moissonne", McOrder.Harvest, McCrop.Any);
    Crop("syncs récolte", McOrder.Harvest, McCrop.Any);
    Crop("syncs fais la récolte", McOrder.Harvest, McCrop.Any);
    Crop("syncs récolte le champ", McOrder.Harvest, McCrop.Any);
    Crop("syncs harvest the wheat", McOrder.Harvest, McCrop.Wheat);
    Crop("syncs récolte du sable", McOrder.Collect);
    Crop("syncs va chercher du bois dans le champ", McOrder.Collect);
    Crop("syncs va chercher de la terre", McOrder.Collect);
    Crop("syncs la récolte est prête", McOrder.Unknown);
    Crop("syncs récolte pas le blé", McOrder.None);
    Crop("syncs occupe-toi de la ferme", McOrder.Farm);
    Crop("syncs reste à la ferme", McOrder.Farm);
    Crop("syncs garde le champ", McOrder.Farm);
    Crop("syncs entretiens les cultures", McOrder.Farm);
    Crop("syncs cultive", McOrder.Farm);
    Crop("syncs ne t'occupe pas de la ferme", McOrder.None);
    Crop("syncs reste là", McOrder.Stay);
    if (McCommand.Parse("occupe toi du champ", mcNames, called: true).Order != McOrder.Farm) Fail("crop: farm after a call");

    // Harvest, the way people type it: more verbs, misspelt crops, the noun « récolte » after a gathering verb.
    foreach (var (text, crop) in new (string, McCrop)[]
    {
        ("syncs fauche le blé", McCrop.Wheat), ("syncs arrache les carottes", McCrop.Carrot), ("syncs déterre les patates", McCrop.Potato),
        ("syncs farm le blé", McCrop.Wheat), ("syncs récup les betteraves", McCrop.Beetroot), ("syncs ramène des carrotes", McCrop.Carrot),
        ("syncs va chercher des betraves", McCrop.Beetroot), ("syncs récolte les citrouiles", McCrop.Pumpkin),
        ("syncs prends les pastèques", McCrop.Melon), ("syncs ramène la récolte", McCrop.Any), ("syncs replante le champ", McCrop.Any),
        ("syncs replante", McCrop.Any), ("syncs vide le champ", McCrop.Any), ("syncs fais le champ", McCrop.Any),
        ("syncs va récolter les légumes", McCrop.Any), ("syncs tu peux récolter le blé ?", McCrop.Wheat),
        ("syncs ramasse les pom de terre", McCrop.Potato), ("syncs cueille les épis", McCrop.Wheat),
    })
        Crop(text, McOrder.Harvest, crop);

    // Farm: a care verb and a farm word, in any wording.
    foreach (var text in new[]
    {
        "syncs prends soin de mes plantations", "syncs surveille le potager", "syncs occupe-toi du jardin",
        "syncs tu peux t'occuper de la ferme ?", "syncs tu peux garder le champ", "syncs gère la ferme",
        "syncs reste dans les champs", "syncs bosse à la ferme", "syncs occupe-toi du blé", "syncs entretiens les carottes",
        "syncs travaille au champ", "syncs mode fermière", "syncs jardine", "syncs keep the farm", "syncs watch the crops",
        "syncs occupe toi de notre ferme stp",
    })
        Crop(text, McOrder.Farm);

    // Traps: words from these lists in sentences that aren't farm orders.
    foreach (var (text, order) in new (string, McOrder)[]
    {
        ("syncs reste là et ferme-la", McOrder.Stay), // « ferme » without an article is « shut »
        ("syncs garde-moi", McOrder.Guard),
        ("syncs garde des carottes", McOrder.Guard), // keeps some: never a farm or a harvest (« garde » alone was already guard)
        ("syncs il reste du blé ?", McOrder.Stay), // as before: « reste » alone is stay
        ("syncs arrache-toi", McOrder.Unknown), ("syncs prends ça", McOrder.Unknown),
        ("syncs t'as vu mon champ ?", McOrder.Unknown), ("syncs le blé a poussé", McOrder.Unknown),
        ("syncs ne prends pas soin de la ferme", McOrder.None), ("syncs n'arrache pas les carottes", McOrder.None),
    })
        Crop(text, order);

    // Keeping animals: a care verb with an animal word, read before the farm.
    foreach (var text in new[]
    {
        "syncs occupe-toi des animaux", "syncs nourris les vaches", "syncs occupe-toi des animaux de la ferme",
        "syncs prends soin des moutons", "syncs gère l'enclos", "syncs fais de l'élevage", "syncs mode éleveuse",
        "syncs tu peux t'occuper des poules ?", "syncs take care of the animals", "syncs élève des cochons",
    })
        Crop(text, McOrder.Ranch);
    // The camp: up and down, « range le camp » taking it down (not putting her things away).
    foreach (var text in new[] { "syncs monte le camp", "syncs installe un campement", "syncs fais un camp", "monte le camp syncs" })
        Crop(text, McOrder.Camp);
    foreach (var text in new[] { "syncs démonte le camp", "syncs plie le camp", "syncs range le camp", "syncs lève le camp" })
        Crop(text, McOrder.Uncamp);
    Crop("syncs range tes affaires", McOrder.Store);
    // Guarding animals stays a patrol (it must never start a cull); a refusal is nothing.
    Crop("syncs garde les moutons", McOrder.Patrol);
    Crop("syncs surveille les vaches", McOrder.Patrol);
    Crop("syncs occupe-toi pas des animaux", McOrder.None);
    Crop("syncs ne nourris pas les cochons", McOrder.None);
    // Talk about one's own animals, and words one slip from an animal, are no ranch.
    foreach (var text in new[] { "syncs je nourris mes vaches", "syncs occupe-toi des sapins", "syncs enlève les cochons de là" })
        if (McCommand.Read(text, mcNames) == McOrder.Ranch) Fail($"ranch: « {text} » is not one");
    // Her routine's places, by name (McRoutine): a pen word anywhere, a farm or fishing word first.
    foreach (var (keys, expected) in new (string[], string)[]
    {
        (new[] { "ferme", "etang", "maison", "mine de fer" }, "ferme|etang||maison"),
        (new[] { "champ de ble", "ferme aux animaux", "base", "foret" }, "champ de ble||ferme aux animaux|base"),
        (new[] { "lac", "enclos", "potager", "qg", "chez moi" }, "potager|lac|enclos|chez moi"),
        (new[] { "foret", "chemin du lac", "mine" }, "|||"),
    })
    {
        var (farm, fish, ranch, home) = McRoutine.Classify(keys);
        if ($"{farm}|{fish}|{ranch}|{home}" is var got && got != expected) Fail($"routine places [{string.Join(", ", keys)}]: got {got}");
    }

    // Errands, small things, timers, fishing, cooking, guarding a place, remembered places.
    var mcPlaces = new[] { "maison", "mine", "mine de fer", "base 2" };
    void Mc(string text, McOrder order, Func<McRequest, bool>? also = null, bool called = false)
    {
        var got = McCommand.Parse(text, mcNames, called, mcPlaces);
        if (got.Order != order || (also is not null && !also(got)))
            Fail($"order « {text} »: got {got.Order} goods={got.Goods} smelt={got.Smelt} place={got.Place} s={got.Seconds} note={got.Note}");
    }
    // Handing over: everything, or a kind of goods; a possessive turns a gathering word into her own stock.
    Mc("syncs vide tes poches", McOrder.Give, r => r.Goods == McGoods.All);
    Mc("syncs donne-moi tout", McOrder.Give, r => r.Goods == McGoods.All);
    Mc("syncs file moi ton stuff", McOrder.Give, r => r.Goods == McGoods.All);
    Mc("syncs donne-moi tes poissons", McOrder.Give, r => r.Goods == McGoods.Fish);
    Mc("syncs donne moi du poisson", McOrder.Give, r => r.Goods == McGoods.Fish);
    Mc("syncs passe-moi ta pioche", McOrder.Give, r => r.Goods == McGoods.Pickaxe);
    Mc("syncs donne moi ta canne à pêche", McOrder.Give, r => r.Goods == McGoods.Rod);
    Mc("syncs rends-moi mes diamants", McOrder.Give, r => r.Goods == McGoods.Diamond);
    Mc("syncs donne-moi ton fer", McOrder.Give, r => r.Goods == McGoods.Iron);
    Mc("syncs donne moi tes pommes de terre", McOrder.Give, r => r.Goods == McGoods.Produce);
    Mc("syncs donne-moi ta meilleure épée", McOrder.Give, r => r.Goods == McGoods.Weapons);
    Mc("syncs donne moi à manger", McOrder.Give, r => r.Goods == McGoods.Food);
    Mc("syncs donne-moi ton armure", McOrder.Give, r => r.Goods == McGoods.Armour);
    Mc("syncs rends-moi mon armure", McOrder.Give, r => r.Goods == McGoods.Armour);
    // Armour on and off: taken off, it stays in her pockets; with another give verb it's still handed over.
    Mc("syncs mets ton armure", McOrder.Dress);
    Mc("syncs remets ton armure stp", McOrder.Dress);
    Mc("syncs équipe-toi", McOrder.Dress);
    Mc("syncs enfile ton armure", McOrder.Dress);
    Mc("syncs put on your armor", McOrder.Dress);
    Mc("syncs enlève ton armure", McOrder.Undress);
    Mc("syncs retire ton armure", McOrder.Undress);
    Mc("syncs ôte ton armure", McOrder.Undress);
    Mc("syncs take off your armor", McOrder.Undress);
    Mc("syncs enlève ton armure et donne-la moi", McOrder.Give, r => r.Goods == McGoods.Armour);
    Mc("syncs n'enlève pas ton armure", McOrder.None);
    Mc("syncs mets ton armure dans le coffre", McOrder.Store);
    Mc("syncs mets tout dans le coffre", McOrder.Store); // « mets » needs the armour word
    Mc("syncs donne-moi ta clé", McOrder.Give, r => r.Goods is null);
    Mc("syncs donne-moi du fer", McOrder.Collect); // a partitive is still the gathering order
    Mc("syncs file-moi du charbon", McOrder.Collect);
    Mc("syncs ne me donne pas ton poisson", McOrder.None);
    Mc("syncs vide tes poches stp", McOrder.Give, r => r.Goods == McGoods.All); // « poches » is not « pêches »
    // Putting away: a store verb, or anything put into a chest.
    Mc("syncs range tes affaires", McOrder.Store);
    Mc("syncs range", McOrder.Store);
    Mc("syncs vide tes poches dans le coffre", McOrder.Store);
    Mc("syncs mets tout dans le coffre", McOrder.Store);
    Mc("syncs dépose tout", McOrder.Store);
    // Small things.
    Mc("syncs t'es où ?", McOrder.Where);
    Mc("syncs tu es où", McOrder.Where);
    Mc("syncs tes coordonnées", McOrder.Where);
    Mc("syncs donne ta position", McOrder.Where);
    Mc("syncs t'es où ? viens", McOrder.Come); // an order beats the question
    Mc("syncs assis", McOrder.Sit);
    Mc("syncs assieds-toi", McOrder.Sit);
    Mc("syncs saute", McOrder.Jump);
    Mc("syncs saute pas", McOrder.None);
    Mc("syncs fais coucou", McOrder.Wave);
    Mc("salut syncs", McOrder.Wave);
    Mc("coucou syncs !", McOrder.Wave);
    Mc("re syncs", McOrder.Wave);
    Mc("salut syncs, viens", McOrder.Come);
    Mc("salut", McOrder.Wave, called: true);
    // Timers: a verb and a length of time, the note after « de », « pour », « que ».
    Mc("syncs préviens-moi dans 5 min", McOrder.Timer, r => r.Seconds == 300 && r.Note is null);
    Mc("syncs rappelle-moi dans 10 minutes de sortir le fer du four", McOrder.Timer, r => r.Seconds == 600 && r.Note == "sortir le fer du four");
    Mc("syncs rappelle moi de retourner à la base dans 2 min", McOrder.Timer, r => r.Seconds == 120 && r.Note == "retourner à la base");
    Mc("syncs préviens moi dans une demi-heure", McOrder.Timer, r => r.Seconds == 1800);
    Mc("syncs préviens moi dans un quart d'heure", McOrder.Timer, r => r.Seconds == 900 && r.Note is null);
    Mc("syncs minuteur 30s", McOrder.Timer, r => r.Seconds == 30);
    Mc("syncs tu peux me prévenir dans cinq minutes ?", McOrder.Timer, r => r.Seconds == 300);
    Mc("syncs rappelle-moi d'arrêter le four dans 1h", McOrder.Timer, r => r.Seconds == 3600 && r.Note == "arrêter le four");
    Mc("syncs dis-moi où t'es", McOrder.Where); // « dis-moi » without a length of time is no timer
    // What she carries: asked, never fetched nor handed over.
    Mc("syncs t'as quoi sur toi ?", McOrder.Inventory);
    Mc("syncs t'as quoi ?", McOrder.Inventory);
    Mc("syncs qu'est-ce que t'as dans tes poches ?", McOrder.Inventory); // not « vide tes poches »
    Mc("syncs montre ton inventaire", McOrder.Inventory);
    Mc("syncs montre-moi ce que tu as", McOrder.Inventory);
    Mc("syncs ton inv ?", McOrder.Inventory);
    Mc("syncs inventaire", McOrder.Inventory);
    Mc("syncs y a quoi dans ton sac", McOrder.Inventory);
    Mc("syncs t'as combien de trucs ?", McOrder.Inventory);
    Mc("syncs what do you have", McOrder.Inventory);
    Mc("syncs t'as combien de fer ?", McOrder.Have, r => r.Goods == McGoods.Iron);
    Mc("syncs combien de pain t'as ?", McOrder.Have, r => r.Goods == McGoods.Food);
    Mc("syncs t'as combien de poissons", McOrder.Have, r => r.Goods == McGoods.Fish); // « combien »: not a request
    Mc("syncs il te reste combien de pommes de terre ?", McOrder.Have, r => r.Goods == McGoods.Produce); // not stay
    Mc("syncs t'as du fer ?", McOrder.Have, r => r.Goods == McGoods.Iron); // not a trip to the mine
    Mc("syncs t'aurais du bois ?", McOrder.Have, r => r.Goods == McGoods.Wood);
    Mc("syncs t'as quoi comme bois ?", McOrder.Have, r => r.Goods == McGoods.Wood);
    Mc("syncs t'as quoi comme armure ?", McOrder.Have, r => r.Goods == McGoods.Armour); // not « take it off »
    Mc("syncs t'as du fer ? ramène-en 10", McOrder.Collect); // an order beats the question
    Mc("syncs donne-moi ton inventaire", McOrder.Give, r => r.Goods == McGoods.All);
    Mc("syncs range ton inventaire", McOrder.Store);
    Mc("syncs t'as quoi comme épée", McOrder.Give, r => r.Goods == McGoods.Weapons); // goods she can't fetch: a request
    Mc("syncs regarde mes poches", McOrder.Unknown); // the player's pockets, not hers
    Mc("syncs t'as quoi contre moi", McOrder.Unknown);
    if (McCommand.Parse("syncs qu'est-ce que t'as fait ?", mcNames).Order is McOrder.Inventory or McOrder.Have)
        Fail("order « qu'est-ce que t'as fait ? »: read as a question about her pockets");
    // Fishing and cooking.
    Mc("syncs va pêcher", McOrder.Fish);
    Mc("syncs va à la pêche", McOrder.Fish);
    Mc("syncs ramène du poisson", McOrder.Fish);
    Mc("syncs ne pêche pas", McOrder.None);
    Mc("syncs cuis la viande", McOrder.Smelt, r => r.Smelt == McSmelt.Food);
    Mc("syncs fais cuire les patates", McOrder.Smelt, r => r.Smelt == McSmelt.Food);
    Mc("syncs fais fondre le fer", McOrder.Smelt, r => r.Smelt == McSmelt.Ore);
    Mc("syncs fais fondre", McOrder.Smelt, r => r.Smelt == McSmelt.Ore);
    Mc("syncs fais du verre", McOrder.Smelt, r => r.Smelt == McSmelt.Sand);
    // Guarding a place, not a player.
    Mc("syncs surveille la base", McOrder.Patrol, r => r.Place is null);
    Mc("syncs protège la maison", McOrder.Patrol, r => r.Place == "maison");
    Mc("syncs garde le coin", McOrder.Patrol);
    Mc("syncs monte la garde ici", McOrder.Patrol);
    Mc("syncs patrouille", McOrder.Patrol);
    Mc("syncs surveille la mine", McOrder.Patrol, r => r.Place == "mine");
    Mc("syncs protège-moi à la maison", McOrder.Guard); // « moi »: the player
    Mc("syncs monte la garde", McOrder.Guard);
    Mc("syncs garde le champ", McOrder.Farm);
    // Remembered places.
    Mc("syncs retiens ici : maison", McOrder.SavePlace, r => r.Place == "maison");
    Mc("syncs retiens cet endroit comme la mine de fer", McOrder.SavePlace, r => r.Place == "mine de fer");
    Mc("syncs note ici la forêt", McOrder.SavePlace, r => r.Place == "foret");
    Mc("syncs ici c'est la ferme", McOrder.SavePlace, r => r.Place == "ferme");
    Mc("syncs enregistre ici base 2", McOrder.SavePlace, r => r.Place == "base 2");
    Mc("syncs souviens-toi de cet endroit", McOrder.SavePlace, r => r.Place is null);
    Mc("syncs ici c'est trop bien", McOrder.Come); // no article: not a name
    Mc("syncs note ça", McOrder.Unknown);
    Mc("syncs va à la mine", McOrder.GoToPlace, r => r.Place == "mine");
    Mc("syncs va à la mine de fer", McOrder.GoToPlace, r => r.Place == "mine de fer");
    Mc("syncs va à la base 2", McOrder.GoToPlace, r => r.Place == "base 2");
    Mc("syncs rentre à la maison", McOrder.GoToPlace, r => r.Place == "maison");
    Mc("syncs rentre", McOrder.GoToPlace, r => r.Place == "");
    Mc("syncs rentre chez nous", McOrder.GoToPlace, r => r.Place == "chez nous"); // a home word: the service finds the home
    Mc("syncs va au village", McOrder.GoToPlace, r => r.Place == "village"); // unknown: the service says so
    Mc("syncs montre-moi le chemin vers la mine", McOrder.Guide, r => r.Place == "mine");
    Mc("syncs emmène-moi à la maison", McOrder.Guide, r => r.Place == "maison");
    Mc("syncs accompagne-moi jusqu'à la base 2", McOrder.Guide, r => r.Place == "base 2");
    Mc("syncs où est la mine ?", McOrder.WherePlace, r => r.Place == "mine");
    Mc("syncs c'est où la maison", McOrder.WherePlace, r => r.Place == "maison");
    Mc("syncs oublie la mine", McOrder.ForgetPlace, r => r.Place == "mine");
    Mc("syncs oublie ça", McOrder.Unknown);
    Mc("syncs tes lieux", McOrder.ListPlaces);
    Mc("syncs quels endroits tu connais ?", McOrder.ListPlaces);
    Mc("syncs accompagne-moi", McOrder.Follow); // no place: follow, as before
    Mc("syncs va voir", McOrder.Unknown);
    if (McCommand.PlaceKey("Forêt  Noire !") != "foret noire" || McCommand.PlaceName("syncs retiens ici la Forêt", "foret") != "forêt")
        Fail("place key and name");
    // Someone else's place: « de X » or « chez X » (X's home), X one of the players; only when it's about a place.
    var placeOwners = new[] { "Rodhengard", "Papa", "xX_Bob_Xx" };
    void McOf(string text, McOrder order, string? place, string? of)
    {
        var got = McCommand.Parse(text, mcNames, false, mcPlaces, placeOwners);
        if (got.Order != order || got.Place != place || got.Of != of)
            Fail($"order « {text} »: got {got.Order} place={got.Place} of={got.Of}");
    }
    McOf("syncs guide-moi à la maison de Rodhengard", McOrder.Guide, "maison", "Rodhengard");
    McOf("syncs emmène-moi chez Rodhengard", McOrder.Guide, "maison", "Rodhengard");
    McOf("syncs va chez rodhengard", McOrder.GoToPlace, "maison", "Rodhengard");
    McOf("syncs va à la mine de Papa", McOrder.GoToPlace, "mine", "Papa");
    McOf("syncs où est la base de xX_Bob_Xx ?", McOrder.WherePlace, "base", "xX_Bob_Xx");
    McOf("syncs retiens ici : maison de Rodhengard", McOrder.SavePlace, "maison", "Rodhengard");
    McOf("syncs oublie la mine de Rodhengard", McOrder.ForgetPlace, "mine", "Rodhengard");
    McOf("syncs liste les lieux de Rodhengard", McOrder.ListPlaces, null, "Rodhengard");
    McOf("syncs protège la maison de Rodhengard", McOrder.Patrol, "maison", "Rodhengard");
    McOf("syncs va à la mine de fer", McOrder.GoToPlace, "mine de fer", null);
    McOf("syncs va à la maison", McOrder.GoToPlace, "maison", null);
    McOf("syncs va chercher du bois chez Rodhengard", McOrder.Collect, null, null); // wood, wherever it is
    McOf("syncs donne-moi le pain de Rodhengard", McOrder.Give, null, null);
    Covers("McGoodsNames", typeof(McGoods));
    Covers("McSmeltNames", typeof(McSmelt));

    // The ways people type the newer orders, and ordinary chat that must stay quiet: a sweep of game-chat wordings,
    // read with five known places. Expected: the order, optionally « :detail » (goods, smelt, place, seconds).
    var sweepPlaces = new[] { "maison", "mine", "base", "ferme", "village" };
    foreach (var (text, expected) in new (string, string)[]
    {
        //  Give
        ("syncs vide tes poches", "Give:All"), ("syncs vide ton inv", "Give:All"),
        ("syncs vide ton inventaire stp", "Give:All"), ("syncs donne moi tt", "Give:All"),
        ("syncs donne tout ce que tu as", "Give:All"), ("syncs donne moi tout ce que t'as", "Give:All"),
        ("syncs file moi tout", "Give:All"), ("syncs lâche tout", "Give:All"), ("syncs drop tout", "Give:All"),
        ("syncs drop all", "Give:All"), ("syncs balance ton stuff", "Give:All"),
        ("syncs montre ce que t'as", "Inventory"), ("syncs qu'est ce que t'as sur toi", "Inventory"),
        ("syncs rends moi mes affaires", "Give:All"), ("syncs rends moi mon stuff", "Give:All"),
        ("syncs donne ton loot", "Give:All"), ("syncs donne moi tes poissons", "Give:Fish"),
        ("syncs file moi les poissons", "Give:Fish"), ("syncs donne moi le poisson", "Give:Fish"),
        ("syncs t'as du poisson ?", "Give:Fish"), ("syncs passe moi ta canne", "Give:Rod"),
        ("syncs donne moi ta canne à pêche", "Give:Rod"), ("syncs donne moi ton épée", "Give:Weapons"),
        ("syncs file moi ton arme", "Give:Weapons"), ("syncs prête moi ta pioche", "Give:Pickaxe"),
        ("syncs passe moi la pioche", "Give:Pickaxe"), ("syncs donne moi une pioche", "Give:Pickaxe"),
        ("syncs lance moi ta hache", "Give:Axe"), ("syncs donne moi tes outils", "Give:Tools"),
        ("syncs donne moi ton arc", "Give:Bow"), ("syncs donne moi tes flèches", "Give:Arrows"),
        ("syncs donne moi ton armure", "Give:Armour"), ("syncs enlève ton armure", "Undress"),
        ("syncs donne moi à manger", "Give:Food"), ("syncs j'ai faim", "Give:Food"),
        ("syncs t'as à manger ?", "Give:Food"), ("syncs file moi de la bouffe", "Give:Food"),
        ("syncs donne moi du pain", "Give:Food"), ("syncs donne moi tes graines", "Give:Seeds"),
        ("syncs donne moi ton bois", "Give:Wood"), ("syncs rends moi mes diamants", "Give:Diamond"),
        ("syncs donne moi tes diams", "Give:Diamond"), ("syncs file moi ton charbon", "Give:Coal"),
        ("syncs donne moi tes carottes", "Give:Produce"), ("syncs donne moi ta récolte", "Give:Produce"),
        ("syncs give me your fish", "Give:Fish"), ("syncs give me everything", "Give:All"),
        //  Store
        ("syncs range tes affaires", "Store"), ("syncs range ton inventaire", "Store"), ("syncs range tout", "Store"),
        ("syncs range tout dans le coffre", "Store"), ("syncs vide toi dans le coffre", "Store"),
        ("syncs mets tout dans le coffre", "Store"), ("syncs mets tes poissons dans le coffre", "Store"),
        ("syncs dépose tout au coffre", "Store"), ("syncs pose tout dans le coffre", "Store"),
        ("syncs stocke tout", "Store"), ("syncs vide ton inventaire dans le coffre", "Store"),
        ("syncs tu peux ranger tes affaires ?", "Store"), ("syncs fais du rangement", "Store"),
        ("syncs ranges toi", "Unknown"), ("syncs put everything in the chest", "Store"),
        //  Where
        ("syncs t'es où", "Where"), ("syncs t ou", "Where"), ("syncs tes où", "Where"), ("syncs tu es où ?", "Where"),
        ("syncs où es tu", "Where"), ("syncs ou t'es", "Where"), ("syncs t'es où là", "Where"),
        ("syncs t'es passée où ?", "Where"), ("syncs tu es passée où", "Where"),
        ("syncs t'es où exactement", "Where"), ("syncs tes coords", "Where"), ("syncs tes coordonnées stp", "Where"),
        ("syncs donne tes coords", "Where"), ("syncs envoie tes coordonnées", "Where"),
        ("syncs ta position ?", "Where"), ("syncs t'es perdue ?", "Where"), ("syncs where are you", "Where"),
        ("syncs where r u", "Where"), ("syncs coords", "Where"),
        //  Sit
        ("syncs assis", "Sit"), ("syncs assise", "Sit"), ("syncs assieds toi", "Sit"), ("syncs assis toi", "Sit"),
        ("syncs assois-toi", "Sit"), ("syncs couchée", "Sit"), ("syncs sit", "Sit"), ("syncs accroupis toi", "Sit"),
        ("syncs baisse toi", "Sit"),
        //  Jump
        ("syncs saute", "Jump"), ("syncs saute saute", "Jump"), ("syncs sautes", "Jump"),
        ("syncs fais un saut", "Jump"), ("syncs jump", "Jump"), ("syncs sautille", "Jump"),
        ("syncs fais des bonds", "Jump"), ("syncs danse", "Jump"),
        //  Wave
        ("salut syncs", "Wave"), ("slt syncs", "Wave"), ("coucou syncs", "Wave"), ("cc syncs", "Wave"),
        ("yo syncs", "Wave"), ("wesh syncs", "Wave"), ("re syncs", "Wave"), ("bonjour syncs", "Wave"),
        ("bonsoir syncs", "Wave"), ("hello syncs", "Wave"), ("hey syncs", "Wave"), ("hi syncs", "Wave"),
        ("salut sync ça va ?", "Wave"), ("coucou syncs tu vas bien", "Wave"), ("syncs fais coucou", "Wave"),
        ("syncs fais coucou à bob", "Wave"), ("syncs dis bonjour", "Wave"),
        ("syncs dis salut à tout le monde", "Wave"), ("syncs salue tout le monde", "Wave"),
        ("bonne nuit syncs", "Bye"), ("au revoir syncs", "Bye"), ("bye syncs", "Bye"),
        //  Timer
        ("syncs préviens moi dans 5 min", "Timer:300"), ("syncs previens moi dans 5mn", "Timer:300"),
        ("syncs rappelle moi dans 10 min", "Timer:600"), ("syncs rappel moi dans 10 min", "Timer:600"),
        ("syncs rapelle moi dans 10 min", "Timer:600"), ("syncs dans 5 min tu me préviens", "Timer:300"),
        ("syncs dans 5 min rappelle moi de rentrer", "Timer:300"),
        ("syncs préviens moi quand ça fait 5 min", "Timer:300"), ("syncs timer 5 min", "Timer:300"),
        ("syncs lance un timer de 3 min", "Timer:180"), ("syncs mets un minuteur de 10 minutes", "Timer:600"),
        ("syncs minuteur 2 min", "Timer:120"), ("syncs réveille moi dans 20 min", "Timer:1200"),
        ("syncs rappelle moi dans 1h de nourrir les poules", "Timer:3600"),
        ("syncs rappelle moi dans 1 heure", "Timer:3600"), ("syncs rappelle moi dans 1h30", "Timer:5400"),
        ("syncs préviens moi dans 30 secondes", "Timer:30"), ("syncs préviens moi dans 30sec", "Timer:30"),
        ("syncs dis moi quand 5 minutes sont passées", "Timer:300"), ("syncs remind me in 5 min", "Timer:300"),
        ("syncs rappelle moi dans 5 min que le four est prêt", "Timer:300"),
        ("syncs rappelle-moi dans 5 min pour la potion", "Timer:300"),
        ("syncs dans 10 min faut que je parte, préviens moi", "Timer:600"), ("syncs ping moi dans 5", "Timer:300"),
        //  Fish
        ("syncs va pêcher", "Fish"), ("syncs pêche", "Fish"), ("syncs va pecher", "Fish"),
        ("syncs pêche du poisson", "Fish"), ("syncs tu peux pêcher ?", "Fish"), ("syncs va à la pêche", "Fish"),
        ("syncs ramène du poisson", "Fish"), ("syncs il me faut du poisson", "Fish"),
        ("syncs va chercher du poisson", "Fish"), ("syncs attrape des poissons", "Fish"),
        ("syncs fais de la pêche", "Fish"), ("syncs mode pêche", "Fish"), ("syncs go fish", "Fish"),
        ("syncs fish", "Fish"), ("syncs pêche moi des poissons", "Fish"),
        //  Smelt
        ("syncs cuis la viande", "Smelt:Food"), ("syncs cuit la viande", "Smelt:Food"),
        ("syncs fais cuire la viande", "Smelt:Food"), ("syncs fais cuire le poisson", "Smelt:Food"),
        ("syncs cuisine", "Smelt:Food"), ("syncs cuisine le poisson", "Smelt:Food"),
        ("syncs fais à manger", "Smelt:Food"), ("syncs fais griller le steak", "Smelt:Food"),
        ("syncs cuis les patates", "Smelt:Food"), ("syncs fais cuire les pommes de terre", "Smelt:Food"),
        ("syncs fais fondre le fer", "Smelt:Ore"), ("syncs fonds le fer", "Smelt:Ore"),
        ("syncs fond le fer", "Smelt:Ore"), ("syncs fais fondre l'or", "Smelt:Ore"),
        ("syncs fais des lingots", "Smelt:Ore"), ("syncs fais des lingots de fer", "Smelt:Ore"),
        ("syncs mets le fer au four", "Smelt:Ore"), ("syncs mets la viande dans le four", "Smelt:Food"),
        ("syncs fais du verre", "Smelt:Sand"), ("syncs cuis le sable", "Smelt:Sand"),
        ("syncs smelt the iron", "Smelt:Ore"), ("syncs cook the meat", "Smelt:Food"),
        ("syncs utilise le four", "Smelt:Food"),
        //  Patrol
        ("syncs surveille la base", "Patrol:base"), ("syncs surveille la maison", "Patrol:maison"),
        ("syncs garde la base", "Patrol:base"), ("syncs garde la maison", "Patrol:maison"),
        ("syncs protège la base", "Patrol:base"), ("syncs défends la base", "Patrol:base"),
        ("syncs protège le village", "Patrol:village"), ("syncs protège les animaux", "Patrol"),
        ("syncs surveille les vaches", "Patrol"), ("syncs monte la garde ici", "Patrol"),
        ("syncs monte la garde devant la porte", "Patrol"), ("syncs garde l'entrée", "Patrol"),
        ("syncs surveille ici", "Patrol"), ("syncs reste ici et surveille", "Patrol"),
        ("syncs reste ici pour surveiller", "Patrol"), ("syncs reste là et protège", "Patrol"),
        ("syncs garde le coin", "Patrol"), ("syncs patrouille", "Patrol"), ("syncs fais une ronde", "Patrol"),
        ("syncs mode sentinelle", "Patrol"), ("syncs tu peux surveiller la base ?", "Patrol:base"),
        ("syncs protège la base pendant qu'on mine", "Patrol:base"), ("syncs guard the base", "Patrol:base"),
        //  Places
        ("syncs retiens ici : maison", "SavePlace:maison"), ("syncs retiens cet endroit : mine", "SavePlace:mine"),
        ("syncs souviens toi d'ici, c'est la mine", "SavePlace:mine"),
        ("syncs enregistre ici sous le nom base", "SavePlace:base"), ("syncs note ici la mine", "SavePlace:mine"),
        ("syncs ici c'est la maison", "SavePlace:maison"), ("syncs ici c'est chez nous", "SavePlace:maison"),
        ("syncs c'est ici la base", "SavePlace:base"), ("syncs retiens ça comme la maison", "SavePlace:maison"),
        ("syncs mémorise cet endroit maison", "SavePlace:maison"),
        ("syncs sauvegarde la position : portail", "SavePlace:portail"), ("syncs set home", "SavePlace:maison"),
        ("syncs sethome", "SavePlace:maison"), ("syncs ajoute ce lieu : mine", "SavePlace:mine"),
        ("syncs va à la maison", "GoToPlace:maison"), ("syncs va a la mine", "GoToPlace:mine"),
        ("syncs vas à la base", "GoToPlace:base"), ("syncs va au village", "GoToPlace:village"),
        ("syncs rentre à la maison", "GoToPlace:maison"), ("syncs rentre", "GoToPlace"),
        ("syncs rentre à la base", "GoToPlace:base"), ("syncs retourne à la mine", "GoToPlace:mine"),
        ("syncs go home", "GoToPlace"), ("syncs file à la maison", "GoToPlace:maison"),
        ("syncs cours à la maison", "GoToPlace:maison"), ("syncs dirige toi vers la mine", "GoToPlace:mine"),
        ("syncs rejoins la base", "GoToPlace:base"), ("syncs tp maison", "GoToPlace:maison"),
        ("syncs go mine", "GoToPlace:mine"), ("syncs à la maison", "GoToPlace:maison"),
        ("syncs direction la mine", "GoToPlace:mine"), ("syncs montre moi le chemin de la mine", "Guide:mine"),
        ("syncs montre moi le chemin vers la maison", "Guide:maison"), ("syncs montre moi la mine", "Guide:mine"),
        ("syncs emmène moi à la mine", "Guide:mine"), ("syncs emmène moi à la maison", "Guide:maison"),
        ("syncs amène moi au village", "Guide:village"), ("syncs guide moi jusqu'à la base", "Guide:base"),
        ("syncs conduis moi à la ferme", "Guide:ferme"), ("syncs ramène moi à la maison", "Guide:maison"),
        ("syncs c'est par où la mine ?", "WherePlace:mine"),
        ("syncs je suis perdu, ramène moi à la base", "Guide:base"),
        ("syncs on rentre à la maison", "GoToPlace:maison"), ("syncs où est la mine ?", "WherePlace:mine"),
        ("syncs c'est où la maison", "WherePlace:maison"), ("syncs elle est où la base ?", "WherePlace:base"),
        ("syncs la mine c'est où ?", "WherePlace:mine"), ("syncs coords de la mine", "WherePlace:mine"),
        ("syncs les coordonnées de la maison", "WherePlace:maison"), ("syncs oublie la mine", "ForgetPlace:mine"),
        ("syncs efface la maison", "ForgetPlace:maison"), ("syncs supprime la base", "ForgetPlace:base"),
        ("syncs tes lieux", "ListPlaces"), ("syncs quels endroits tu connais ?", "ListPlaces"),
        ("syncs tu connais quels lieux", "ListPlaces"), ("syncs liste des lieux", "ListPlaces"),
        ("syncs tes waypoints", "ListPlaces"), ("syncs les endroits que tu connais", "ListPlaces"),
        //  Not orders: ordinary chat that names her
        ("syncs t'es nulle", "Unknown"), ("syncs t'es où dans le classement ?", "Where"),
        ("syncs la base est jolie", "Unknown"), ("syncs regarde ma maison", "Unknown"),
        ("syncs j'adore la pêche", "Unknown"), ("syncs le fer c'est cuit", "Unknown"),
        ("syncs c'est la maison de bob", "Unknown"), ("syncs ma maison a brûlé", "Unknown"),
        ("syncs t'as vu ma base ?", "Unknown"), ("syncs tu sais où est bob ?", "Unknown"),
        ("syncs la mine est dangereuse", "Unknown"), ("syncs j'ai trouvé un village", "Unknown"),
        ("syncs le four est plein", "Unknown"), ("syncs je range ma chambre", "Unknown"),
        ("syncs bonne idée", "Unknown"), ("syncs t'es drôle", "Unknown"),
        ("syncs elle est où ta maison ?", "WherePlace:maison"), ("syncs c'est quoi ton inventaire", "Inventory"),
        ("syncs j'ai pêché un poisson", "Unknown"), ("syncs tu manges quoi ?", "Unknown"),
        ("syncs je reviens dans 5 min", "Unknown"), ("syncs je pars dans 10 minutes", "Unknown"),
        ("syncs ça fait 5 min que j'attends", "None"), ("syncs t'as des poissons ?", "Give:Fish"),
        ("syncs la base c'est la meilleure", "Unknown"), ("syncs salut la base", "Wave"),
        ("syncs tu me manques", "Unknown"), ("syncs t'as faim ?", "Unknown"), ("syncs je mange", "Unknown"),
        ("syncs je pêche", "Unknown"), ("syncs je cuis la viande", "Unknown"),
        ("syncs je range mon coffre", "Unknown"), ("syncs on se retrouve à la maison", "Unknown"),
        ("syncs ma maison est là", "Unknown"), ("syncs c'est ma base", "Unknown"),
        ("syncs le village est loin", "Unknown"), ("syncs t'as vu la mine ?", "Unknown"),
        ("syncs elle est belle la base", "Unknown"), ("syncs dans 5 min on y va", "Unknown"),
        ("syncs t'as une épée ?", "Give:Weapons"), ("syncs t'as quoi comme épée", "Give:Weapons"),
        ("syncs ta pioche est cassée", "Unknown"), ("syncs je t'ai donné ma pioche", "Unknown"),
        ("syncs j'ai vidé mes poches", "Unknown"), ("syncs regarde mes poches", "Unknown"),
        ("syncs salut c'est moi", "Wave"), ("syncs dis moi tout", "Unknown"),
        ("syncs dis moi ce que tu fais", "Unknown"), ("syncs dis moi t'es où", "Where"), ("syncs bonne nuit", "Bye"),
        ("syncs danse pas", "None"), ("syncs saute dans le vide", "Jump"), ("syncs le four est où ?", "Unknown"),
        ("syncs t'as du charbon ?", "Have:Coal"), ("syncs mets toi là", "Unknown"), ("syncs pose toi là", "Sit"),
        ("syncs j'ai trop faim", "Give:Food"), ("syncs viens on rentre", "Come"),
        ("syncs on va à la mine", "GoToPlace:mine"), ("syncs je vais à la mine", "Unknown"),
        ("syncs je retourne à la base", "Unknown"), ("syncs je rentre", "Unknown"),
        ("syncs tu rentres ?", "GoToPlace"), ("syncs rentre pas", "None"), ("syncs garde mes affaires", "Patrol"),
        ("syncs protège mes affaires", "Patrol"), ("syncs surveille mes coffres", "Patrol"),
        ("syncs la base est protégée ?", "Unknown"), ("syncs reste ici", "Stay"),
        ("syncs reste là je reviens", "Stay"), ("syncs reste ici et attends", "Stay"),
        ("syncs t'es où ? je te vois pas", "Where"), ("syncs coucou t'es où", "Where"),
    })
    {
        var r = McCommand.Parse(text, mcNames, false, sweepPlaces);
        var got = r.Order + (r.Goods is { } g ? ":" + g : "") + (r.Smelt is { } sm ? ":" + sm : "") + (r.Place is { } pl ? ":" + pl : "")
                  + (r.Seconds > 0 ? ":" + r.Seconds : "");
        if (got != expected && !got.StartsWith(expected + ":")) Fail($"sweep « {text} »: got {got}, expected {expected}");
    }

    // What she carries, as she says it: French names with their plural and article, the biggest first, the rest
    // « autres trucs », and never longer than the list's room in her lines.
    McStack S(string id, int count, params string[] goods) => new(id, count, goods);
    var pockets = new McInventory([S("iron_helmet", 1, "armour")], null, null,
        [S("bread", 20, "food"), S("oak_log", 64, "wood"), S("iron_pickaxe", 1, "tools", "pickaxe"), S("bread", 3, "food"),
         S("strange_thing", 2), S("iron_ingot", 12, "iron"), S("raw_iron", 5, "iron")]);
    void Says(string what, string? got, string? expected)
    {
        if (got != expected) Fail($"inventory {what}: got « {got} », expected « {expected} »");
    }
    Says("overview", pockets.Overview(MinecraftBodyService.ListMax),
        "64 bûches de chêne, 23 pains, 12 lingots de fer, 5 fers bruts, une pioche en fer et un autre truc");
    Says("iron", pockets.Of(McGoods.Iron, MinecraftBodyService.ListMax), "12 lingots de fer et 5 fers bruts");
    Says("armour", pockets.Of(McGoods.Armour, MinecraftBodyService.ListMax), "un casque en fer"); // worn counts
    Says("no fish", pockets.Of(McGoods.Fish, MinecraftBodyService.ListMax), null);
    Says("empty", McInventory.Empty.Overview(MinecraftBodyService.ListMax), null);
    Says("unnamed only", new McInventory([], null, null, [S("strange_thing", 2), S("odd_thing", 1)]).Overview(50), "2 trucs");
    var full = new McInventory([], null, null, McItems.All.Take(36).Select((n, i) => S(n.Key, 64 - i)).ToArray());
    if (full.Overview(MinecraftBodyService.ListMax) is not { } fullText || fullText.Length > MinecraftBodyService.ListMax
        || !fullText.EndsWith(" autres trucs"))
        Fail($"inventory full pockets: « {full.Overview(MinecraftBodyService.ListMax)} »");
    // The names go in game chat: plain text Minecraft can draw.
    foreach (var (id, name) in McItems.All)
        if (name.One.Length == 0 || name.Many.Length == 0 || $"{name.One}{name.Many}".Any(c => c is '§' or '{' or '}' || char.IsSurrogate(c)))
            Fail($"item name {id}: « {name.One} » / « {name.Many} »");

    // SRV only where the client library does it: a name, on the default port.
    if (!McAddress.NeedsLookup("play.example.net", 25565) || McAddress.NeedsLookup("play.example.net", 25566)
        || McAddress.NeedsLookup("192.168.1.10", 25565) || McAddress.NeedsLookup("localhost", 25565))
        Fail("minecraft address: when to look up SRV");

    var mcStatus = ServerListPing.Parse("""{"version":{"name":"1.21.4","protocol":769},"players":{"max":20,"online":3,"sample":[{"name":"Alice","id":"x"},{"name":"Bob","id":"y"}]},"description":"hi"}""");
    if (mcStatus is not { Online: 3, Max: 20, Version: "1.21.4" } || !mcStatus.Sample.SequenceEqual(new[] { "Alice", "Bob" }))
        Fail("server list ping: parse");
    if (ServerListPing.Parse("""{"version":{"name":"x"}}""") is not null || ServerListPing.Parse("not json") is not null)
        Fail("server list ping: not a status");

    // Her family in game: matched by name without case, never by an unset name. Each twin exists for both.
    if (McFamily.Of("rodhengard", "Rodhengard", "Analuz") != McKin.Papa || McFamily.Of("ANALUZ", "Rodhengard", "Analuz") != McKin.Tata
        || McFamily.Of("Bob", "Rodhengard", "Analuz") != McKin.Anyone || McFamily.Of("", "", "") != McKin.Anyone
        || McFamily.Of("Bob", "", "") != McKin.Anyone)
        Fail("minecraft family: who is who");
    // Hit streaks (McHitStreaks): the third hit from one player within a minute tips her over, once; other
    // players count apart; a pause longer than the window starts over.
    var hits = new McHitStreaks();
    var hitAt = DateTimeOffset.UtcNow;
    var stages = new[]
    {
        hits.Register("Bob", hitAt), hits.Register("Eve", hitAt.AddSeconds(5)), hits.Register("bob", hitAt.AddSeconds(10)),
        hits.Register("Bob", hitAt.AddSeconds(20)), hits.Register("Bob", hitAt.AddSeconds(30)), hits.Register("Eve", hitAt.AddSeconds(40)),
        hits.Register("Bob", hitAt.AddSeconds(30) + McHitStreaks.Window + TimeSpan.FromSeconds(1)),
    };
    if (!stages.SequenceEqual(new[] { McHitStage.Hit, McHitStage.Hit, McHitStage.Hit, McHitStage.Escalated, McHitStage.Again,
            McHitStage.Hit, McHitStage.Hit }))
        Fail($"minecraft hit streaks: {string.Join(", ", stages)}");
    // Someone hitting her family: a word, then one hit back, then a fight, per attacker, starting over after a pause.
    var familyHits = new McHitStreaks();
    var defence = new[]
    {
        familyHits.Tally("Bob", hitAt), familyHits.Tally("Eve", hitAt.AddSeconds(2)), familyHits.Tally("Bob", hitAt.AddSeconds(5)),
        familyHits.Tally("Bob", hitAt.AddSeconds(8)), familyHits.Tally("Bob", hitAt.AddSeconds(9)),
        familyHits.Tally("Bob", hitAt.AddSeconds(9) + McHitStreaks.Window + TimeSpan.FromSeconds(1)),
    }.Select(McHitStreaks.DefendStage);
    if (!defence.SequenceEqual(new[] { McDefendStage.Warn, McDefendStage.Warn, McDefendStage.Swat, McDefendStage.Fight,
            McDefendStage.Fight, McDefendStage.Warn }))
        Fail($"minecraft family defence: {string.Join(", ", defence)}");

    // Who she works for (McClaim): the holder alone, until their claim runs out, and family outranks.
    var claimAt = DateTimeOffset.UtcNow;
    void Claim(string what, string? holder, McKin holderKin, TimeSpan age, string player, McKin kin, McVerdict expected)
    {
        var got = McClaim.Decide(holder, holderKin, claimAt - age, player, kin, claimAt);
        if (got != expected) Fail($"minecraft claim, {what}: got {got}, expected {expected}");
    }
    var shortly = TimeSpan.FromMinutes(1);
    Claim("free", null, McKin.Anyone, TimeSpan.Zero, "Bob", McKin.Anyone, McVerdict.Take);
    Claim("same player, any case", "Bob", McKin.Anyone, shortly, "bob", McKin.Anyone, McVerdict.Take);
    Claim("someone else", "Bob", McKin.Anyone, shortly, "Eve", McKin.Anyone, McVerdict.Busy);
    Claim("claim ran out", "Bob", McKin.Anyone, McClaim.Expiry, "Eve", McKin.Anyone, McVerdict.Take);
    Claim("Tata takes from anyone", "Bob", McKin.Anyone, shortly, "Ana", McKin.Tata, McVerdict.Take);
    Claim("Papa takes from Tata", "Ana", McKin.Tata, shortly, "Rod", McKin.Papa, McVerdict.Take);
    Claim("nobody takes from Papa", "Rod", McKin.Papa, shortly, "Ana", McKin.Tata, McVerdict.Busy);
    Claim("anyone can't take from Tata", "Ana", McKin.Tata, shortly, "Bob", McKin.Anyone, McVerdict.Busy);
    Claim("Papa's claim runs out too", "Rod", McKin.Papa, McClaim.Expiry, "Bob", McKin.Anyone, McVerdict.Take);
    if (McClaim.Decide("Bob", McKin.Anyone, claimAt - TimeSpan.FromHours(2), "Eve", McKin.Anyone, claimAt, lasting: true) != McVerdict.Busy
        || McClaim.Decide("Bob", McKin.Anyone, claimAt - TimeSpan.FromHours(2), "Ana", McKin.Tata, claimAt, lasting: true) != McVerdict.Take)
        Fail("minecraft claim: a kept farm doesn't run out, family still outranks it");

    var McWideArgs = new HashSet<string>
    {
        "McTimerNoteLines", "McPlaceListLines", "McPlaceSavedLines", "McPlaceMovedLines", "McPlaceNotYoursLines", "McPlaceFullLines",
        "McPlaceUnknownLines", "McPlaceForgottenLines", "McPlaceWhereLines", "McPlaceOtherDimensionLines", "McPlaceGoAckLines",
        "McPlaceArrivedLines", "McGuideAckLines", "McGuideArrivedLines", "McPatrolPlaceAckLines", "McTripTooFarLines",
        "McInventoryLines", "McHaveLines",
    };
    string[] McFamilyPools = { "McCalledLines", "McDeathLines", "McHitByPlayerLines", "McHitAgainLines", "McFedLines", "McAdvancementLines", "McComeAckLines",
        "McFollowAckLines", "McGuardAckLines", "McCollectDoneLines", "McWaveLines", "McGiveDoneLines", "McByeLines", "McCreeperBehindLines", "McDiveLines", "McBoardLines",
        "McPraisedLines", "McThankedLines", "McPortalLines", "McArmouredLines" };
    foreach (var p in McFamilyPools)
    foreach (var twin in new[] { p + "Owner", p + "Tata" })
    {
        if (Field(twin) is not string[] { Length: > 0 } twinLines) { Fail($"{twin}: missing"); continue; }
        // Papa and Tata by title, never by an in-game name.
        if (twinLines.Any(l => l.Contains("{0}"))) Fail($"{twin}: uses {{0}}, they're called Papa and Tata");
    }
    // Pools that only exist as twins, same rule.
    foreach (var twin in new[] { "McSeekMetLinesOwner", "McSeekMetLinesTata", "McSeekMissLinesOwner", "McSeekMissLinesTata",
                 "McTagAlongLinesOwner", "McTagAlongLinesTata" })
        if (Field(twin) is not string[] { Length: > 0 } twinLines) Fail($"{twin}: missing");
        else if (twinLines.Any(l => l.Contains("{0}"))) Fail($"{twin}: uses {{0}}, they're called Papa and Tata");

    // In game there is no Discord markup: no custom emotes, no sticker tags, and Minecraft cuts chat at 256.
    // Markup opens with « <: », « <a: » (emotes), « <@ » (mentions, roles included), « <# » (channels), « <t: » (timestamps).
    var DiscordMarkup = new System.Text.RegularExpressions.Regex(@"<(a?:|@|#|t:)");
    foreach (var f in new[] { "McArrivalLines", "McLeavingLines", "McDeathLines", "McHitByPlayerLines", "McAdvancementLines", "McNightLines",
             "McComeAckLines", "McFollowAckLines", "McStayAckLines", "McArrivedLines", "McTooFarLines", "McNoPathLines", "McLostLines",
             "McCalledLines", "McPraisedLines", "McThankedLines", "McGoToAckLines", "McGoToArrivedLines", "McTripTooFarLines", "McGuardAckLines", "McGuardKillLines",
             "McCollectAckLines", "McCollectDoneLines", "McCollectKeptLines", "McCollectEmptyLines", "McCollectNoneLines",
             "McCollectNoToolLines", "McSleepLines", "McBedtimeLines", "McNoBedLines", "McNotNightLines", "McBedFailedLines",
             "McBusyLines", "McTakenLines", "McHitAgainLines", "McFleeSafeLines", "McCorneredLines", "McFleeDroppedLines", "McDangerDroppedLines", "McTotemLines", "McFamilyHitLines", "McFamilySwatLines", "McFamilyFightLines", "McHungryLines", "McStarvingLines", "McHarvestAckLines", "McHarvestDoneLines", "McHarvestKeptLines", "McHarvestEmptyLines",
             "McHarvestNoneLines", "McFarmAckLines", "McFarmNoneLines", "McFarmNoChestLines", "McSeekMetLinesOwner", "McSeekMetLinesTata", "McSeekMissLinesOwner", "McSeekMissLinesTata",
             "McGiveLostLines", "McGiveNoneLines", "McGiveEmptyLines", "McGiveUnknownLines", "McCantDoLines", "McLinkedLines", "McLinkFailedLines", "McInvitedLines", "McStoreDoneLines", "McStoreFullLines",
             "McStoreEmptyLines", "McStoreNoChestLines", "McSitLines", "McJumpLines", "McWhereLines", "McTimerSetLines", "McTimerDoneLines",
             "McTimerNoteLines", "McTimerTooLongLines", "McTimerFullLines", "McFishAckLines", "McFishNoRodLines", "McFishRodBrokeLines",
             "McFishNoWaterLines", "McFishCatchLines", "McFishTreasureLines", "McFishJunkLines", "McPatrolAckLines", "McPatrolPlaceAckLines",
             "McCreeperLines", "McRetreatLines", "McSmeltAckLines", "McSmeltDoneLines", "McSmeltKeptLines", "McSmeltEmptyLines",
             "McSmeltNothingLines", "McSmeltNoFuelLines", "McSmeltNoFurnaceLines", "McSmeltBusyLines", "McPlaceSavedLines",
             "McPlaceMovedLines", "McPlaceNoNameLines", "McPlaceNotYoursLines", "McPlaceFullLines", "McPlaceUnknownLines",
             "McPlaceForgottenLines", "McPlaceListLines", "McPlaceListEmptyLines", "McPlaceListEmptyOfLines", "McPlaceWhereLines", "McPlaceOtherDimensionLines",
             "McPlaceGoAckLines", "McPlaceArrivedLines", "McGuideAckLines", "McGuideWaitLines", "McGuideArrivedLines", "McGuideLostLines",
             "McRanchAckLines", "McRanchNoneLines", "McRanchNoChestLines",
             "McCampAckLines", "McCampDoneLines", "McCampPartialLines", "McCampNoBedLines", "McCampMissingLines", "McCampNoRoomLines", "McCampExistsLines",
             "McCampRefusedLines", "McUncampAckLines", "McUncampDoneLines", "McUncampNoneLines", "McCampFarLines", "McRoutineFarmLines", "McRoutineFishLines", "McRoutineRanchLines",
             "McRoutineHomeLines", "McTagAlongLinesOwner", "McTagAlongLinesTata", "McRetrieveLines", "McRetrievedLines", "McRetrieveEmptyLines",
             "McRetrieveLostLines", "McToolWornLines", "McToolBrokeLines", "McNeedRodLines", "McNeedShearsLines", "McNeedBreedFoodLines",
             "McNeedArrowsLines", "McPocketsFullLines", "McDiveLines", "McDiveAirLines", "McDrowningLines", "McBreathingLines", "McBoardLines",
             "McShakeLines", "McInventoryLines", "McInventoryEmptyLines", "McHaveLines", "McHaveNoneLines", "McPortalLines",
             "McDressLines", "McDressWornLines", "McArmourNoneLines", "McUndressLines", "McUndressFullLines" }
             .Concat(McFamilyPools.SelectMany(p => new[] { p + "Owner", p + "Tata" })))
    foreach (var (name, lines) in Pools(f))
    {
        // Empty lines are silence on purpose (the body sends nothing for them), and may repeat to weigh it.
        var spoken = lines.Where(l => l.Length > 0).ToArray();
        if (spoken.Distinct().Count() != spoken.Length) Fail($"{name}: duplicate lines");
        foreach (var l in lines)
        {
            string s;
            // The longest of each: a 16-letter name, the widest coordinates, the longest resource name; for the pools
            // whose {1} or {2} carry more (a reminder's note, the list of places, a place's name), the longest of those.
            var wide = McWideArgs.Contains(name.Split('[')[0]);
            try
            {
                s = wide
                    ? string.Format(l, "Abcdefghijklmnop", new string('x', Math.Max(McCommand.NoteMax + 1, MinecraftBodyService.ListMax + 1)),
                        new string('x', McCommand.MaxPlaceName + 4))
                    : string.Format(l, "Abcdefghijklmnop", "-29999999 320 -29999999", "tout ce que j'ai");
            }
            catch (FormatException) { Fail($"{name}: format: {l}"); continue; }
            if (s.Length > 256) Fail($"{name}: {s.Length} characters: {l}");
            // Discord markup (a custom emote, a mention, a timestamp) would show raw in game; a « <3 » is fine.
            if (Sticker.Strip(s) != s || DiscordMarkup.IsMatch(s) || s.Contains('{') || s.Contains('}') || s.Contains('✨') || s.Contains("(｡•́︿•̀｡)"))
                Fail($"{name}: not plain chat text: {l}");
            // Minecraft's font draws the Basic Multilingual Plane one code point at a time: nothing past it (most
            // emoji), no combining marks (« •̀ » lands beside the dot), no format characters, no § (colour codes).
            foreach (var c in s)
                if (char.IsSurrogate(c) || c == '§' || System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) is
                        System.Globalization.UnicodeCategory.NonSpacingMark or System.Globalization.UnicodeCategory.SpacingCombiningMark
                        or System.Globalization.UnicodeCategory.EnclosingMark or System.Globalization.UnicodeCategory.Format)
                {
                    Fail($"{name}: Minecraft can't draw U+{(int)c:X4}: {l}");
                    break;
                }
        }
    }
}

// Talk in game (McTalkReader): chat that isn't an order, read as Discord reads it, answered from the Discord pools made
// fit for the game (McChat.Game). `--mc-review` lists the Discord lines left out of the game, and why (« - »), then every line as she says it in game (« + »).
{
    string[] TalkPool(string field) => (string[])Field(field);
    string[] IntentPool(string field, string intent) =>
        (string[])((IDictionary)Field(field))[Enum.Parse(intentType, intent)]!;
    var talkPicker = new ResponsePicker();
    var talkRandom = new Random(1);

    void SaysInGame(string text, McKin kin, string? kind, string[]? pool = null, bool call = false, bool listening = false,
        bool whispered = false, bool remark = false)
    {
        var got = McTalkReader.Read(text, kin, call, listening, whispered, McTalkReader.AskerId("Bob", kin), null, talkPicker, talkRandom);
        var said = got is { } t ? $"{t.Kind}{(t.Remark ? " (remark)" : "")}" : "nothing";
        if (kind is null ? got is not null
            : got is not { } g || g.Kind != kind || g.Remark != remark || (pool is not null && !ReferenceEquals(g.Pool, pool)))
            Fail($"talk « {text} » ({kin}{(call ? ", call" : "")}{(listening ? ", listening" : "")}{(whispered ? ", whispered" : "")}): got {said}, expected {kind ?? "nothing"}");
    }
    // A call stays a call: « oui ? » comes from the service.
    SaysInGame("syncs", McKin.Anyone, null, call: true);
    SaysInGame("hé syncs ?", McKin.Anyone, null, call: true);
    // Said to her: the intent, else the mood.
    SaysInGame("syncs ça va ?", McKin.Anyone, "HowAreYou", IntentPool("IntentLines", "HowAreYou"), call: true);
    SaysInGame("syncs je t'aime", McKin.Anyone, "Love", IntentPool("IntentLines", "Love"), call: true);
    SaysInGame("je t'aime syncs", McKin.Papa, "Love", IntentPool("OwnerIntentLines", "Love"), call: true);
    SaysInGame("syncs t'es nulle", McKin.Anyone, "comeback", TalkPool("Comebacks"), call: true);
    SaysInGame("syncs t'es nulle", McKin.Papa, "papa mean", TalkPool("OwnerMeanReplies"), call: true);
    SaysInGame("syncs raconte une blague", McKin.Anyone, "Joke", call: true);
    SaysInGame("syncs tu penses quoi de minecraft ?", McKin.Anyone, "Opinion", TalkPool("OpinionLines"), call: true);
    SaysInGame("syncs tu peux me construire une maison ?", McKin.Anyone, "Request", TalkPool("McCantDoLines"));
    SaysInGame("syncs t'es un bot ?", McKin.Anyone, "AreYouABot", call: true);
    SaysInGame("inabot viens voir, syncs", McKin.Anyone, "inabot");
    // About her, not to her: a remark, rationed by the service.
    SaysInGame("syncs a pris mon fer", McKin.Anyone, "talked about", TalkPool("TalkedAboutLines"), remark: true);
    SaysInGame("je joue avec syncs", McKin.Anyone, "talked about", remark: true);
    // Right after she answered: only what's plainly for her, without her name.
    SaysInGame("je t'aime", McKin.Anyone, "Love", listening: true);
    SaysInGame("mdr", McKin.Anyone, "Laughter", listening: true);
    SaysInGame("t'es nul", McKin.Anyone, null, listening: true);
    SaysInGame("on va miner ?", McKin.Anyone, null, listening: true);
    SaysInGame("bonjour", McKin.Anyone, null);
    // A whisper is to her, named or not.
    SaysInGame("ça va ?", McKin.Anyone, "HowAreYou", whispered: true);
    // A refused order is no talk.
    if (!McCommand.Parse("syncs viens pas", ["SYNCS", .. McCommand.HerNames]).Refused) Fail("« syncs viens pas » isn't a refusal");
    if (McCommand.Parse("syncs ça va", ["SYNCS", .. McCommand.HerNames]).Refused) Fail("« syncs ça va » read as a refusal");
    if (!McTalkReader.IsHerName("SYNCS") || McTalkReader.IsHerName("Bob")) Fail("McTalkReader.IsHerName");
    if (McTalkReader.AskerId("Bob", McKin.Anyone) != McTalkReader.AskerId("bob", McKin.Anyone)) Fail("AskerId depends on case");

    // What the game keeps of a Discord line.
    foreach (var (line, expected) in new (string, string?)[]
    {
        ("Je note." + Sticker.Jenote, "Je note."),
        ("Je sais pas gérer ça (>⩊<)" + Sticker.Flustered, "Je sais pas gérer ça"),
        ("Enfin, avec `/schedule create`." + Sticker.Smug, null),
        ("Pourquoi tu me tag ?", null),
        ("Va voir /level.", null),
        ("C'est **moi** la meilleure ✨", "C'est moi la meilleure"),
        ("Ok (enfin presque)", "Ok (enfin presque)"),
        // Her Discord systems, and having no body, which she has in game.
        ("Je compte vos XP. C'est passionnant.", null),
        ("Encore un et je commence à facturer. En cailloux.", null),
        ("Plus que tous les good bot du monde.", null),
        ("Ça va. Toujours pas de bras, mais à part ça, tout va bien.", null),
        ("À plus, {0}. Moi, je bouge pas. Je bouge jamais.", null),
        ("« Mon œil. » J' ai même pas d'œil.", null),
        ("Je veille sur Ping-Qilin. Elle dort.", "Je veille sur Ping-Qilin. Elle dort."),
        ("Inabot ?! Bouge pas ...", "Inabot ?! Bouge pas ..."),
    })
        if (McChat.Line(line) != expected) Fail($"McChat.Line « {line} »: got « {McChat.Line(line) ?? "null"} », expected « {expected ?? "null"} »");

    // Every pool she can answer with in game: something left, every line plain game chat, faces on a quarter at most.
    var talkPools = new[]
    {
        "IntentLines", "OwnerIntentLines", "TataIntentLines", "AboutHerLines", "HowAreYouTempLines", "OpinionLines", "PreferLines",
        "ChoiceLines", "YesLines", "NoLines", "MorningFunFacts", "Comebacks", "OwnerComebacks", "OwnerMeanReplies", "NiceReplies",
        "Greetings", "OwnerGreetings", "TataReplies", "SharingShrugLines", "TalkedAboutLines", "TalkedAboutOwnerLines",
        "BackbiteRoasts", "MistakenIdentityReplies", "ShutdownThreatOwner", "ShutdownThreatTata", "ShutdownThreatReplies",
        "OwnerRaspberryNameLines", "OwnerChildNameLines", "StreakEndLines", "StreakNudgeLines", "McCantDoLines",
    };
    var mcReview = args.Contains("--mc-review");
    var (talkTotal, talkKept) = (0, 0);
    foreach (var f in talkPools)
    foreach (var (name, lines) in Pools(f))
    {
        // Never said in game (McTalkReader leaves these intents to Discord): their lines are slash commands.
        if (name is "IntentLines[NextSession]" or "IntentLines[MyStats]") continue;
        var game = McChat.Game(lines);
        talkTotal += lines.Length;
        talkKept += game.Length;
        if (game.Length == 0) Fail($"{name}: nothing left in game");
        if (game.Count(McChat.HasFace) * 4 > Math.Max(game.Length, 4)) Fail($"{name}: too many faces in game");
        foreach (var l in game)
        {
            var s = string.Format(l, "Abcdefghijklmnop", "mercredi", "x", "y");
            if (s.Length > 256 || Sticker.Strip(s) != s || s.Contains('{') || s.Contains('}') || s.Contains('✨') || s.Contains('<')
                && s.Contains(':') || !McChat.Drawable(s))
                Fail($"{name}: not plain game chat: {l}");
        }
        if (mcReview)
        {
            foreach (var l in lines.Where(l => McChat.Line(l) is null))
                Console.WriteLine($"- [{name}] {McChat.WhyNot(McChat.Clean(l))}: {Sticker.Strip(l)}");
            foreach (var l in game) Console.WriteLine($"+ [{name}] {l}");
        }
    }
    // The verdict an opinion quotes is cleaned at send time: nothing Discord-only may survive it.
    foreach (var f in new[] { "RateHerselfLines", "RateOwnerLines", "RateOwnerSelfLines", "RateCrushLines", "RateNemesisLines",
                 "RateDevilLines", "RateGodLines", "RateTataLines", "RateTataSelfLines", "RateRoutierLines", "RateMitrailletteLines" })
    foreach (var l in TalkPool(f))
        if (McChat.Clean(l) is var c && (c.Contains('`') || c.Contains("<:") || c.Contains("<@") || !McChat.Drawable(c)))
            Fail($"{f}: verdict not plain game chat: {c}");
    Console.WriteLine($"Minecraft talk: {talkKept} of {talkTotal} Discord lines usable in game.");
}

// A call always gets words: an empty line there is a call she seems not to have heard (no « oui ? », no wave).
foreach (var f in new[] { "McCalledLines", "McCalledLinesOwner", "McCalledLinesTata" })
    if (((string[])Field(f)).Any(l => l.Trim().Length == 0)) Fail($"{f}: an empty line, a call left unanswered");

// /minecraft (MinecraftModule and its pure pieces in Helpers/Minecraft).
{
    string[] herNames = ["SYNCS", .. McCommand.HerNames];
    // help: every example parses as the order it illustrates; a talk example reads as talk; every page fits.
    foreach (var s in McHelp.Sections)
    foreach (var e in s.Examples)
    {
        var got = McCommand.Parse(e.Text, herNames, places: ["maison", "mine"]).Order;
        if (e.Order != McOrder.None && got != e.Order) Fail($"/minecraft help « {e.Text} »: parses as {got}, shown as {e.Order}");
        if (e.Order == McOrder.None && McTalkReader.Read(e.Text, McKin.Anyone, true, false, false, 1, null, new ResponsePicker(), new Random(1)) is null)
            Fail($"/minecraft help « {e.Text} »: shown as talk, read as nothing");
    }
    for (var p = 0; p < ProjectSYNCS.Commands.MinecraftModule.HelpPages; p++)
    {
        var embed = ProjectSYNCS.Commands.MinecraftModule.BuildHelp(p);
        if (embed.Length > 6000 || embed.Fields.Any(f => f.Value.Length > 1024)) Fail($"/minecraft help page {p}: over the caps");
        Console.WriteLine($"/minecraft help page {p}: {embed.Length} chars, longest field {embed.Fields.Max(f => f.Value.Length)}");
    }

    // status: what she's doing, in a clause.
    foreach (var (order, who, place, expected) in new (McOrder?, string?, string?, string)[]
    {
        (McOrder.Fish, "Bob", null, "elle pêche pour Bob"),
        (McOrder.Follow, "Papa", null, "elle suit Papa"),
        (McOrder.Patrol, "Bob", "la mine", "elle garde la mine pour Bob"),
        (null, null, null, "elle ne fait rien de spécial"),
    })
        if (McDoing.Describe(order, who, place) != expected)
            Fail($"McDoing.Describe({order}, {who}, {place}): « {McDoing.Describe(order, who, place)} », expected « {expected} »");
    foreach (var order in Enum.GetValues<McOrder>())
        if (string.IsNullOrWhiteSpace(McDoing.Describe(order, "Bob", null))) Fail($"McDoing.Describe({order}): empty");

    // link: a code works once, for the player it was issued for, within 10 minutes, and only the member's latest.
    var codes = new McLinkCodes();
    var lt0 = DateTimeOffset.UnixEpoch;
    var lc = codes.Issue(1, "Bob", lt0);
    if (lc.Length != 4 || !lc.All(char.IsDigit)) Fail("link code: not 4 digits");
    if (codes.Redeem("Alice", lc, lt0) is not null) Fail("link code: redeemed by another player");
    if (codes.Redeem("bob", lc, lt0.AddMinutes(11)) is not null) Fail("link code: redeemed after 10 minutes");
    var lc2 = codes.Issue(1, "Bob", lt0);
    var lc3 = codes.Issue(1, "Bob", lt0.AddSeconds(1));
    if (lc2 != lc3 && codes.Redeem("Bob", lc2, lt0.AddSeconds(2)) is not null) Fail("link code: an old code still works");
    if (codes.Redeem("BOB", lc3, lt0.AddSeconds(2)) != 1) Fail("link code: the right code, any case, refused");
    if (codes.Redeem("Bob", lc3, lt0.AddSeconds(3)) is not null) Fail("link code: used twice");
    if (McLinkCodes.ReadCode("lien 4821") != "4821" || McLinkCodes.ReadCode("Lien : 4821") != "4821"
        || McLinkCodes.ReadCode("syncs viens") is not null || McLinkCodes.ReadCode("lien 48211") is not null)
        Fail("link code: ReadCode");
    foreach (var (name, ok) in new[] { ("Bob_123", true), ("ab", false), ("Rodhengard", true), ("a b", false), ("§cBob", false),
                 ("abcdefghijklmnopq", false) })
        if (McLinkCodes.ValidName(name) != ok) Fail($"link: ValidName(« {name} ») should be {ok}");

    // invite: not allowed, then the owner's leave hold, then already in, then the server down, then already invited, else go.
    var inow = DateTimeOffset.UnixEpoch.AddDays(1);
    var never = DateTimeOffset.MinValue;
    foreach (var (allowed, up, inGame, hold, invited, expected) in new (bool, bool, bool, DateTimeOffset, DateTimeOffset, McInviteVerdict)[]
    {
        (false, true, false, never, never, McInviteVerdict.NotAllowed),
        (false, true, true, inow.AddMinutes(10), inow.AddMinutes(30), McInviteVerdict.NotAllowed),
        (true, true, false, inow.AddMinutes(10), never, McInviteVerdict.Held),
        (true, true, true, never, never, McInviteVerdict.AlreadyIn),
        (true, false, true, never, never, McInviteVerdict.AlreadyIn),
        (true, false, false, never, never, McInviteVerdict.ServerDown),
        (true, false, false, never, inow.AddMinutes(30), McInviteVerdict.ServerDown),
        (true, true, false, never, inow.AddMinutes(30), McInviteVerdict.AlreadyInvited),
        (true, true, false, never, inow.AddSeconds(-1), McInviteVerdict.Go),
    })
        if (McInvite.Decide(allowed, up, inGame, hold, invited, inow) != expected)
            Fail($"McInvite.Decide({allowed}, up {up}, {inGame}, hold, invited): {McInvite.Decide(allowed, up, inGame, hold, invited, inow)}, expected {expected}");
    if (McInvite.Length != TimeSpan.FromHours(1)) Fail("McInvite.Length: an hour");
    // Her arrival credits the inviter only while the invite runs: an invite whose body never spawned is forgotten.
    if (McInvite.Inviter("Bob", inow.AddMinutes(5), inow) != "Bob") Fail("McInvite.Inviter: a running invite");
    if (McInvite.Inviter("Bob", inow.AddSeconds(-1), inow) is not null) Fail("McInvite.Inviter: an expired invite still credited");
    if (McInvite.Inviter(null, inow.AddMinutes(5), inow) is not null) Fail("McInvite.Inviter: nobody invited");

    // status: a whispered order's place or coordinates stay private.
    if (McDoing.ForStatus(McOrder.GoTo, "Bob", "120 64 -300", whispered: true).Contains("120"))
        Fail("McDoing.ForStatus: a whispered order's coordinates shown");
    if (!McDoing.ForStatus(McOrder.Patrol, "Bob", "la mine", whispered: false).Contains("la mine"))
        Fail("McDoing.ForStatus: a public order's place hidden");

    // stats: the tally adds, peeks without emptying, drains; the embed fits, empty or with many players.
    var tally = new McTally();
    tally.Add(McStats.Fish, 2);
    tally.Add(McStats.Fish, 3);
    tally.Add(McStats.Gathered("wood"), 20);
    if (tally.Peek().GetValueOrDefault(McStats.Fish) != 5) Fail("McTally: Add doesn't sum");
    if (tally.Peek().Count != 2) Fail("McTally: Peek emptied it");
    var drained = tally.Drain();
    if (drained.GetValueOrDefault(McStats.Gathered("wood")) != 20 || tally.Peek().Count != 0) Fail("McTally: Drain");
    if (McStats.OrdersBy("BoB") != "orders:bob") Fail("McStats.OrdersBy: lower case");
    var emptyStats = ProjectSYNCS.Commands.MinecraftModule.BuildStats(new Dictionary<string, long>(), 0, s => s);
    if (emptyStats.Length > 6000) Fail("/minecraft stats: empty embed over the cap");
    var crowd = Enumerable.Range(0, 50).ToDictionary(i => McStats.OrdersBy($"Player_{i:D2}_xxxxxx"), i => (long)i);
    foreach (var r in new[] { "wood", "stone", "iron", "coal", "gold", "diamond", "sand", "dirt" }) crowd[McStats.Gathered(r)] = 999_999;
    var crowded = ProjectSYNCS.Commands.MinecraftModule.BuildStats(crowd, 20, s => s);
    if (crowded.Length > 6000 || crowded.Fields.Any(f => f.Value.Length > 1024)) Fail("/minecraft stats: over the caps");
    if (crowded.Fields.Sum(f => f.Value.Split('\n').Count(l => l.Contains("player", StringComparison.OrdinalIgnoreCase))) != 5) Fail("/minecraft stats: not the top 5 players");
    if (!crowded.Fields.Any(f => f.Value.Contains(@"player\_49", StringComparison.OrdinalIgnoreCase))) Fail("/minecraft stats: the top player is missing");
}

// The picker's narrow pick (Minecraft's chat): a line comes back only once all but one of the others were said, and a
// line repeated on purpose (a pool weighing its silence with empty lines) still leaves something to pick.
{
    void Gaps(string[] pool, int minGap)
    {
        var talkPicker = new ResponsePicker();
        var last = new Dictionary<string, int>();
        for (var i = 0; i < 300; i++)
        {
            var l = talkPicker.Pick(pool, 2);
            if (last.TryGetValue(l, out var at) && i - at < minGap)
            {
                Fail($"ResponsePicker.Pick({pool.Length} lines, 2): « {l} » again after {i - at} picks");
                return;
            }
            last[l] = i;
        }
        if (last.Count != pool.Distinct().Count()) Fail($"ResponsePicker.Pick({pool.Length} lines, 2): only {last.Count} lines ever said");
    }
    Gaps(["a", "b", "c", "d", "e", "f"], 5);
    Gaps(["a", "b", "c", "", "", ""], 4);
    Gaps(["a", "b"], 2);
}

// Replies under her cards (MessageCues.ReadCardReply): what each is doing, and what must stay ordinary
// talk. The bool is whether the reply tags someone else, which is what makes « vote » an order.
{
    var readCardReply = cues.GetMethod("ReadCardReply", BindingFlags.Public | BindingFlags.Static)!;
    foreach (var (text, tags, expected) in new (string, bool, string)[]
    {
        // Bringing it back.
        ("remonte le sondage stp", false, "Repost"), ("tu peux le remonter ?", false, "Repost"), ("up", false, "Repost"),
        ("up !", false, "Repost"), ("petit up", false, "Repost"), ("syncs, remets-le en bas", false, "Repost"),
        ("tu peux le mettre à la fin ?", false, "Repost"), ("republie-le", false, "Repost"), ("ramène la carte", false, "Repost"),
        ("tu peux pas le remonter ?", false, "Repost"), ("bump", false, "Repost"),
        ("le remonte pas", false, "None"), ("je suis en bas", false, "None"),
        ("remets-le", false, "Repost"), ("renvoie le sondage", false, "Repost"), ("tu peux le renvoyer ?", false, "Repost"),
        ("on le voit plus", false, "Repost"), ("le sondage est tout en haut", false, "Repost"), ("reposte stp", false, "Repost"),
        ("ramène des chips", false, "None"), ("tu ramènes quoi ?", false, "None"), ("renvoie-moi le lien", false, "None"),
        ("remets ton vote", false, "None"), ("remets la musique", false, "None"), ("on remonte la pente", false, "None"),
        ("je suis noyé de boulot", false, "None"), ("Luca il est perdu", false, "None"), ("tu remontes à quelle heure ?", false, "None"),
        // Rallying the others.
        ("votez !", false, "Rally"), ("allez votez svp", false, "Rally"), ("<@123> vote", true, "Rally"),
        ("<@123> <@456> votez", true, "Rally"), ("vote stp <@123>", true, "Rally"), ("faut voter les gars", false, "Rally"),
        ("oubliez pas de voter", false, "Rally"), ("<@123> tu viens ?", true, "Rally"), ("venez", false, "Rally"),
        ("inscrivez-vous", false, "Rally"), ("<@123> clique", true, "Rally"),
        ("<@123> a voté", true, "None"), ("le vote est nul", false, "None"), ("vote", false, "Done"),
        ("<@123> j'ai voté", true, "Done"), ("votez pas", false, "None"),
        ("<@123>", true, "Rally"), ("<@123> <@456> 👀", true, "Rally"), ("<@123> t'as voté ?", true, "Rally"),
        ("<@123> et toi ?", true, "Rally"), ("<@123> chaud ?", true, "Rally"), ("et vous ?", false, "Rally"),
        ("personne a voté", false, "Rally"), ("bougez-vous", false, "Rally"), ("allez on vote", false, "Rally"),
        ("tout le monde clique", false, "Rally"), ("<@123> t'es chaud ce soir ?", true, "Rally"),
        ("t'as voté ?", false, "None"), ("<@123> je suis d'accord avec toi sur ce point", true, "None"),
        // Saying it's done.
        ("j'ai voté", false, "Done"), ("c'est fait", false, "Done"), ("voté !", false, "Done"), ("a voté", false, "Done"),
        ("j'ai voté pour samedi", false, "Done"), ("je me suis inscrite", false, "Done"), ("fait", false, "Done"),
        ("j'ai pas voté", false, "None"), ("c'est voté", false, "Done"), ("c'est bon", false, "Done"),
        ("voilà", false, "Done"), ("✅", false, "Done"), ("j'ai coché samedi", false, "Done"), ("on a tous voté", false, "Done"),
        ("jsuis inscrit", false, "Done"), ("j'ai mis du temps à venir", false, "None"), ("<@123> c'est bon", true, "None"),
        // Asking how it stands.
        ("c'est quand ?", false, "Status"), ("qui vient ?", false, "Status"), ("on en est où ?", false, "Status"),
        ("on est combien ?", false, "Status"), ("qui a gagné ?", false, "Status"), ("ça finit quand ?", false, "Status"),
        ("pourquoi samedi ?", false, "None"),
        ("c'est quand", false, "Status"), ("qui vient ce soir", false, "Status"), ("alors ?", false, "Status"),
        ("y a combien de votes ?", false, "Status"), ("ça commence à quelle heure ?", false, "Status"),
        ("qui est en tête ?", false, "Status"), ("c'est pour quand ?", false, "Status"),
        ("il est quelle heure ?", false, "None"), ("t'as fait combien de kills ?", false, "None"),
        ("le tournoi commence quand ?", false, "None"), ("résultat je suis mort", false, "None"),
        ("alors t'en penses quoi ?", false, "None"),
        // An answer typed instead of clicked.
        ("samedi pour moi", false, "TextAnswer"), ("je peux pas", false, "TextAnswer"), ("dispo", false, "TextAnswer"),
        ("je viens", false, "TextAnswer"), ("chaud", false, "TextAnswer"), ("moi", false, "TextAnswer"),
        ("plutôt dimanche", false, "TextAnswer"), ("go", false, "TextAnswer"), ("je vote Minecraft", false, "TextAnswer"),
        ("je peux voter ?", false, "None"),
        ("ok pour samedi", false, "TextAnswer"), ("samedi ou dimanche", false, "TextAnswer"), ("samedi ça me va", false, "TextAnswer"),
        ("ça va ce soir", false, "TextAnswer"), ("j'y serai", false, "TextAnswer"), ("compte sur moi", false, "TextAnswer"),
        ("je suis trop chaud", false, "TextAnswer"), ("pareil", false, "TextAnswer"), ("+1", false, "TextAnswer"),
        ("je peux pas venir samedi", false, "TextAnswer"), ("je peux voter pour deux", false, "None"),
        ("il fait chaud ici", false, "None"), ("c'est chaud", false, "None"), ("ça va être chaud ce soir", false, "None"),
        ("je dors chez Luca ce soir", false, "None"), ("à demain", false, "None"), ("bon weekend", false, "None"),
        ("c'est calme ce soir", false, "None"), ("le bot a planté hier soir", false, "None"),
        ("je te dis ça demain", false, "None"), ("ma connexion est nulle ce soir", false, "None"),
        // Ordinary replies: the card's context line, or the usual path.
        ("ok", false, "None"), ("merci", false, "None"), ("t'es nulle", false, "None"), ("", false, "None"),
        ("« votez » qu'il a dit", false, "None"),
    })
    {
        var got = readCardReply.Invoke(null, new object[] { text, tags })!.ToString();
        if (got != expected) Fail($"ReadCardReply(\"{text}\", tags: {tags}) = {got}, expected {expected}");
    }

    // A card is recognised by its buttons, whatever else rides on the message.
    void CardIs(string[] ids, CardRef? expected)
    {
        var got = CardRefs.Read(ids);
        if (got != expected) Fail($"CardRefs.Read({string.Join(", ", ids)}) = {got?.ToString() ?? "null"}, expected {expected?.ToString() ?? "null"}");
    }
    CardIs(["poll:vote:12:40", "poll:close:12"], new CardRef(CardKind.Poll, 12));
    CardIs(["poll:tosession:7"], new CardRef(CardKind.Poll, 7));
    CardIs(["event:join:3", "event:sub:3"], new CardRef(CardKind.Session, 3));
    CardIs(["tonight:in:5"], new CardRef(CardKind.Tonight, 5));
    CardIs(["giveaway:enter:9"], new CardRef(CardKind.Giveaway, 9));
    CardIs(["poll:republish"], null);
    CardIs(["arcade:play:4"], null);
    CardIs([], null);

    // The everyday sweeps again, as if replied under a card: not failures (« je peux pas venir samedi »
    // under a poll is an answer), a count to watch. Run with --card-review to read them.
    var cardReads = neutral.Concat(slang)
        .Select(text => (Text: text, Got: readCardReply.Invoke(null, new object[] { text, false })!.ToString()))
        .Where(r => r.Got != "None")
        .ToList();
    Console.WriteLine($"card sweep: {cardReads.Count} of {neutral.Length + slang.Length} ordinary messages read as a card reply");
    if (args.Contains("--card-review"))
        foreach (var (text, got) in cardReads) Console.WriteLine($"  card: \"{text}\" -> {got}");
}

// 34. Member badges: the catalog, tiers at their edges, derived moments, icon keys.
{
    var mb = MemberBadges.All;
    if (mb.Count(b => b.Kind == MemberBadgeKind.Palier) != 12 || mb.Count(b => b.Kind == MemberBadgeKind.Moment) != 6
        || mb.Count(b => b.Kind == MemberBadgeKind.Secret) != 13) Fail("member badges: catalog counts");
    if (mb.Select(b => b.Key).Distinct().Count() != mb.Count) Fail("member badges: duplicate key");
    foreach (var b in mb)
    {
        if (b.Kind == MemberBadgeKind.Palier != (b.Thresholds.Count == 5)) Fail($"{b.Key}: thresholds");
        if (b.Thresholds.Zip(b.Thresholds.Skip(1)).Any(p => p.First >= p.Second)) Fail($"{b.Key}: thresholds not ascending");
        if (string.IsNullOrWhiteSpace(b.Name) || string.IsNullOrWhiteSpace(b.Hint) || (b.Name + b.Hint).IndexOfAny(['{', '}']) >= 0) Fail($"{b.Key}: name/hint");
        var mbTiers = b.Kind == MemberBadgeKind.Palier ? Enum.GetValues<CollectionTier>().Skip(1).ToArray() : new[] { CollectionTier.None };
        foreach (var t in mbTiers)
        {
            var key = MemberBadges.IconKey(b, t);
            var name = ItemEmojis.EmojiName(key);
            if (!MemberBadges.IsIconKey(key) || name is null || name.Length > 32 || !name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')) Fail($"{key}: icon key");
            if (ItemCatalog.Sets.Any(s => Enum.GetValues<CollectionTier>().Any(ct => ItemEmojis.BadgeKey(s.Key, ct) == key))) Fail($"{key}: clashes with a collection badge");
        }
    }
    var mbNow = new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
    MemberBadgeFacts MbFacts(long voice = 0, long bestMs = 0, DateTimeOffset? joined = null, Dictionary<string, StoredBadge>? stored = null) =>
        new(voice, 0, 0, bestMs, 0, 0, 0, 0, 0, 0, false, joined, stored ?? new());
    BadgeStanding MbOf(MemberBadgeFacts f, string key) => MemberBadges.Evaluate(f, mbNow).Single(s => s.Info.Key == key);
    if (MbOf(MbFacts(voice: 599), "voice").Earned || MbOf(MbFacts(voice: 600), "voice").Tier != CollectionTier.Bronze
        || MbOf(MbFacts(voice: 60000), "voice").Tier != CollectionTier.Diamond) Fail("voice tier edges");
    if (MbOf(MbFacts(), "seniority").Earned) Fail("seniority null");
    if (MbOf(MbFacts(joined: mbNow.AddDays(-90)), "seniority").Tier != CollectionTier.Bronze) Fail("seniority 90 days");
    if (!MbOf(MbFacts(bestMs: 2999), "lightning").Earned || MbOf(MbFacts(bestMs: 3000), "lightning").Earned
        || MbOf(MbFacts(bestMs: 0), "lightning").Earned) Fail("lightning edges");
    var mbStored = new Dictionary<string, StoredBadge> { ["arcade"] = new(5, mbNow), ["hush"] = new(1, mbNow), ["gone_key"] = new(1, mbNow) };
    var mbAll = MemberBadges.Evaluate(MbFacts(stored: mbStored), mbNow);
    if (MbOf(MbFacts(stored: mbStored), "arcade").Tier != CollectionTier.Bronze || !MbOf(MbFacts(stored: mbStored), "hush").Earned) Fail("stored badges");
    if (mbAll.Count != mb.Count || mbAll.Count(s => s.Earned) != 2) Fail("unknown stored key");
    if (MemberBadges.IsAwardable(0, "hush") || MemberBadges.IsAwardable(1, "nope") || MemberBadges.IsAwardable(1, "voice")
        || !MemberBadges.IsAwardable(1, "nanny") || !MemberBadges.IsAwardable(1, "crowned")) Fail("IsAwardable");

    // The card line and the « Succès » view, at their widest.
    var mbEmpty = MemberBadges.Evaluate(MbFacts(), mbNow);
    if (ProjectSYNCS.Commands.MemberBadgeCards.CardLine(new List<ProjectSYNCS.Models.CollectionCompletion>(), mbEmpty) is not null) Fail("empty card line");
    // Worst case: everything at Diamant, every secret found, every icon a real emoji markup.
    var mbMax = new MemberBadgeFacts(99_999_999, 99_999_999, 99_999, 1, 99_999_999, 99_999, 99_999, 99_999, 99_999_999, 99_999, true,
        mbNow.AddYears(-20), mb.ToDictionary(b => b.Key, _ => new StoredBadge(99_999_999, mbNow)));
    var mbDone = ItemCatalog.Sets.Select(s => new ProjectSYNCS.Models.CollectionCompletion { SetKey = s.Key, Count = 999 }).ToList();
    foreach (var b in mb) foreach (var t in Enum.GetValues<CollectionTier>())
        ItemEmojis.Set(MemberBadges.IconKey(b, t), ItemEmojis.Markup("b_" + new string('x', 30), ulong.MaxValue));
    (int Text, int Count, List<string> Ids) V2Measure(Discord.IMessageComponent c)
    {
        var text = 0; var count = 1; var ids = new List<string>();
        void Add((int Text, int Count, List<string> Ids) r) { text += r.Text; count += r.Count; ids.AddRange(r.Ids); }
        switch (c)
        {
            case Discord.ContainerComponent x: foreach (var k in x.Components) Add(V2Measure(k)); break;
            case Discord.SectionComponent x:
                foreach (var k in x.Components) Add(V2Measure(k));
                if (x.Accessory is not null) Add(V2Measure(x.Accessory));
                break;
            case Discord.ActionRowComponent x: foreach (var k in x.Components) Add(V2Measure(k)); break;
            case Discord.TextDisplayComponent x: text += x.Content.Length; break;
            case Discord.ButtonComponent x: if (x.CustomId is not null) ids.Add(x.CustomId); break;
        }
        return (text, count, ids);
    }
    var mbWidest = 0;
    foreach (var (facts, done, what) in new[] { (MbFacts(), new List<ProjectSYNCS.Models.CollectionCompletion>(), "empty view"), (mbMax, mbDone, "worst view") })
    foreach (var section in Enum.GetValues<ProjectSYNCS.Commands.BadgeSection>())
    {
        var view = ProjectSYNCS.Commands.MemberBadgeCards.BuildView(ulong.MaxValue, new string('*', 32), section, done, MemberBadges.Evaluate(facts, mbNow));
        var parts = view.Components.Select(V2Measure).ToList();
        var (text, count, ids) = (parts.Sum(p => p.Text), parts.Sum(p => p.Count), parts.SelectMany(p => p.Ids).ToList());
        mbWidest = Math.Max(mbWidest, text);
        if (text > 4000 || count > 40 || ids.Count != ids.Distinct().Count()) Fail($"{what} {section}: {text} chars, {count} components");
    }
    if (ProjectSYNCS.Commands.MemberBadgeCards.CardLine(mbDone, MemberBadges.Evaluate(mbMax, mbNow)) is not { } mbLine || mbLine.Length > 4000) Fail("full card line");
    const ulong mbSelf = 111, mbAsker = 222, mbOther = 333;
    var mbTata = (ulong)br.GetField("TataId")!.GetValue(null)!; // BotResponses is internal: reflection, like the rest of the file
    bool Heart(string a, string b, ulong asker = mbAsker) => Shipping.IsSelfWithHer(a, b, asker, mbSelf, AvailabilityService.OwnerId, mbTata);
    if (!Heart("syncs", "moi") || !Heart("moi", "toi") || !Heart($"<@{mbAsker}>", "syncs")
        || !Heart("moi", "syncs", AvailabilityService.OwnerId)) Fail("heart: self with her");
    if (Heart("syncs", "pizza") || Heart("syncs", $"<@{mbOther}>") || Heart("moi", "pizza") || Heart("syncs", "toi")) Fail("heart: not self with her");
    var flip = new FeedbackFlip();
    if (flip.Record(1, FeedbackKind.Good, mbNow) || !flip.Record(1, FeedbackKind.Bad, mbNow.AddSeconds(30))) Fail("flip within a minute");
    if (flip.Record(2, FeedbackKind.Good, mbNow) || flip.Record(2, FeedbackKind.Bad, mbNow.AddSeconds(61))) Fail("flip after a minute");
    if (flip.Record(3, FeedbackKind.Good, mbNow) || flip.Record(3, FeedbackKind.Good, mbNow.AddSeconds(5))) Fail("same verdict twice");
    if (flip.Record(4, FeedbackKind.Good, mbNow) || flip.Record(5, FeedbackKind.Bad, mbNow.AddSeconds(5))) Fail("two people");
    ItemEmojis.Clear();

    // GetFactsAsync against a real SQLite: the navigations, the Plynling join and the projection must
    // translate, and the guild scoping must hold. A query that does not translate breaks /level for everyone.
    using (var mbConn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:"))
    {
        mbConn.Open();
        var mbOpts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ProjectSYNCS.Data.AppDbContext>().UseSqlite(mbConn).Options;
        const ulong gA = 1000, gB = 2000, me = 42, other = 43;
        using (var seed = new ProjectSYNCS.Data.AppDbContext(mbOpts))
        {
            seed.Database.EnsureCreated();
            var at = mbNow;
            ProjectSYNCS.Models.SessionEvent Ses(ulong guild, ulong org, DateTimeOffset when, bool cancelled = false) =>
                new() { GuildId = guild, OrganizerId = org, Title = "s", ScheduledAt = when, IsCancelled = cancelled };
            ProjectSYNCS.Models.Participant Part(ulong user, DateTimeOffset? seen) =>
                new() { UserId = user, Username = "u", FirstSeenInVoiceAt = seen };
            // Organised by me in guild A: two past (counted), one cancelled, one future; one past in guild B.
            var sPunctual = Ses(gA, me, at.AddDays(-5)); sPunctual.Participants.Add(Part(me, at.AddDays(-5).AddMinutes(-3)));
            var sLate = Ses(gA, me, at.AddDays(-4)); sLate.Participants.Add(Part(me, at.AddDays(-4).AddMinutes(10)));
            var sCancelled = Ses(gA, me, at.AddDays(-3), cancelled: true); sCancelled.Participants.Add(Part(me, at.AddDays(-3).AddMinutes(-5)));
            var sFuture = Ses(gA, me, at.AddDays(2));
            var sOtherGuild = Ses(gB, me, at.AddDays(-2)); sOtherGuild.Participants.Add(Part(me, at.AddDays(-2).AddMinutes(-5)));
            var sNotSeen = Ses(gA, other, at.AddDays(-1)); sNotSeen.Participants.Add(Part(me, null));
            seed.SessionEvents.AddRange(sPunctual, sLate, sCancelled, sFuture, sOtherGuild, sNotSeen);

            var gw = new ProjectSYNCS.Models.Giveaway { GuildId = gA, Prize = "p", IsClosed = true };
            gw.Entries.Add(new() { UserId = me, IsWinner = true });
            gw.Entries.Add(new() { UserId = other, IsWinner = true });
            var gwLost = new ProjectSYNCS.Models.Giveaway { GuildId = gA, Prize = "p", IsClosed = true };
            gwLost.Entries.Add(new() { UserId = me, IsWinner = false });
            seed.Giveaways.Add(gwLost);
            var gwB = new ProjectSYNCS.Models.Giveaway { GuildId = gB, Prize = "p", IsClosed = true };
            gwB.Entries.Add(new() { UserId = me, IsWinner = true });
            seed.Giveaways.AddRange(gw, gwB);

            var plMine = new ProjectSYNCS.Models.Plynling { GuildId = gA, OwnerId = me, Name = "a" };
            var plOther = new ProjectSYNCS.Models.Plynling { GuildId = gA, OwnerId = other, Name = "b" };
            var plOtherGuild = new ProjectSYNCS.Models.Plynling { GuildId = gB, OwnerId = me, Name = "c" };
            seed.Plynlings.AddRange(plMine, plOther, plOtherGuild);
            seed.SaveChanges();
            seed.PlynlingBadges.AddRange(
                new ProjectSYNCS.Models.PlynlingBadge { PlynlingId = plMine.Id, Key = "k1", EarnedAt = at },
                new ProjectSYNCS.Models.PlynlingBadge { PlynlingId = plMine.Id, Key = "k2", EarnedAt = at },
                new ProjectSYNCS.Models.PlynlingBadge { PlynlingId = plOther.Id, Key = "k1", EarnedAt = at },
                new ProjectSYNCS.Models.PlynlingBadge { PlynlingId = plOtherGuild.Id, Key = "k1", EarnedAt = at });

            seed.MemberXps.Add(new() { GuildId = gA, UserId = me, VoiceMinutes = 700, ReactionsUsed = 9 });
            seed.MemberXps.Add(new() { GuildId = gB, UserId = me, VoiceMinutes = 5, ReactionsUsed = 1 });
            seed.VoiceCompanionStats.Add(new() { GuildId = gA, UserId = me, Minutes = 33 });
            seed.QuizStats.Add(new() { GuildId = gA, UserId = me, Wins = 4, BestMs = 1500 });
            seed.BotFeedbacks.Add(new() { GuildId = gA, UserId = me, GoodCount = 6 });
            seed.MemberBadges.Add(new() { GuildId = gA, UserId = me, Key = "arcade", Count = 3, EarnedAt = at });
            seed.MemberBadges.Add(new() { GuildId = gB, UserId = me, Key = "hush", Count = 1, EarnedAt = at });
            seed.SaveChanges();
        }
        using var read = new ProjectSYNCS.Data.AppDbContext(mbOpts);
        try
        {
            var f = await new MemberBadgeService(read).GetFactsAsync(gA, me, mbNow.AddDays(-100), mbNow);
            if (f.VoiceMinutes != 700 || f.Reactions != 9) Fail($"facts sql: xp {f.VoiceMinutes}/{f.Reactions}");
            if (f.CompanionMinutes != 33 || f.QuizWins != 4 || f.QuizBestMs != 1500 || f.GoodBot != 6) Fail("facts sql: companion/quiz/goodbot");
            if (f.GiveawaysWon != 1) Fail($"facts sql: giveaways won {f.GiveawaysWon}");
            if (f.PlynlingBadges != 2) Fail($"facts sql: plynling badges {f.PlynlingBadges}");
            if (f.SessionsOrganised != 2) Fail($"facts sql: organised {f.SessionsOrganised}");
            if (f.SessionsAttended != 2) Fail($"facts sql: attended {f.SessionsAttended}");
            if (!f.Punctual) Fail("facts sql: punctual");
            if (f.Stored.Count != 1 || f.Stored["arcade"].Count != 3) Fail("facts sql: stored badges");
            var none = await new MemberBadgeService(read).GetFactsAsync(gA, 999, null, mbNow);
            if (none.VoiceMinutes != 0 || none.SessionsAttended != 0 || none.Punctual || none.Stored.Count != 0) Fail("facts sql: stranger");
        }
        catch (Exception ex) { Fail("facts sql: " + ex.Message); }
    }
    Console.WriteLine($"succès view at worst: {mbWidest} chars");
}

Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} FAILURE(S)");
return failures == 0 ? 0 : 1;
