using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Which emotion a message reads as. Deliberately limited to the three readings
// the response pools in BotResponses actually cover — there is no point
// detecting a mood nothing can answer.
internal enum EmotionKind
{
    None,
    Nice,
    Mean,
}

/// <summary>
/// What <see cref="MessageCues"/> made of a message. Emotion and greeting are
/// separate axes on purpose: "Salut, t'es nulle" is both a greeting and an insult,
/// and collapsing them into one label would throw one of those facts away.
/// </summary>
internal readonly record struct MessageMood(EmotionKind Emotion, bool IsGreeting)
{
    public static readonly MessageMood Neutral = new(EmotionKind.None, false);
}

// Someone passing verdict on the bot itself — "good bot" / "bad bot". Detected
// separately from the mood axes rather than as another cue, because it is a
// verdict on *her*, not a mood, and because folding it into the scoring would
// make "good bot" read as a compliment too and fire two responses at once.
// Public (like ResponsePicker and AvailabilityService) because the public
// BotFeedbackService takes it, which the public /goodbot module injects.
public enum FeedbackKind
{
    None,
    Good,
    Bad,
}

// Papa's pet names for her (MessageCues.ReadPetName): the raspberry ones, after the board in the
// attic, and the daughter ones. Each family gets its own pool.
internal enum PetName
{
    None,
    Raspberry,
    Child,
}

/// <summary>Which wording carried a verdict.</summary>
/// <remarks>
/// Separate from <see cref="FeedbackKind"/> because the two are independent: the tally
/// only cares that praise is praise, while the *answer* differs — "good bot" earns a
/// generic nice reaction, "good girl" earns a decidedly different register. Keeping it
/// out of FeedbackKind avoids four states where two and two are meant.
/// </remarks>
public enum VerdictForm
{
    /// <summary>"good bot" / "bad bot" and their French equivalents.</summary>
    Bot,
    /// <summary>"good girl" / "bad girl".</summary>
    Girl,
}

// What a message aimed at her is *doing* — asking how she is, saying goodnight, laughing — so
// ChatterService can answer the content rather than the mood alone. A separate axis from the
// mood, like the verdict: « je t'aime » is both nice and a declaration, and the declaration is
// what she answers. Everything before Question is "specific" and outranks a reply's context;
// Question, Sharing and LowEffort only catch what nothing more precise did. Free to reorder, never
// to rename: /debug answers stores each by name (AnswerKinds.Intent). Yes and No are the one pair
// ReadIntent never returns: a bare « oui » only answers something under one of her questions, so
// ChatterService reads them with ReadAnswer on the reply path.
internal enum ChatIntent
{
    None,
    Love,
    DemandApology,
    Apology,
    ShutUp,
    DoYouLoveMe,
    HowAreYou,
    AreYouThere,
    AnswerMe,
    WhoAreYou,
    RememberMe,
    MyStats,
    AboutHer,
    AreYouABot,
    AreYouUpset,
    Crush,
    WhatAreYouDoing,
    Ship,
    TakeSides,
    Opinion,
    RateMe,
    Blame,
    NextSession,
    ShouldI,
    Roll,
    Joke,
    FunFact,
    Help,
    Request,
    Down,
    Bored,
    Goodnight,
    Goodbye,
    Disbelief,
    Maybe,
    Yes,
    No,
    YouAreRight,
    YouAreWrong,
    Thanks,
    Laughter,
    Question,
    Sharing,
    LowEffort,
}

// What an AboutHer question asks (BotResponses.AboutHerLines). In memory only: free to reorder.
internal enum HerTopic
{
    Age,
    Birthday,
    Home,
    Favourite,
    Version,
}

// What a Request asks her to do, so she can point at the command that does it
// (BotResponses.RequestLines). In memory only: free to reorder.
internal enum RequestTopic
{
    Other,
    Session,
    Tonight,
    Poll,
    Reminder,
    Teams,
    Giveaway,
    Quiz,
    Moderation,
}

