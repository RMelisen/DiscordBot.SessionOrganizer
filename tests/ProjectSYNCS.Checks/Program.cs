using System.Collections;
using System.Reflection;
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
    "SharingShrugLines",
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
Covers("IntentLines", intentType, "None", "Opinion", "Request", "Ship", "Help", "AboutHer");
Covers("HelpCommands", asm.GetType("ProjectSYNCS.Services.RequestTopic")!);
Covers("AboutHerLines", asm.GetType("ProjectSYNCS.Services.HerTopic")!);
var topicType = asm.GetType("ProjectSYNCS.Services.RequestTopic")!;
Covers("RequestLines", topicType);
Covers("RequestCommands", topicType);
Covers("ReplyContextLines", typeof(ReplyContext));
Covers("ChatFactLines", typeof(ChatFact));
var owner = (IDictionary)Field("OwnerIntentLines");
if (owner.Count != 16) Fail($"OwnerIntentLines has {owner.Count} keys, expected 16");

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
    ("pourquoi t'as dit ça", "Question"), ("est-ce que tu viens ce soir", "Question"), ("quand est-ce qu'on joue", "Question"),
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
    .ToList();
if (allKinds.Distinct().Count() != allKinds.Count) Fail("duplicate answer kind keys");
if (!AnswerKinds.IsUnrecognised(AnswerKinds.Comeback) || AnswerKinds.IsUnrecognised(AnswerKinds.Nice)
    || !AnswerKinds.IsUnrecognised(AnswerKinds.Fact(ChatFact.Broke))) Fail("IsUnrecognised misreads");

var log = new UnansweredLog();
for (var i = 0; i < 150; i++) log.Add(1, ChatPath.Reply, AnswerKinds.Comeback, $"message {i}\nligne", t0.AddSeconds(i));
log.Add(2, ChatPath.Mention, AnswerKinds.Interrogation, new string('x', 500), t0);
var kept = log.For(1);
if (kept.Count != 99 || kept[0].Text != "message 149 ligne") Fail($"examples log kept {kept.Count}, newest '{kept.FirstOrDefault().Text}'");
if (log.For(2).Single().Text.Length != UnansweredLog.MaxTextLength) Fail("long text not capped");
if (typeof(UnansweredMessage).GetProperties().Any(p => p.Name.Contains("User") || p.Name.Contains("Author"))) Fail("an example stores its author");

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
var file = (string)buildExamples.Invoke(null, new object[] { log.For(1) })!;
if (!file.Contains("message 149")) Fail("examples file misses a line");

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
    ("t'es dans le faux", "YouAreWrong"), ("tu délires", "YouAreWrong"), ("nimp", "YouAreWrong"), ("tu mens", "YouAreWrong"),
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
    ("qui vient ce soir", "Question"), ("où est le lien", "Question"), ("c'est quand la session", "Question"),
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
var herTopic = cues.GetMethod("ReadHerTopic", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) })!;
foreach (var (text, topic) in new[] { ("t'as quel âge ?", "Age"), ("ton anniv c'est quand", "Birthday"), ("tu vis où ?", "Home"), ("ton plat préféré ?", "Favourite") })
    if (herTopic.Invoke(null, new object[] { text })?.ToString() != topic) Fail($"ReadHerTopic(\"{text}\") != {topic}");
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
    ("Question", new[] { "c'est quoi le jeu de ce soir ?", "pourquoi t'as dit ça", "qui vient ce soir ?", "il est où le lien ?" }),
    ("Sharing", new[] { "j'ai mangé de la soupe", "on a perdu la game", "je viens de rentrer du boulot", "devine quoi, j'ai gagné le tournoi", "mon chat a vomi sur le clavier" }),
    ("LowEffort", new[] { "ok", "ah bon", "bah oui", "d'accord", "hein" }),
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

// Every intent the enum has is covered by the table above, so a new one can't ship untested.
var covered = byIntent.Select(b => b.Intent).ToHashSet();
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

Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} FAILURE(S)");
return failures == 0 ? 0 : 1;