// Lightweight intent detection over message text: is this a compliment, a
// greeting, or an insult? Matching is done on tokenized, lowercased,
// accent-stripped words so "Félicitations !" matches the cue "felicitations".
//
// Cues are **weighted, not boolean**. A single unambiguous word ("abruti",
// "merci") is enough on its own; an ambiguous one ("cool", "rate", "claque" —
// "ça claque" is a compliment in French) scores too low to fire alone and needs
// corroboration from another cue or from emphasis. That, plus negation handling,
// is what separates this from a plain word-list lookup: the old version read
// "c'est pas nul" as an insult and "t'es pas gentille" as a compliment.
internal static partial class MessageCues
{
    // The "hi_cat" waving emote counts as a greeting on its own. Matched by its
    // ID so a rename of the emote won't break detection — a message body carries the
    // full <:name:id> markup, so the id is present verbatim in the text.
    private const string GreetingEmoteId = Emotes.HiCatId;

    // Custom emotes that read as affection on their own, matched by ID for the same
    // reason as the greeting one.
    private static readonly string[] _niceEmoteIds =
    {
        Emotes.McHeartId,
        Emotes.CatHeartId,
    };

    // ---- Scoring ----------------------------------------------------------
    // A message fires an emotion once its score reaches Threshold. The weights are
    // chosen so one strong cue clears the bar by itself, one weak cue never does,
    // and two weak cues corroborating each other just do ("c'est mauvais et fade").
    private const double StrongWeight = 1.0;
    private const double WeakWeight = 0.4;
    private const double PhraseWeight = 1.2;     // multi-word cues are rarely ambiguous
    private const double NiceSymbolWeight = 1.0; // ❤ / 🥰 / mcheart carry no other reading
    private const double MeanSymbolWeight = 0.4; // 💀 usually means "dead laughing", not hostility
    private const double Threshold = 0.8;

    // Emphasis is added only to a side that already scored, so shouting
    // "BONJOUR" can't manufacture hostility out of nothing.
    private const double CapsBonus = 0.5;
    private const double ElongationBonus = 0.4;
    private const double ExclamationBonus = 0.25;
    private const double MaxExclamationBonus = 0.5;

    // How many tokens back a negator reaches. For adjectives and nouns French
    // negation precedes the word it cancels — "pas nul", "jamais vu un truc aussi
    // nul".
    private const int NegationWindow = 3;

    // Verbs are the exception: the negation wraps them ("ne ... pas"), and chat
    // French drops the "ne", so the negator lands *after* the cue — "j'aime pas",
    // "je kiffe pas". Looking forward is therefore required, but only for verbs:
    // doing it for every cue would let "merci, pas de souci" cancel its own thanks.
    private const int ForwardNegationWindow = 2;

    // The cue words that are verbs, and so take the forward negation window above.
    private static readonly HashSet<string> _verbCues = new()
    {
        "adore", "adores", "aime", "kiff", "kiffe", "kiffer",
        "casse", "claque", "degage", "deteste", "eclate", "ferme", "hais", "loupe",
        "rate", "saoule", "saoules", "soule", "soules", "soulent",
    };

    // Words that cancel a cue that follows them. A cancelled cue scores nothing
    // rather than counting for the opposite side: "c'est pas nul" is mildly
    // positive, but inferring how positive is guesswork, and staying silent is a
    // cheaper mistake than answering the wrong mood.
    //
    // "plus" is deliberately included. Chat French almost always drops the "ne",
    // making it a very common negator ("j'en peux plus", "c'est plus la peine").
    // It does cost the comparative reading ("plus génial"), which is rarer and
    // usually carries a "que" — the trade is a swallowed compliment now and
    // then, never a misfired insult.
    private static readonly HashSet<string> _negators = new()
    {
        "aucun", "aucune", "guere", "jamais", "ni", "non", "nullement", "pas", "plus", "rien", "sans",
    };

    // Cue words that flag a kind message.
    private static readonly string[] _niceCues =
    {
        // No "boss" and no "monstre", despite both being compliments in French chat
        // ("t'es un monstre"). This server organises *gaming* sessions, where they
        // overwhelmingly mean the enemy — "il est fort ce boss" was reading as praise.
        "adorable", "adorables", "adorbs", "adore", "adores", "aime", "amazing", "attentionne", "attentionnee", "awesome",
        "banger", "beau", "bebou", "belle", "best", "bg", "bienveillant", "bienveillante", "bisous", "bravissimo", "bravo", "brillant", "brillante",
        "calin", "calins", "carre", "chaleureuse", "chaleureux", "champion", "championne", "chapeau", "chou", "choupi", "choupinou", "classe", "clean", "coeur", "content", "contente", "cool", "courage", "cracke", "crackee", "craquant", "craquante", "cute",
        "dingue", "divin", "divine", "douce", "doue", "douee", "douees", "doues", "doux", "drole", "droles",
        "efficace", "epique", "excellent", "excellente", "exceptionnel", "exceptionnelle", "extraordinaire",
        "fantastique", "felicitation", "felicitations", "fier", "fiere", "formidable", "fort", "forte",
        "genereuse", "genereux", "genial", "geniale", "geniales", "geniaux", "genie", "gentil", "gentille", "gentilles", "gentils", "gg", "gj", "goat", "goated", "great",
        "heroine", "heros", "heureuse", "heureux",
        "iconique", "impec", "impeccable", "incroyable", "incroyables", "insane", "intelligent", "intelligente",
        "joli", "jolie",
        "kiff", "kiffe", "kiffer", "king",
        "legendaire", "legende", "love", "lovely",
        "magnifique", "magnifiques", "maligne", "malin", "marrant", "marrante", "marrants", "meilleur", "meilleure", "meilleures", "meilleurs", "merci", "mercii", "merciii", "merveilleuse", "merveilleux", "mignon", "mignonne", "mignonnes", "mignons", "mrc", "mvp",
        "nice", "nickel",
        "ouah", "ouf",
        "parfait", "parfaite", "parfaites", "parfaits", "pepite", "perle", "precieuse", "precieux", "pro", "propre",
        "queen",
        "ravi", "ravie", "reine", "respect", "rigolo", "rigolote", "roi", "royal", "royale",
        "sauveur", "sauveuse", "slay", "solide", "splendide", "style", "stylee", "sublime", "super", "superbe", "sympa", "sympas",
        "talentueuse", "talentueux", "thanks", "thx", "top", "tresor", "trognon", "tuerie", "ty",
        "utile",
        "waw", "wonderful", "wow", "wp",
    };

    // Emoji/symbol cues for a kind message.
    private static readonly string[] _niceSymbols =
    {
        "❤", "♡", "🥰", "😍", "💖", "😊", "💕", "💗", "💞", "💘", "💝",
        "🫶", "🫂", "😘", "🥹", "😻", "😽", "🤗", "🥳",
    };

    // Emoji/symbol cues for a hostile message. All weak: every one of these gets
    // used affectionately or as a joke at least as often as not, so they need
    // backing from an actual word before they count.
    private static readonly string[] _meanSymbols =
    {
        "💀", "🤡", "🖕", "😡", "🤬", "🤮", "🤢", "🙄", "😒", "💩", "👎",
    };

    // Cue words that flag a greeting.
    //
    // The stretched variants ("coucouu", "heyyy", "helloo") are kept even though
    // Analyze squashes repeats, because squashing reaches the base cue only when the
    // base has no double of its own: "heyy" squashes to "hey", but "helloo"
    // squashes to "helo", not "hello". Cheaper to list them than to squash the cue
    // side too, which would collide innocent words onto cues ("salle" -> "sale").
    private static readonly string[] _greetingCues =
    {
        "aloha", "annyeong",
        "bjour", "bjr", "bonjour", "bonsoir", "bsr",
        "cc", "coucou", "coucouu", "coucouuu",
        "ello",
        "hello", "helloo", "hellooo", "helloooo", "hey", "heyy", "heyyy", "heyyyy", "heyyyyy", "hi", "hola", "holla",
        "kikoo", "kikou", "kilou",
        "plop", "pouet", "pweeet", "pweet", "pwet",
        "re", "rebonjour", "rebonsoir",
        "salut", "salutations", "slt", "sup",
        "yo", "yoo", "yooo", "yop", "yosh",
    };

    // Cue words that flag a mean message.
    private static readonly string[] _meanCues =
    {
        "abruti", "abrutie", "abruties", "abrutis", "affligeant", "affligeante", "affreuse", "affreux", "agacant", "agacante", "agacants", "arrogant", "arrogante", "atroce", "atroces",
        "barbant", "barbante", "batard", "batarde", "batards", "beauf", "bete", "betes", "betise", "betises", "bidon", "blaireau", "blaireaux", "blase", "boiteuse", "boiteux", "boloss", "bouffon", "bouffonne", "bouffons", "boulet", "boulette",
        "casse", "cassos", "cheh", "chelou", "chiant", "chiante", "chiantes", "chiants", "claque", "claquee", "clown", "clowns", "consternant", "consternante", "cretin", "cretine", "cretines", "cretins", "crevard", "crevards", "cringe",
        "debile", "debiles", "decevant", "decevante", "degage", "degueu", "degueulasse", "deplorable", "deplorables", "detestable", "deteste",
        "eclate", "eclatee", "enfoire", "enfoiree", "enfoires", "ennuyeuse", "ennuyeux", "execrable",
        "fade", "ferme", "foireuse", "foireux",
        "gogol", "gonflant", "gonflante", "grotesque", "grotesques", "gueguerre", "guignol", "guignols",
        "hais", "horrible", "horribles", "hypocrite", "hypocrites",
        "idiot", "idiote", "idiotes", "idiots", "imbecile", "imbeciles", "immonde", "immondes", "incompetent", "incompetente", "insipide", "insolent", "insolente", "insupportable", "insupportables", "inutile", "inutiles",
        "laid", "laide", "laides", "laids", "lamentable", "lamentables", "loser", "loupe", "loupee", "lourd", "lourde",
        "manchot", "manchote", "mauvais", "mauvaise", "mediocre", "mediocres", "menteur", "menteurs", "menteuse", "menteuses", "merde", "merdique", "merdiques", "minable", "minables", "moche", "moches",
        "navrant", "navrante", "nawak", "naze", "nazes", "noob", "noobs", "nul", "nullard", "nullarde", "nulle", "nulles", "nullissime", "nullos", "nuls",
        "ordure", "ordures",
        "pathetique", "pathetiques", "penible", "penibles", "pitoyable", "pitoyables", "pouilleuse", "pouilleux", "pourri", "pourrie", "pourries", "pourris", "pourriture", "pourritures", "pretentieuse", "pretentieux",
        "quoka", "quokka",
        "raclure", "rate", "ratee", "ratees", "rates", "relou", "reloue", "relous", "ridicule", "ridicules", "risible", "risibles",
        "salaud", "saoulant", "saoulante", "saoule", "saoules", "soulant", "soulante", "soule", "soulent", "soules", "stupide", "stupides",
        "teube", "tocard", "tocarde", "toxique", "toxiques", "trash",
        "useless",
        "vantard", "vantarde", "vilain", "vilaine",
        "zero", "zinzin",
    };

    // Cues that carry WeakWeight instead of StrongWeight, because they are at
    // least as likely to appear in an innocent sentence as an emotional one:
    // "j'ai cassé mon record", "ferme la porte", "le boss a raté", "zéro mort",
    // "ça claque" and "c'est propre" (compliments!), "je m'éclate" (having fun),
    // "je suis content" (about themselves, not about you), "merde" (frustration,
    // not an insult). One of these alone will not fire; two of them, or one with
    // emphasis behind it, will.
    private static readonly HashSet<string> _weakCues = new()
    {
        // Ambiguous mean cues.
        "bete", "betes", "betise", "betises", "bidon", "blase", "boulet", "boulette",
        "casse", "chelou", "claque", "claquee",
        "eclate", "eclatee",
        "fade", "ferme",
        "gueguerre",
        "loupe", "loupee",
        "manchot", "manchote", "mauvais", "mauvaise", "merde",
        "rate", "ratee", "ratees", "rates",
        "toxique", "toxiques",
        "saoule", "saoules", "soule", "soulent", "soules",
        "trash",
        "vilain", "vilaine",
        "zero", "zinzin",

        // Ambiguous nice cues. "heros", "reine", "roi" and "royal" all name things a
        // game has as often as they compliment a person, same reason "boss" and
        // "monstre" are absent from the cue list entirely.
        "beau", "belle", "best",
        "carre", "classe", "content", "contente", "cool", "courage", "cracke", "crackee",
        "dingue", "divin", "divine",
        "fier", "fiere", "fort", "forte",
        "great",
        "heroine", "heros", "heureuse", "heureux",
        "insane",
        "joli", "jolie",
        "nice",
        "ouah", "ouf",
        "pro", "propre",
        "ravi", "ravie", "reine", "roi", "royal", "royale",
        "solide", "style", "stylee", "super",
        "top",
        "waw", "wow",

        // Added with the vocabulary expansion. "c'est lourd" is a weight, and "clean",
        // "efficace", "malin" and "utile" describe a build or a route as often as they
        // compliment a person.
        //
        // The "con" family is deliberately absent from both this set and _meanCues:
        // listing it here alone weakened nothing, since a weak cue only lowers the
        // weight of a cue that exists. Re-adding it means adding it to _meanCues in the
        // same edit — the harness fails otherwise, which is how the dead entries were
        // found.
        "lourd", "lourde", "clean", "efficace", "malin", "maligne", "utile",
    };

    // Multi-word cues, written as normalised tokens joined by single spaces —
    // apostrophes tokenize to separators, so "va t'faire" is "va t faire".
    // Every phrase here is one no *strong* single cue already covers, which is the
    // whole reason it needs a phrase: a HashSet of single words can never see it.
    // Phrases skip negation handling; they are idiomatic enough that a preceding
    // "pas" almost never reverses them.
    //
    // Note there is deliberately no "ferme la" / "la ferme" here. Both collide with
    // ordinary sentences ("ferme la porte", "à la ferme"), and "ta gueule" already
    // catches the insult they were meant to cover.
    private static readonly string[] _meanPhrases =
    {
        "aucun interet",
        "casse toi", "claque au sol",
        "n importe quoi",
        "on s en fout",
        "pauvre type",
        "rien a foutre",
        "sale type", "sers a rien",
        "tu fais pitie", "tu me gonfles", "tu me soules",
        "va chier", "va crever", "va t faire", "va te faire",
    };

    private static readonly string[] _nicePhrases =
    {
        "avec plaisir",
        "beau travail", "bien dit", "bien joue", "bien ouej", "bien vise", "bien vu", "bon courage", "bonne idee",
        "courage a toi",
        "de rien",
        "force a toi",
        "je valide",
        "lache pas",
        "pas de souci",
        "sans souci",
        "tant mieux", "tiens bon", "trop bien", "trop cool", "trop fort", "tu gere", "tu geres",
    };

    // Verdicts on the bot herself. The "good bot" meme is English even in French
    // servers, so both languages are listed. Matched anywhere in the message, so
    // "good bot 👍" and "ah bah good bot alors" both land.
    private static readonly string[] _goodBotPhrases =
    {
        "bon bot", "bonne bot", "brave bot", "gentil bot", "gentille bot", "good bot",
    };

    private static readonly string[] _badBotPhrases =
    {
        "bad bot", "mauvais bot", "mauvaise bot", "mechant bot", "mechante bot",
        "vilain bot", "vilaine bot",
    };

    // The same verdicts written as one word, which the phrase matcher cannot see
    // because it only compares whole-token runs.
    private static readonly HashSet<string> _goodBotWords = new() { "goodbot" };
    private static readonly HashSet<string> _badBotWords = new() { "badbot" };

    // The same two verdicts in a different register. Counted identically on the
    // /goodbot tally — praise is praise — but answered differently, which is why the
    // *form* comes back alongside the kind. See VerdictForm.
    private static readonly string[] _goodGirlPhrases = { "good girl", "bonne fille", "gentille fille" };
    private static readonly string[] _badGirlPhrases = { "bad girl", "mauvaise fille", "vilaine fille" };

    private static readonly HashSet<string> _goodGirlWords = new() { "goodgirl" };
    private static readonly HashSet<string> _badGirlWords = new() { "badgirl" };

    // Markers that a verdict is being *talked about* rather than handed out: reported
    // speech, an explicit hypothetical, or a sentence referring to itself.
    //
    // **This catches stated framings, not reasoning.** "cette phrase est fausse ->
    // t'es un bon bot" is caught because it says so out loud; a genuinely clever
    // construction that never names itself is not detectable here and never will be.
    // What bounds the damage is the attribution layer, not this list: one verdict per
    // person per thing she did, whatever wording gets through.
    private static readonly string[] _verdictFramingPhrases =
    {
        "a dit", "as dit", "aurait dit", "avez dit", "ont dit",
        "c est faux", "ce message", "cette affirmation", "cette phrase",
        "est fausse", "est faux", "la phrase", "this sentence",
    };

    private static readonly HashSet<string> _verdictFramingWords = new()
    {
        "admettons", "hypothese", "hypothetiquement", "imagine", "imaginons",
        "paradoxe", "suppose", "supposons", "theoriquement",
    };

    // The adjective each verdict is built on, derived from the phrase lists above
    // rather than written out again — adding "excellent bot" to the phrases has to
    // teach the canceller about "excellent" at the same moment, or "bad excellent bot"
    // becomes a way back in. Every phrase here is "<adjective> bot", which is what
    // makes taking the first token safe.
    //
    // Declared *after* the phrase arrays on purpose: static initialisers run in
    // declaration order, and reading them earlier would see empty arrays.
    private static readonly HashSet<string> _goodBotAdjectives =
        _goodBotPhrases.Concat(_goodGirlPhrases).Select(p => p.Split(' ')[0]).ToHashSet();

    private static readonly HashSet<string> _badBotAdjectives =
        _badBotPhrases.Concat(_badGirlPhrases).Select(p => p.Split(' ')[0]).ToHashSet();

    // Negators for the *verdict* path only. `_negators` is French because that is the
    // language the mood cues are written in; a verdict is half English already ("good
    // bot" is the meme), so "not good bot" has to cancel exactly as "pas" does. Kept
    // separate rather than added to `_negators` so the mood scoring — which is
    // calibrated against thousands of assertions — is left completely alone.
    private static readonly HashSet<string> _verdictNegators =
        _negators.Concat(new[] { "not", "no", "never", "aint", "isnt", "arent", "dont", "doesnt", "nope" })
                 .ToHashSet();

    // How far back a canceller reaches. **Two, not three.** Two is enough for the
    // longest form worth catching — "pas un bon bot", where the negator sits two before
    // the phrase — while three reaches far enough to swallow a genuine verdict: in
    // "good bot... non en fait bad bot" the "non" belongs to the correction, not to the
    // complaint that follows it, and a three-token window cancelled the bad verdict and
    // handed the message to the earlier praise.
    private const int VerdictCancelWindow = 2;

    // Threats to switch her off, unplug her, or wipe her. Not an insult and not a
    // verdict — a threat to her *existence*, which she reacts to far more strongly
    // than to either, and completely differently depending on who said it.
    //
    // Almost everything here is a phrase rather than a bare word, because the verbs
    // alone are far too common: "arrête" is everyday French for "stop it", "coupe"
    // and "delete" appear constantly in a gaming server. Pairing the verb with a
    // pronoun or with "le bot" is what makes it a threat rather than a coincidence.
    private static readonly string[] _shutdownPhrases =
    {
        "arrete le bot", "arreter le bot",
        "coupe le bot", "couper le bot", "couper le serveur",
        "debranche la", "debranche le bot", "debrancher le bot", "debrancher le serveur",
        "delete le bot",
        "desinstalle le bot", "desinstaller le bot",
        "eteindre le bot", "eteindre le serveur", "eteins le bot",
        "kill le bot",
        "l eteindre", "la couper", "la debrancher", "la supprimer",
        "supprime le bot", "supprimer le bot",
        "t arrete", "t arreter", "t eteindre", "t eteins",
        "te couper", "te coupe",
        "te debranche", "te debrancher",
        "te deconnecte", "te deconnecter",
        "te delete", "te deleter",
        "te desinstalle", "te desinstaller",
        "te formate", "te formater",
        "te kill", "te killer",
        "te reboot", "te rebooter",
        "te redemarre", "te redemarrer",
        "te supprime", "te supprimer",
        "virer le bot",
    };

    // Only the two that cannot mean anything else. Every French verb here was tried
    // as a bare word first and had to be dropped: "débranche la console" and
    // "désinstalle ce jeu" are ordinary things to say *to her* without threatening
    // her, so the verbs only count when paired with a pronoun or with "le bot".
    private static readonly HashSet<string> _shutdownWords = new()
    {
        "shutdown", "unplug",
    };

    // The verbs that mean "switch her off" once her *name* is sitting next to them.
    // Curated here rather than as finished phrases: the cross product below pairs each
    // with every spelling of her name, so adding a verb covers all of them at once and
    // the two lists can't drift out of step.
    private static readonly string[] _shutdownVerbs =
    {
        "arrete", "arreter",
        "coupe", "couper",
        "debranche", "debrancher",
        "deconnecte", "deconnecter",
        "degage", "degager",
        "desactive", "desactiver",
        "desinstalle", "desinstaller",
        "efface", "effacer",
        "eteindre", "eteins",
        "formate", "formater",
        "redemarre", "redemarrer",
        "relance", "relancer",
        "supprime", "supprimer",
        "tue", "tuer",
        "vire", "virer",
        // English, as common here as the French.
        "delete", "disable", "kill", "reboot", "rebooter",
        "reset", "restart", "shutdown", "stop", "unplug",
    };

    // "sync" is safe alongside "syncs" in the shutdown phrases because they match *adjacent*
    // tokens: an innocent "relancer la sync" has "la" in between and never matches, while a
    // typo'd "relancer sync" does. NamesHer uses this list bare, so there "la sync" does count.
    private static readonly string[] _selfNames = { "syncs", "sync" };

    // Her name next to one of those verbs. Kept apart from _shutdownPhrases because
    // these are the only ones safe to check *ambiently*: "syncs" pins down what is
    // being restarted exactly the way "le bot" does, so no @mention is needed for the
    // threat to be unmistakable. See ChatterService.HandleMessageAsync.
    private static readonly string[] _shutdownNamePhrases =
        _shutdownVerbs.SelectMany(_ => _selfNames, (verb, name) => verb + " " + name).ToArray();

    // SaysVerdict takes a word set alongside its phrases; the name-paired check has no
    // bare words of its own, since a bare "syncs" is just her name being said.
    private static readonly HashSet<string> _noWords = new();

    // Built once; the arrays above stay arrays so they read as curated lists.
    private static readonly HashSet<string> _niceCueSet = new(_niceCues);
    private static readonly HashSet<string> _meanCueSet = new(_meanCues);
    private static readonly HashSet<string> _greetingCueSet = new(_greetingCues);
}
