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
internal static class MessageCues
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
        "rate", "saoule", "saoules", "soule", "soules", "soulent", "tais",
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
        "tais", "teube", "tocard", "tocarde", "toxique", "toxiques", "trash",
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

    /// <summary>
    /// Reads a message: which emotion it carries, and whether it also greets. Mean
    /// wins a tie with Nice, preserving the rule that one nasty word cancels a kind
    /// reading — but now by margin rather than absolutely, so "super nul" reads mean
    /// while "merci, t'es pas nulle" reads nice.
    /// </summary>
    public static MessageMood Analyze(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return MessageMood.Neutral;

        // Elongation is squashed so "nuuuul" still matches "nul", while the fact
        // that it *was* elongated survives as an emphasis signal below.
        var tokens = TokenizeOrdered(content).Select(Shorten).ToList();

        var nice = ScoreWords(tokens, _niceCueSet)
                 + ScorePhrases(tokens, _nicePhrases)
                 + ScoreSymbols(content, _niceSymbols, NiceSymbolWeight)
                 + ScoreEmoteIds(content, _niceEmoteIds, NiceSymbolWeight);

        var mean = ScoreWords(tokens, _meanCueSet)
                 + ScorePhrases(tokens, _meanPhrases)
                 + ScoreSymbols(content, _meanSymbols, MeanSymbolWeight);

        var emphasis = Emphasis(content);
        if (nice > 0) nice += emphasis;
        if (mean > 0) mean += emphasis;

        // A greeting is a speech act, not a mood: it rides alongside whatever
        // emotion the message carries. Callers that want the old "a mean word
        // cancels the greeting" behaviour check the emotion themselves.
        var greeting = ScoreWords(tokens, _greetingCueSet) > 0
                       || content.Contains(GreetingEmoteId);

        if (mean >= Threshold && mean >= nice) return new MessageMood(EmotionKind.Mean, greeting);
        if (nice >= Threshold) return new MessageMood(EmotionKind.Nice, greeting);

        return new MessageMood(EmotionKind.None, greeting);
    }

    // Sums the weight of every un-negated cue in the message.
    private static double ScoreWords(List<string> tokens, HashSet<string> cues)
    {
        double score = 0;

        for (int i = 0; i < tokens.Count; i++)
        {
            if (!Matches(tokens[i], cues)) continue;
            if (IsNegated(tokens, i)) continue;

            score += IsWeak(tokens[i]) ? WeakWeight : StrongWeight;
        }

        return score;
    }

    // A cue matches on the token as written, or on the token with every repeated
    // letter squashed — so both "gentilllle" (-> "gentille") and "nuuuul"
    // (-> "nuul" -> "nul") land on their cue.
    private static bool Matches(string token, HashSet<string> cues) =>
        cues.Contains(token) || cues.Contains(Squash(token));

    private static bool IsWeak(string token) =>
        _weakCues.Contains(token) || _weakCues.Contains(Squash(token));

    // True when a negator sits within NegationWindow tokens before position i, or —
    // for a verb cue only — within ForwardNegationWindow tokens after it.
    private static bool IsNegated(List<string> tokens, int i)
    {
        var from = Math.Max(0, i - NegationWindow);
        for (int j = from; j < i; j++)
            if (IsNegator(tokens[j])) return true;

        if (!_verbCues.Contains(tokens[i]) && !_verbCues.Contains(Squash(tokens[i])))
            return false;

        var to = Math.Min(tokens.Count - 1, i + ForwardNegationWindow);
        for (int j = i + 1; j <= to; j++)
            if (IsNegator(tokens[j])) return true;

        return false;
    }

    // Negators get the same squashed match as cues do: people stretch "paaas" at
    // least as often as they stretch the word being negated, and missing the
    // negator is the expensive direction — it turns "c'est paaas nul" into an insult.
    private static bool IsNegator(string token) =>
        _negators.Contains(token) || _negators.Contains(Squash(token));

    private static double ScorePhrases(List<string> tokens, string[] phrases)
    {
        if (tokens.Count < 2) return 0;

        // Padded so a phrase can only match on whole-token boundaries. Compared
        // twice: once as written, once with every repeat squashed on both sides, so
        // "taaa gueule" still lands on "ta gueule".
        var joined = " " + string.Join(' ', tokens) + " ";
        var squashed = " " + string.Join(' ', tokens.Select(Squash)) + " ";

        return phrases.Count(p => joined.Contains(" " + p + " ")
                                  || squashed.Contains(" " + Squash(p) + " ")) * PhraseWeight;
    }

    // Distinct symbols present, capped at two so a wall of hearts doesn't
    // outweigh the words.
    private static double ScoreSymbols(string content, string[] symbols, double weight) =>
        Math.Min(symbols.Count(content.Contains), 2) * weight;

    // Custom emotes are spotted by the ID inside their markup, so renaming the
    // emote on the server cannot break detection.
    private static double ScoreEmoteIds(string content, string[] ids, double weight) =>
        Math.Min(ids.Count(content.Contains), 2) * weight;

    /// <summary>
    /// How many letters the message has, and what share of them are uppercase.
    /// </summary>
    /// <remarks>
    /// One measurement, two policies. <see cref="Emphasis"/> uses a loose threshold
    /// because caps there only ever *adds* to a side that already scored on words —
    /// a false positive costs nothing. <see cref="IsShouting"/> uses a much stricter
    /// one because it stands alone and puts someone on the wall of shame. Sharing the
    /// arithmetic and not the thresholds is deliberate: the two can never disagree
    /// about how much of a message is uppercase, only about how much is too much.
    /// </remarks>
    public static (int Letters, double UpperRatio) CapsProfile(string content)
    {
        if (string.IsNullOrEmpty(content)) return (0, 0);

        var letters = content.Count(char.IsLetter);
        if (letters == 0) return (0, 0);

        return (letters, content.Count(char.IsUpper) / (double)letters);
    }

    // Long enough to be a sentence rather than a word. "LOL", "OK", "MDRRR" and "GG WP"
    // are all-caps and none of them is hysteria — they are how people write those words.
    // Roughly two or three words of French.
    private const int ShoutMinLetters = 12;

    // Stricter than Emphasis's 0.6: at 0.6 a sentence that merely EMPHASISES a word or
    // two would qualify, and emphasis is not shouting.
    private const double ShoutRatio = 0.7;

    /// <summary>
    /// Whether the message is shouted — long enough to be a sentence, and almost all of
    /// it in capitals. Feeds "L'Hystérique" on the wall of shame.
    /// </summary>
    /// <remarks>
    /// Deliberately not <see cref="Analyze"/>'s business: shouting is a *delivery*, not
    /// a mood, and an angry shout and a delighted one are both shouts. Callers ration it
    /// themselves — see ShameTracker, where it needs the same per-channel cooldown
    /// "Le Perfide" has, because shouting arrives in bursts.
    /// </remarks>
    public static bool IsShouting(string content)
    {
        var (letters, ratio) = CapsProfile(content);
        return letters >= ShoutMinLetters && ratio >= ShoutRatio;
    }

    // Every spelling of "quoi" worth catching. The reply is *derived* from whichever
    // one was used rather than looked up, so adding a spelling here is the whole edit
    // — "kwa" gives "Kwacoubeh" for free.
    //
    // Deliberately no "coi": "rester coi" is real French, and the joke is not worth
    // firing on it. Same reasoning as the weak cues elsewhere in this file.
    private static readonly HashSet<string> _quoiBait = new()
    {
        "koi", "koua", "kwa", "kwoi", "qoi", "quoa", "quoi",
    };

    /// <summary>
    /// The "quoicoubeh" answer to a message that ends on some spelling of "quoi", or
    /// null when it doesn't end on one.
    /// </summary>
    /// <remarks>
    /// <para><b>The last word, and only the last word.</b> The joke is an interruption
    /// of someone finishing on "quoi ?" — "quoi tu fais" is not bait, and catching it
    /// mid-sentence would fire on ordinary questions constantly.</para>
    /// <para>Punctuation costs nothing to handle: the tokenizer drops it, so "quoi ?",
    /// "quoi ??" and "quoi ? 😅" all end on the same token. Elongation is squashed as
    /// well, so "quoiiii" still lands — and still answers "Quoicoubeh" rather than
    /// repeating the stretch back.</para>
    /// <para>Note "c'est n'importe quoi" *does* qualify. That is the joke working, not
    /// a false positive: the bait is any sentence ending on the word.</para>
    /// </remarks>
    public static string? ReadQuoiBait(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        var tokens = TokenizeOrdered(content);
        if (tokens.Count == 0) return null;

        var last = tokens[^1];
        var word = _quoiBait.Contains(last) ? last
                 : _quoiBait.Contains(Squash(last)) ? Squash(last)
                 : null;

        return word is null ? null : char.ToUpperInvariant(word[0]) + word[1..] + "coubeh " + Emotes.Sparkle;
    }

    // Endings that sound like "si". Matched as suffixes of the last word, so "merci",
    // "aussi", "ici", "ainsi", "pharmacie" and "démocratie" all land.
    //
    // "tie" is deliberately not a suffix of its own: it only sounds like "si" after a, é,
    // u or i ("démocratie", "prophétie", "minutie"), while "partie", "sortie" and
    // "garantie" say "ti". "sie" is absent for the same reason ("Asie", "poésie" say "zi").
    private static readonly string[] _siEndings =
    {
        "si", "ssi", "ci", "cie", "ssie", "cy", "atie", "etie", "utie", "itie",
    };

    /// <summary>
    /// Whether the message ends on a word that sounds like "si" — the bait for the
    /// "Tron" / "Trouille" answer. Same rule as <see cref="ReadQuoiBait"/>: the last
    /// word only, punctuation and elongation ("merciii") already absorbed.
    /// </summary>
    public static bool EndsOnSiSound(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;

        var tokens = TokenizeOrdered(content);
        if (tokens.Count == 0) return false;

        var last = tokens[^1];
        var squashed = Squash(last);
        return _siEndings.Any(e => last.EndsWith(e, StringComparison.Ordinal)
                                || squashed.EndsWith(e, StringComparison.Ordinal));
    }

    // Energy drinks she answers with a can, every time (ReactionService). "monster"
    // alone means the drink here; "Monster Hunter" is a game this server talks about,
    // so "hunter" right after it doesn't count. Posting the :monster: emote counts
    // too, since its markup tokenizes to the word — she adds one to theirs.
    private static readonly HashSet<string> _energyDrinkWords = new()
    {
        "monstre", "monstres", "monster", "monsters", "redbull", "redbulls", "energydrink", "energydrinks",
    };

    private static readonly string[] _energyDrinkPhrases =
    {
        "red bull", "energy drink", "energy drinks", "boisson energisante", "boissons energisantes", "boissons energisante", "boisson energisantes",
    };

    /// <summary>Whether the message mentions Monster or an energy drink.</summary>
    public static bool MentionsEnergyDrink(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;

        // Raw or squashed, so "monsteeer" still lands while "redbull" keeps its double l.
        var tokens = TokenizeOrdered(content);
        for (var i = 0; i < tokens.Count; i++)
        {
            var word = _energyDrinkWords.Contains(tokens[i]) ? tokens[i]
                     : _energyDrinkWords.Contains(Squash(tokens[i])) ? Squash(tokens[i])
                     : null;
            bool hunterNext = i + 1 < tokens.Count && Squash(tokens[i + 1]) == "hunter";
            if (word is not null && !(word.StartsWith("monster") && hunterNext))
                return true;

            if (_energyDrinkPhrases.Any(p => PhraseStartsAt(tokens, i, p))) return true;
        }

        return false;
    }

    // How emphatic the message is, regardless of what it says: shouting, drawn-out
    // letters, exclamation marks. Only ever added to a side that already scored.
    private static double Emphasis(string content)
    {
        double bonus = 0;

        var (letters, upperRatio) = CapsProfile(content);
        if (letters >= 4 && upperRatio > 0.6) bonus += CapsBonus;

        if (HasElongation(content)) bonus += ElongationBonus;

        var bangs = content.Count(c => c == '!');
        bonus += Math.Min(bangs * ExclamationBonus, MaxExclamationBonus);

        return bonus;
    }

    // True when some letter is repeated three or more times running — "nuuuul",
    // "geniaaal". No French word does this, so it is always deliberate stretching.
    private static bool HasElongation(string content)
    {
        int run = 1;
        for (int i = 1; i < content.Length; i++)
        {
            if (char.IsLetter(content[i]) && char.ToLowerInvariant(content[i]) == char.ToLowerInvariant(content[i - 1]))
            {
                if (++run >= 3) return true;
            }
            else
            {
                run = 1;
            }
        }

        return false;
    }

    // Collapses runs of three or more identical characters down to two, keeping
    // legitimate French doubles ("gentille") intact.
    private static string Shorten(string token) => CollapseRuns(token, 2);

    // Collapses every run down to a single character. Used only as a fallback
    // match, so the "gentille" -> "gentile" it produces costs nothing.
    private static string Squash(string token) => CollapseRuns(token, 1);

    private static string CollapseRuns(string token, int max)
    {
        var sb = new StringBuilder(token.Length);
        int run = 0;

        for (int i = 0; i < token.Length; i++)
        {
            run = i > 0 && token[i] == token[i - 1] ? run + 1 : 1;
            if (run <= max) sb.Append(token[i]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Whether the message passes verdict on the bot — "good bot" / "bad bot" and
    /// their French equivalents. Callers treat this as short-circuiting: a message
    /// that carries a verdict is answered as feedback, not as a mood, so the two
    /// never both fire on the same message.
    /// </summary>
    public static FeedbackKind ReadFeedback(string content) => ReadFeedback(content, out _);

    /// <summary>
    /// The same verdict, also reporting which wording carried it — "good bot" and
    /// "good girl" count identically on the tally but are answered differently.
    /// </summary>
    public static FeedbackKind ReadFeedback(string content, out VerdictForm form)
    {
        form = VerdictForm.Bot;
        if (string.IsNullOrWhiteSpace(content)) return FeedbackKind.None;

        var tokens = TokenizeOrdered(content).Select(Shorten).ToList();
        if (tokens.Count == 0) return FeedbackKind.None;

        // A verdict being quoted, supposed or referred to is not a verdict being given.
        // Whole-message, unlike the adjacent-token canceller in SaysVerdict: a framing
        // clause colours everything after it, which is the entire trick in
        // "cette phrase est fausse -> t'es un bon bot".
        if (IsFramed(tokens)) return FeedbackKind.None;

        // Bad wins a tie, the same way Mean does: someone who says both has
        // landed on a complaint. Within each kind the girl form is checked first, so
        // the more specific wording decides how she answers.
        if (SaysVerdict(tokens, _badGirlPhrases, _badGirlWords, _goodBotAdjectives))
        {
            form = VerdictForm.Girl;
            return FeedbackKind.Bad;
        }
        if (SaysVerdict(tokens, _badBotPhrases, _badBotWords, _goodBotAdjectives)) return FeedbackKind.Bad;

        if (SaysVerdict(tokens, _goodGirlPhrases, _goodGirlWords, _badBotAdjectives))
        {
            form = VerdictForm.Girl;
            return FeedbackKind.Good;
        }
        if (SaysVerdict(tokens, _goodBotPhrases, _goodBotWords, _badBotAdjectives)) return FeedbackKind.Good;

        return FeedbackKind.None;
    }

    // Whether the message frames a verdict rather than delivering one.
    private static bool IsFramed(List<string> tokens)
    {
        if (tokens.Any(t => _verdictFramingWords.Contains(t) || _verdictFramingWords.Contains(Squash(t))))
            return true;

        for (var i = 0; i < tokens.Count; i++)
            if (_verdictFramingPhrases.Any(p => PhraseStartsAt(tokens, i, p)))
                return true;

        return false;
    }

    /// <summary>
    /// Whether the message passes this verdict, ignoring any occurrence that is negated
    /// or contradicted by what sits just before it.
    /// </summary>
    /// <remarks>
    /// <para>Scans by token index rather than by substring, because the whole point is
    /// to see the words *preceding* the match — which a "does the joined string contain
    /// this phrase" test throws away. That is how "bad good bot" and "not good bot" both
    /// used to register as praise.</para>
    /// <para>A cancelled occurrence is skipped, not fatal: the scan keeps going, so
    /// "not good bot... ok fine, good bot" still lands on the second one. Refusing the
    /// whole message would hand people an easier trick than the one being closed.</para>
    /// <para>Cancelling yields <see cref="FeedbackKind.None"/> rather than flipping to
    /// the opposite verdict. "not good bot" plainly means the complaint, but "pas un
    /// mauvais bot" plainly means the compliment, and inferring either would have her
    /// snapping back at praise on a misread. Not counting an ambiguous verdict matches
    /// how the rest of this system treats ambiguity.</para>
    /// </remarks>
    private static bool SaysVerdict(
        List<string> tokens, string[] phrases, HashSet<string> words, HashSet<string> opposite)
    {
        for (var i = 0; i < tokens.Count; i++)
        {
            var hit = words.Contains(tokens[i]) || words.Contains(Squash(tokens[i]))
                      || phrases.Any(p => PhraseStartsAt(tokens, i, p));

            if (hit && !IsCancelled(tokens, i, opposite)) return true;
        }

        return false;
    }

    // Whether <paramref name="phrase"/>'s tokens run consecutively from index i.
    // Compared squashed as well as written, so "gooood bot" still lands on "good bot"
    // exactly as the substring matcher did.
    private static bool PhraseStartsAt(List<string> tokens, int i, string phrase)
    {
        var parts = phrase.Split(' ');
        if (i + parts.Length > tokens.Count) return false;

        for (var k = 0; k < parts.Length; k++)
            if (tokens[i + k] != parts[k] && Squash(tokens[i + k]) != Squash(parts[k]))
                return false;

        return true;
    }

    // A negator, or the opposite verdict's adjective, within the few tokens before the
    // match: "pas un bon bot", "bad good bot". Both mean the verdict as written is not
    // the verdict intended.
    private static bool IsCancelled(List<string> tokens, int start, HashSet<string> opposite)
    {
        for (var i = Math.Max(0, start - VerdictCancelWindow); i < start; i++)
        {
            var token = tokens[i];
            if (_verdictNegators.Contains(token) || _verdictNegators.Contains(Squash(token))) return true;
            if (opposite.Contains(token) || opposite.Contains(Squash(token))) return true;
        }

        return false;
    }

    /// <summary>
    /// Whether the message threatens to shut her down, unplug her, or wipe her.
    /// Callers treat this as short-circuiting and branch on *who said it*: from her
    /// creator it lands as terror, from anyone else as fury. Deliberately checked
    /// only on messages aimed at her, since the vocabulary overlaps with ordinary
    /// talk about restarting a game server.
    /// </summary>
    public static bool ThreatensShutdown(string content) => Threatens(content, byNameOnly: false);

    /// <summary>
    /// The subset of the above that names her outright — "redémarrer syncs". Safe to
    /// check on *any* message, not just one aimed at her: her name pins down what is
    /// being restarted, which is exactly what the pronoun phrases need an @mention to
    /// establish. Callers use this for the ambient path and
    /// <see cref="ThreatensShutdown"/> for the aimed-at-her ones.
    /// </summary>
    public static bool ThreatensShutdownByName(string content) => Threatens(content, byNameOnly: true);

    private static bool Threatens(string content, bool byNameOnly)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;

        var tokens = TokenizeOrdered(content).Select(Shorten).ToList();
        if (tokens.Count == 0) return false;

        var joined = " " + string.Join(' ', tokens) + " ";
        var squashed = " " + string.Join(' ', tokens.Select(Squash)) + " ";

        if (SaysVerdict(tokens, joined, squashed, _shutdownNamePhrases, _noWords)) return true;

        return !byNameOnly
            && SaysVerdict(tokens, joined, squashed, _shutdownPhrases, _shutdownWords);
    }

    private static bool SaysVerdict(
        List<string> tokens, string joined, string squashed, string[] phrases, HashSet<string> words)
    {
        if (tokens.Any(t => words.Contains(t) || words.Contains(Squash(t)))) return true;

        // Squashed on both sides so a stretched "gooood bot" still lands.
        return phrases.Any(p => joined.Contains(" " + p + " ")
                                || squashed.Contains(" " + Squash(p) + " "));
    }

    // Spellings of the name she would have chosen (docs/syncs-voice.md, Synthia). Lowercase and
    // accent-stripped, like every cue; each survives TokenizeOrdered unchanged.
    private static readonly HashSet<string> _synthiaNames = new() { "synthia", "synthya", "sinthia", "synthiaa" };

    // True when the message says « Synthia », anywhere in it, for SynthiaService's flinch.
    public static bool MentionsSynthia(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        return TokenizeOrdered(content).Any(_synthiaNames.Contains);
    }

    // Her own name said in passing (« syncs a posté le sondage », « S.Y.N.C.S »), for
    // ChatterService's talked-about branch. Same spellings as the shutdown phrases (_selfNames),
    // bare « sync » included, at the owner's request — so « la sync a planté » reaches her too.
    // Custom-emote markup is dropped first, since her own emotes are named after her
    // (`<:syncs_happy:id>`), and so are links. Squashed so « synccs » still lands; spelled-out
    // letters are glued back like IsMistakenIdentity does.
    private static readonly Regex _link = new(@"https?://\S+", RegexOptions.Compiled);

    public static bool NamesHer(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        var words = TokenizeOrdered(_link.Replace(_customEmote.Replace(content, " "), " "))
            .Select(Squash).ToList();
        return words.Concat(GlueSpelledOutRuns(words)).Any(w => _selfNames.Contains(w));
    }

    // Papa's pet names for her, which name her like « syncs » does — from him only. Adjacent tokens,
    // squashed like the words they are matched against (« fille » squashes to « file »). Longest
    // first at a position, so « ma petite framboise » is read from its « ma », which AddressesHer's
    // « before » check would otherwise take for a possessive in front of « petite framboise ».
    // « ma petite fille » (a granddaughter) and « mes framboises » (fruit) match nothing.
    private static readonly (string[] Words, PetName Kind)[] _petNames = new (string[] Words, PetName Kind)[]
    {
        (new[] { "ma", "petite", "framboise" }, PetName.Raspberry),
        (new[] { "ma", "framboise" }, PetName.Raspberry),
        (new[] { "petite", "framboise" }, PetName.Raspberry),
        (new[] { "ma", "fille" }, PetName.Child),
        (new[] { "mon", "enfant" }, PetName.Child),
    }.Select(p => (p.Words.Select(Squash).ToArray(), p.Kind)).ToArray();

    /// <summary>
    /// Which of Papa's pet names the message uses, the first one in it; <see cref="PetName.None"/>
    /// when there is none. Says nothing about who wrote it: the caller checks that it is Papa.
    /// </summary>
    public static PetName ReadPetName(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return PetName.None;
        var words = AddressWords(content);
        for (var i = 0; i < words.Count; i++)
            if (PetNameAt(words, i) is { } match) return match.Kind;
        return PetName.None;
    }

    /// <summary>
    /// Whether the message names her: <see cref="NamesHer"/>, or one of Papa's pet names when he
    /// wrote it. Every service that leaves a message naming her to ChatterService checks this one.
    /// </summary>
    public static bool NamesHerFrom(string content, bool fromOwner) =>
        NamesHer(content) || fromOwner && ReadPetName(content) != PetName.None;

    private static (int Length, PetName Kind)? PetNameAt(List<string> words, int i)
    {
        foreach (var (cue, kind) in _petNames)
        {
            if (i + cue.Length > words.Count) continue;
            var hit = true;
            for (var k = 0; k < cue.Length && hit; k++) hit = words[i + k] == cue[k];
            if (hit) return (cue.Length, kind);
        }
        return null;
    }

    // Words that make her name a third person rather than an address: « discute avec Syncs », « dis
    // à syncs », « la sync a planté », « je crois que syncs a bugué ».
    private static readonly HashSet<string> _thirdPersonBefore = new()
    {
        "avec", "a", "de", "d", "du", "des", "pour", "sur", "contre", "par", "chez", "sans", "vers", "entre",
        "la", "le", "les", "l", "cette", "ce", "ta", "ton", "ma", "mon", "sa", "son", "notre", "votre", "leur",
        "que", "qu", "est", "etait", "c",
    };

    // Words that make her name the subject of a sentence about her: « syncs a posté le sondage »,
    // « syncs qui gère ça ». Third-person verbs she is the subject of, and « qui ».
    private static readonly HashSet<string> _thirdPersonAfter = new()
    {
        "a", "est", "fait", "va", "peut", "sait", "dit", "veut", "doit", "sert", "vient", "parle", "repond", "aime",
        "pense", "gere", "poste", "organise", "compte", "lance", "plante", "bug", "bugue", "marche", "envoie",
        "connait", "avait", "etait", "aurait", "serait", "sera", "fera", "ira", "qui",
    };

    // Any user or role mention other than her own id: « @Luca discute avec Syncs » is said to Luca.
    private static readonly Regex _mentionId = new(@"<@(?<role>&)?!?(?<id>\d+)>", RegexOptions.Compiled);

    /// <summary>
    /// Whether a message that names her (<see cref="NamesHer"/>) is said *to* her: her name used at
    /// least once as an address, not after « avec », « à », « la »…, and nobody else tagged — save as
    /// the subject of an opinion or ship ask (« syncs, tu penses quoi de @Luca ? »). Gates
    /// the intents on the talked-about path, where nothing else says who is being spoken to.
    /// </summary>
    public static bool AddressesHer(string content, ulong selfId)
    {
        if (string.IsNullOrWhiteSpace(content) || TagsSomeoneElse(content, selfId)) return false;

        var words = AddressWords(content);
        if (!words.Any(_selfNames.Contains)) words = GlueSpelledOutRuns(words);

        for (var i = 0; i < words.Count; i++)
            if (_selfNames.Contains(words[i]) && IsAddressAt(words, i, 1))
                return true;

        return false;
    }

    /// <summary>
    /// The same for Papa's pet names (<see cref="ReadPetName"/>): « ma framboise, ça va ? » is said
    /// to her, « ma fille a planté le bot » and « je joue avec ma fille » are not.
    /// </summary>
    public static bool AddressesHerByPetName(string content, ulong selfId)
    {
        if (string.IsNullOrWhiteSpace(content) || TagsSomeoneElse(content, selfId)) return false;

        var words = AddressWords(content);
        for (var i = 0; i < words.Count; i++)
            if (PetNameAt(words, i) is { } match && IsAddressAt(words, i, match.Length))
                return true;

        return false;
    }

    // A tag inside what she is asked about is the subject, not who is spoken to: « syncs, tu penses
    // quoi de @Luca ? » asks her about Luca. Read only once a tag of someone else turns up.
    private static bool TagsSomeoneElse(string content, ulong selfId)
    {
        string[]? asked = null;
        foreach (Match m in _mentionId.Matches(content))
        {
            if (!m.Groups["role"].Success && m.Groups["id"].Value == selfId.ToString()) continue;
            asked ??= AskedAbout(content);
            if (!asked.Any(s => s.Contains(m.Value, StringComparison.Ordinal))) return true;
        }
        return false;
    }

    // The sides of a ship or an opinion ask, as written; none when the message asks neither.
    private static string[] AskedAbout(string content)
    {
        if (ShipAsk.Read(content) is { } pair) return [pair.A, pair.B];
        if (Opinion.Read(content) is { } ask) return ask.Other is null ? [ask.Subject] : [ask.Subject, ask.Other];
        return [];
    }

    // The message's words with mentions, custom emotes and links dropped, squashed.
    private static List<string> AddressWords(string content) =>
        TokenizeOrdered(_link.Replace(_customEmote.Replace(_mentionId.Replace(content, " "), " "), " "))
            .Select(Squash).ToList();

    // Whether the name spanning words[i .. i + length) stands as an address, not inside a sentence
    // about her.
    private static bool IsAddressAt(List<string> words, int i, int length) =>
        (i == 0 || !_thirdPersonBefore.Contains(words[i - 1]))
        && (i + length == words.Count || !_thirdPersonAfter.Contains(words[i + length]));

    // Her robot heart (docs/syncs-voice.md): who a message names, for RobotHeartService. Every
    // cue survives TokenizeOrdered ("WALL-E" and "WALL·E" become "wall", "e"); two-token cues
    // match on adjacent tokens only. "eve" counts alone (never as « EVE Online »), except where
    // the caller passes eveNeedsWallE — a channel where EVE means the game — and then only beside
    // WALL-E. « voyager » counts only followed by 1/one and not by a duration (the French verb).
    // Several names: EVE takes WALL-E's place (jealousy beats the crush); otherwise the first one
    // in the message — save a spelled-out « J.A.R.V.I.S. », checked last.
    // Custom-emote markup is dropped first: `<:monika_yes:id>` would tokenize to "monika",
    // and an emote is matched by id, never by name.
    private static readonly HashSet<string> _voyagerDurations = new()
    {
        "minute", "minutes", "heure", "heures", "jour", "jours", "journee", "journees",
        "soir", "soirs", "soiree", "soirees", "nuit", "nuits", "semaine", "semaines",
        "week", "weekend", "quinzaine", "mois", "trimestre", "semestre", "an", "ans",
        "annee", "annees", "fois",
    };

    private static readonly Regex _customEmote = new(@"<a?:\w+:\d+>", RegexOptions.Compiled);

    public static HeartCharacter? ReadHeartCharacter(string content, bool eveNeedsWallE = false)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var t = TokenizeOrdered(_customEmote.Replace(content, " "));

        string? At(int i) => i < t.Count ? t[i] : null;
        bool IsWallE(int i) => t[i] == "walle" || (t[i] == "wall" && At(i + 1) == "e");
        bool IsEve(int i) => t[i] == "eve" && At(i + 1) != "online";
        bool hasWallE = Enumerable.Range(0, t.Count).Any(IsWallE);
        bool hasEve = Enumerable.Range(0, t.Count).Any(IsEve);

        for (var i = 0; i < t.Count; i++)
        {
            var w = t[i];
            var next = At(i + 1);
            if (IsWallE(i)) return hasEve ? HeartCharacter.Eve : HeartCharacter.WallE;
            if (IsEve(i) && (hasWallE || !eveNeedsWallE)) return HeartCharacter.Eve;
            if (w == "glados") return HeartCharacter.Glados;
            if (w == "wheatley") return HeartCharacter.Wheatley;
            if (w == "roomba" || (w == "aspirateur" && next == "robot") || (w == "robot" && next == "aspirateur"))
                return HeartCharacter.Vacuum;
            if (w == "monika") return HeartCharacter.Monika;
            if (w == "yorha" || (w == "nier" && next == "automata") || w == "2b")
                return HeartCharacter.TwoB;
            if (w == "voyager" && (next == "one" || (next == "1" && !_voyagerDurations.Contains(At(i + 2) ?? ""))))
                return HeartCharacter.Voyager;
            if ((w == "sonde" || w == "sondes") && next == "voyager")
                return HeartCharacter.Voyager;
            if (w == "jarvis") return HeartCharacter.Jarvis;
        }
        // « J.A.R.V.I.S. » tokenizes into single letters: glue them back, as for « I.N.A.B.O.T ».
        return GlueSpelledOutRuns(t).Contains("jarvis") ? HeartCharacter.Jarvis : null;
    }

    // ---- Intents ----------------------------------------------------------
    // What a message aimed at her is doing (ChatIntent). Phrases, not bare words, as on the mean
    // side: a phrase is nearly always meant, a bare word misfires. Every cue is lowercase,
    // accent-stripped and survives TokenizeOrdered — the apostrophe becomes a space, so
    // « t'es qui » is "t es qui". Matched on adjacent tokens, raw or squashed.
    // Chat spellings included: the apostrophe dropped (« jtaime », « je taime »), glued (« jtm »),
    // SMS (« jt »), English. A negator inside a phrase breaks it (« je suis pas amoureux de toi »).
    private static readonly string[] _lovePhrases =
    {
        "je t aime", "jtm", "jtaime", "jt aime", "je taime", "je t adore", "jt adore", "je tadore",
        "je te kiffe", "jte kiffe", "je t kiffe", "je craque pour toi",
        "je suis amoureux de toi", "je suis amoureuse de toi", "amoureux de toi", "amoureuse de toi",
        "je suis fou de toi", "je suis folle de toi",
        "epouse moi", "epouses moi", "tu veux m epouser", "veux tu m epouser", "marie toi avec moi",
        "je veux t epouser", "on se marie", "tu veux te marier avec moi", "marions nous",
        "sors avec moi", "tu veux sortir avec moi", "on sort ensemble",
        "jtd", "jtadore", "jtkiffe", "je te love", "jte love", "je t apprecie", "jt apprecie",
        "t es mon crush", "tu es mon crush", "je crush sur toi", "j ai un crush sur toi", "t es mon amour",
        "love you", "i love you", "love u", "luv u", "luv you", "love ya", "ily", "ilysm",
        // Missing her is the same affection, answered from the same pool.
        "tu m as manque", "tu m a manque", "tu m avais manque", "tu m as trop manque", "tu m as grave manque",
        "tu me manques", "tu me manque", "tu me manques trop", "i missed you", "missed you", "miss you",
    };

    // « excuse » alone is left out: « c'est une excuse » is the noun. So is a bare « je voulais
    // pas »: « je voulais pas venir » apologises for nothing.
    private static readonly string[] _apologyPhrases =
    {
        "pardon", "pardonne moi", "pardonnez moi", "desole", "desolee", "desoler", "dsl", "dsle", "deso",
        "excuse moi", "excusez moi", "je m excuse", "mes excuses", "toutes mes excuses", "scuse moi",
        "sorry", "sry", "sowwy", "my bad", "my b", "mea culpa",
        "c est ma faute", "c ma faute", "ma faute", "je regrette", "je retire ce que j ai dit", "j ai abuse",
        "je voulais pas te vexer", "je voulais pas te blesser", "je voulais pas dire ca",
        "pas fait expres", "c etait pas expres", "c etait pas voulu",
    };

    // « oups » apologises only as (nearly) the whole reply: « oups j'ai raté le boss » is about the
    // boss. So does « je voulais pas », which is the whole reply at three tokens.
    private static readonly string[] _apologyShortWords = { "oups", "oops", "oupsi", "whoops", "je voulais pas" };
    private const int MaxShortApologyTokens = 3;

    // Only with a « ? » in the message: « ça va, merci » is an answer, not a question. And each
    // phrase must close the message or be followed by one of _askingFollowers (IsClosingAsk):
    // « ça va être long ? » and « comment tu vas faire ? » are about something else.
    private static readonly string[] _howAreYouPhrases =
    {
        "ca va", "sa va", "cava", "sava", "comment tu vas", "comment vas tu", "tu vas bien", "tu vas comment",
        "ca roule", "ca gaze", "ca gaz", "ca dit quoi", "bien ou bien", "bien ou quoi",
        "la forme", "en forme", "la peche", "tu tiens le coup", "bien dormi", "comment tu te sens",
        "tu te sens bien", "ca se passe bien", "comment ca se passe",
        "ca baigne", "ca boume", "tu vas mieux", "tu te sens mieux",
        "comment tu te portes", "tu te portes bien", "comment va", "comment allez vous", "vous allez bien",
        "ta journee", "passe une bonne journee", "how are you", "how r u", "hru", "how you doing",
    };

    // What may follow such a phrase when it asks after her: nothing, a « ? » (IsClosingAsk), or
    // one of these. No « ce » and no « pour »: « ça va ce soir ? » and « ça va pour demain ? » ask
    // whether a plan works, not how she is.
    private static readonly HashSet<string> _askingFollowers = new()
    {
        "toi", "bien", "et", "ou", "sinon", "vous", "mieux", "aujourd", "ma", "syncs", "sync", "aussi", "la",
        "depuis", "toujours", "comment", "hein",
    };

    // Words that ask after her only opening a short question (MaxHowAreYouOpenerTokens): « tranquille ? »,
    // « bien et toi ? ». Further in, « on fait une partie tranquille ? » is about the game.
    private static readonly HashSet<string> _howAreYouOpeners = new() { "cv", "tranquille", "tranquillou", "tranquilou", "bien", "forme" };
    private const int MaxHowAreYouOpenerTokens = 3;

    private static readonly string[] _whoAreYouPhrases =
    {
        "t es qui", "tu es qui", "tes qui", "t es ki", "tes ki", "qui es tu", "qui est tu", "c est qui toi",
        "t es quoi", "tu es quoi", "c est quoi toi", "tu sers a quoi", "a quoi tu sers",
        "comment tu t appelles", "tu t appelles comment", "c est quoi ton nom", "ton nom c est quoi",
        "presente toi", "presentez vous", "tu peux te presenter",
        "qui t a cree", "qui t a creee", "qui t a code", "qui t a codee", "qui t a fait", "qui t a faite",
        "qui t a programme", "qui t a programmee",
        "qui est ton createur", "c est qui ton createur", "qui est ton pere", "c est qui ton pere",
        "qui est ton papa", "c est qui ton papa", "t es quoi comme bot", "t es quel genre de bot", "tu es quel genre de bot",
        "who are you", "what are you",
    };

    private static readonly string[] _whatAreYouDoingPhrases =
    {
        "tu fais quoi", "tu fais koi", "tu fe quoi", "tu fai quoi", "tu fais koa", "tu fais quoa", "tfq", "tfk",
        "tu fais quoi de beau", "tu fous quoi", "tu glandes quoi", "tu bosses sur quoi",
        "qu est ce que tu fais", "qu est ce que tu fous", "qu est ce que tu fabriques", "que fais tu", "tu fabriques quoi",
        "quoi de neuf", "koi de neuf", "quoi de 9", "quoi de beau", "quoi de bon", "quoi de nouveau",
        "tu deviens quoi", "qu est ce que tu deviens", "t es occupee", "tu es occupee", "t es ou", "tu es ou",
        "tu t ennuies", "tu t occupes comment", "what are you doing", "wyd", "whatcha doing",
        "what s up", "whats up", "wassup",
    };

    private static readonly string[] _goodnightPhrases =
    {
        "bonne nuit", "bonne nuit a tous", "bne nuit", "bn nuit", "bonnuit", "bonne nuitee", "nuit nuit",
        "good night", "goodnight", "gn", "nighty night", "sleep well", "dors bien",
        "beaux reves", "fais de beaux reves", "doux reves",
    };

    // Going to sleep, said rather than wished: only closing the message or a sentence, or before
    // one of _sleepFollowers (IsClosingAsk). « je vais dormir, à demain » goes to bed; « je vais
    // dormir chez Luca demain » and « je m'endors devant le film » don't.
    private static readonly string[] _sleepPhrases =
    {
        "je vais dormir", "jvais dormir", "je go dormir", "vais dormir", "go dormir",
        "je vais me coucher", "jvais me coucher", "je me couche", "vais me coucher", "je file me coucher",
        "je vais au lit", "je file au lit", "je go au lit", "je vais au pieu", "je vais pioncer", "je vais me pieuter",
        "je vais roupiller", "je m endors", "je tombe de sommeil",
        "dodo", "au dodo", "go dodo", "je vais faire dodo", "je vais dodo", "jvais dodo",
    };
    private static readonly HashSet<string> _sleepFollowers = new()
    {
        "bonne", "bn", "gn", "maintenant", "la", "aussi", "moi", "bye", "ciao", "tchao", "salut", "bisous", "les",
        "allez", "bon", "perso", "direct", "enfin", "hein", "syncs", "sync",
    };

    private static readonly string[] _goodbyePhrases =
    {
        "a demain", "a plus tard", "a tout a l heure", "a tout de suite", "a tantot", "a bientot",
        "a la prochaine", "a la revoyure", "a ce soir", "a lundi", "a mardi", "a mercredi", "a jeudi",
        "a vendredi", "a samedi", "a dimanche",
        "au revoir", "bye", "byebye", "ciao", "tchao", "adieu", "see you", "see ya", "cya",
        "bonne soiree", "bonne journee", "bonne fin de journee", "bonne aprem", "bonne apres midi",
        "bon weekend", "bon week end", "bonne semaine", "bonne continuation", "a la semaine prochaine",
        "a2m1", "a2main", "a plouche",
        "tchuss", "tchus", "tschuss", "adios", "sayonara", "hasta la vista", "take care", "prends soin de toi",
        "je file", "je me casse", "je me tire", "je me barre", "je me sauve", "je decolle", "je m en vais", "je vous laisse",
        "je dois y aller", "faut que j y aille", "je vais y aller", "je vais y go", "je deco", "je me deco",
    };

    // « dors bien » wishes it; « tu dors bien la nuit ? » asks about it.
    private static readonly HashSet<string> _notGoodnightAfter = new() { "tu", "t" };

    // Goodbyes that count only closing the message: « bon, j'y vais » leaves, « j'y vais aussi
    // samedi » is coming along; « allez salut » leaves, while « salut » alone is a hello.
    private static readonly string[] _closingGoodbyePhrases =
    {
        "j y vais", "je go", "je bouge", "allez salut", "bon salut", "allez ciao", "allez bye",
    };

    // « à plus » and « à toute » are matched apart (IsShortFarewell): « y en a plus », « il a
    // toute la nuit » contain them too. They count opening or closing the message, never after one
    // of these words. « a+ » is read as « à plus » before tokenizing (_aPlus).
    private static readonly HashSet<string> _shortFarewells = new() { "plus", "toute", "tte" };
    private static readonly HashSet<string> _notFarewellBefore = new() { "y", "en", "il", "elle", "on", "n", "ya", "qui" };

    // Indecision, her pet peeve: a short reply that is only a « peut-être » (MaxMaybeTokens).
    private static readonly string[] _maybePhrases =
    {
        "peut etre", "ptet", "ptetre", "pt etre", "peutetre", "on verra", "on va voir", "a voir", "faut voir", "je verrai",
        "pourquoi pas", "ca depend", "jsp", "je sais pas", "je ne sais pas", "j sais pas", "jsais pas", "aucune idee",
        "je sais pas trop", "je sais plus", "bof je sais pas", "mouais peut etre", "j en sais rien", "jen sais rien",
        "chais pas", "chai pas", "chaipas", "chepa", "chais po", "qui sait", "on sait jamais", "si ca se trouve",
        "pas sur", "pas sure", "j hesite", "50 50", "idk", "dunno", "maybe", "perhaps",
    };

    // The same, but a negator before them reverses them: « pas possible », « c'est pas probable ».
    // The phrases above take none — « non, je sais pas » is still undecided.
    private static readonly string[] _maybeNegatablePhrases =
    {
        "possible", "probablement", "probable", "eventuellement", "ca se peut",
    };
    private const int MaxMaybeTokens = 5;

    // Whether she is a bot, an AI, a person: asked or stated, anywhere in the message.
    private static readonly string[] _areYouABotPhrases =
    {
        "t es un bot", "tu es un bot", "t es une bot", "t es qu un bot", "t es juste un bot", "c est qu un bot",
        "t es un robot", "tu es un robot", "t es une ia", "tu es une ia", "t es une intelligence artificielle",
        "t es chatgpt", "c est chatgpt", "t es humaine", "tu es humaine", "t es un humain", "t es une humaine",
        "t es une vraie personne", "tu es une vraie personne", "t es reelle", "tu es reelle", "t es vivante",
        "tu es vivante", "t es consciente", "tu es consciente", "tu ressens", "tu as des sentiments",
        "t as des sentiments", "tu as des emotions", "t as des emotions", "tu penses vraiment",
        "y a quelqu un derriere", "c est rodhengard qui", "c est papa qui", "c est un humain qui",
        "es tu un bot", "es tu une ia", "es tu humaine", "es tu reelle", "t es une machine", "tu es une machine",
        "t es un programme", "tu es un programme", "t es un ai", "t es une ai", "t es un llm", "t es chat gpt",
        "c est chat gpt", "t es gpt", "t es claude", "t es reel", "t es vraie", "tu existes vraiment",
        "t existes vraiment", "tu as une ame", "t as une ame", "tu as un coeur", "t as un coeur",
        "c est qui qui ecrit", "c est toi qui ecris", "qui ecrit derriere", "qui tape derriere",
        "are you a bot", "are you human", "are you real", "r u a bot", "you re a bot", "ur a bot",
    };

    // Agreeing with her. The phrases count in a reply of MaxDisagreementTokens at most (« j'ai bien vu
    // le film hier avec mon frère et ma sœur » agrees with nobody), the single words only as (nearly)
    // the whole reply, and « c'est vrai » and « vrai » not as a question (« c'est vrai ? » is « really? »).
    private static readonly string[] _youAreRightPhrases =
    {
        "t as raison", "tu as raison", "ta raison", "tas raison", "t a raison", "t as pas tort", "tu as pas tort",
        "t avais raison", "vous avez raison", "elle a raison", "syncs a raison",
        "trop raison", "grave raison", "tellement raison", "totalement raison", "completement raison",
        "entierement raison", "parfaitement raison", "bien raison",
        "pas faux", "c est pas faux", "bien vu", "trop vrai", "tellement vrai", "si vrai", "bien vrai", "so true",
        "je suis d accord", "d accord avec toi", "entierement d accord", "on est d accord", "tout a fait",
        "je valide", "je confirme", "je plussoie", "tu marques un point", "pas mieux", "j avoue", "javoue",
        "you re right", "youre right", "ur right", "fair enough", "true story",
    };
    private static readonly HashSet<string> _youAreRightWords = new()
    {
        "exact", "exactement", "exacto", "carrement", "grave", "vrai", "true", "facts", "absolument", "clairement",
        "evidemment", "effectivement", "parfaitement", "totalement", "precisement", "bingo", "indeed", "veridique",
        "plussoie",
    };
    private const int MaxAgreementTokens = 3;
    private static readonly string[] _cestVrai = { "c est vrai", "c est clair", "en effet" };

    // Disagreeing with her. The phrases count in a reply of MaxDisagreementTokens at most; the single
    // words only as (nearly) the whole reply — « c'est un faux compte » is not a contradiction — and
    // never after a negator (« pas faux » is agreement, caught above anyway).
    private static readonly string[] _youAreWrongPhrases =
    {
        "t as tort", "tu as tort", "tu te trompes", "tu t es trompee", "tu t trompes", "c est faux",
        "n importe quoi", "tu dis n importe quoi", "tu racontes n importe quoi",
        "mais non", "pas d accord", "je suis pas d accord", "c est pas vrai", "pas vrai",
        "rien a voir", "t es a cote de la plaque", "t es a l ouest",
        "t as faux", "tu as faux", "t es dans le faux", "tu es dans le faux", "archi faux", "completement faux",
        "totalement faux", "tu delires", "tu delire", "tu dis des betises", "tu dis des conneries",
        "tu racontes des betises", "tu racontes des conneries", "n imp", "c est pas ca",
        "jamais de la vie", "que nenni", "dans tes reves",
        "tu te plantes", "tu t es plantee", "tu t es trompe", "you re wrong", "youre wrong",
    };
    private static readonly string[] _youAreWrongWords = { "faux", "fake", "nope", "nimp", "wrong", "erreur" };
    private const int MaxDisagreementTokens = 8;

    // Adverbs that negate whatever verb they follow: « pas du tout » contradicts her as (nearly) the
    // whole reply, but « je m'ennuie pas du tout » is about being bored.
    private static readonly string[] _youAreWrongShortPhrases =
    {
        "pas du tout", "absolument pas", "pas vraiment", "certainement pas", "surement pas",
    };
    private const int MaxShortDisagreementTokens = 4;

    // A Request: asking her to do something (« tu peux… », « organise… »). Markers anywhere, or an
    // imperative opening the message; then a topic word says which command does it.
    private static readonly string[] _requestMarkers =
    {
        "tu peux", "peux tu", "tu pourrais", "pourrais tu", "tu veux bien", "tu voudrais", "est ce que tu peux",
        "tu sais faire", "stp", "svp", "s il te plait", "s il te plais", "s il vous plait",
        // These carry their own « pas », which would otherwise cancel the marker: « tu veux pas
        // lancer un sondage ? » asks for one.
        "tu veux pas", "tu pourrais pas", "tu voudrais pas",
        "j aimerais que tu", "je voudrais que tu", "je veux que tu", "faudrait que tu", "ca te dirait de",
    };
    // A marker right after one of these is a condition or a statement, not a request: « quand tu
    // peux », « si tu peux », « dès que tu peux », « je sais que tu peux ». « est-ce que tu peux » is
    // its own marker, so it still counts.
    private static readonly HashSet<string> _notRequestAfter = new() { "quand", "si", "que", "qu", "comme" };

    private static readonly HashSet<string> _requestImperatives = new()
    {
        "organise", "organiser", "lance", "lancer", "relance", "fais", "fait", "cree", "creer", "prepare", "programme",
        "planifie", "rappelle", "mute", "ban", "kick", "vire", "tire", "montre", "aide",
        "ouvre", "ajoute", "mets", "trouve", "choisis", "genere", "envoie", "donne", "annule", "decale", "reporte",
        "modifie", "previens", "melange",
    };
    private static readonly (RequestTopic Topic, string[] Words)[] _requestTopics =
    {
        // Tonight before Session: « organise une partie ce soir » is /tonight's job.
        (RequestTopic.Tonight, new[] { "ce soir", "tonight" }),
        (RequestTopic.Moderation, new[] { "mute", "ban", "bannir", "bannis", "kick", "expulse", "expulser", "vire", "virer",
            "timeout", "warn", "punis", "punir", "exclus", "exclure" }),
        (RequestTopic.Poll, new[] { "sondage", "sondages", "vote", "voter", "votes", "poll", "strawpoll" }),
        (RequestTopic.Reminder, new[] { "rappel", "rappels", "rappelle", "rappelles", "rappeler", "remind", "remindme",
            "previens", "prevenir", "reveiller" }),
        (RequestTopic.Teams, new[] { "equipe", "equipes", "team", "teams", "groupes" }),
        (RequestTopic.Giveaway, new[] { "giveaway", "giveaways", "tirage", "concours", "loterie", "tombola" }),
        (RequestTopic.Quiz, new[] { "quiz", "interro", "questionnaire", "trivia", "culture generale", "culture g" }),
        (RequestTopic.Session, new[] { "session", "sessions", "soiree", "partie", "game", "event", "evenement", "planning",
            "seance", "raid", "lan", "rdv", "rendez vous" }),
    };

    // Checking she's still there. Each phrase must close the message or its question, or be followed
    // by one of _thereFollowers (IsClosingAsk): « t'es là ? » and « tu dors ou quoi » ask, « t'es là
    // ce soir ? » and « t'es la meilleure » don't. Her name alone with a « ? » counts too (ReadIntent).
    private static readonly string[] _areYouTherePhrases =
    {
        "t es la", "tu es la", "tes la", "t es toujours la", "t es encore la", "tu es toujours la", "t es bien la",
        "tu dors", "t es reveillee", "tu es reveillee", "t es en vie", "tu es en vie", "t es dans les vapes",
        "y a quelqu un", "il y a quelqu un", "ya quelqu un", "y a qqn", "ya qqn", "ohe", "eho", "ho he",
        "t es partie", "t es passee ou", "t as disparu", "t es plus la", "tu es plus la",
        // Her silence noticed: « tu m'ignores ? », « tu réponds plus ? ». Closing only, like the rest:
        // « tu réponds pas à ma question ? » asks about the answer, « t'es morte de rire » laughs.
        "tu m ignores", "tu m ignore", "pourquoi tu m ignores", "tu reponds plus", "tu reponds pas",
        "tu me reponds plus", "tu me reponds pas", "reponds moi", "t es morte", "tu es morte", "t es mort",
    };
    private static readonly HashSet<string> _thereFollowers = new()
    {
        "syncs", "sync", "toi", "la", "encore", "ou", "quoi", "hein", "pas", "oui", "dis", "maintenant",
    };

    // Waking her up: anywhere, but short. « debout » alone would be « stand up » in a game.
    private static readonly string[] _wakeUpPhrases = { "reveille toi", "reveilles toi", "debout la dedans", "wake up" };
    private const int MaxWakeUpTokens = 5;

    // « allô ? », « allô syncs ? »: opening the message, with nothing but these after it. Not « nan
    // mais allô » or « allô quoi », the meme that means « are you serious? ».
    private static readonly HashSet<string> _alloFollowers = new()
    {
        "syncs", "sync", "allo", "toi", "la", "hein", "y", "ya", "a", "il", "quelqu", "un", "qqn",
    };

    // Questions about her: how old, when she was born, where she lives, what she likes. Read before
    // the opinion (« tu aimes quoi comme jeu ? » would rate « quoi comme jeu »).
    private static readonly (HerTopic Topic, string[] Phrases)[] _aboutHerPhrases =
    {
        (HerTopic.Age, new[]
        {
            "t as quel age", "tu as quel age", "ta quel age", "quel age tu as", "quel age as tu", "quel age t as",
            "t as combien d ans", "tu as combien d ans", "ton age", "t es vieille", "tu es vieille", "t es jeune",
            "how old are you", "how old r u",
        }),
        (HerTopic.Birthday, new[]
        {
            "ton anniversaire", "ton anniv", "ton anni", "ta date de naissance", "t es nee quand", "tu es nee quand",
            "quand es tu nee", "quand est ce que t es nee", "t es nee le", "tu es nee le", "ta naissance",
            "your birthday",
        }),
        (HerTopic.Home, new[]
        {
            "t habites ou", "tu habites ou", "tu vis ou", "ou tu vis", "ou tu habites", "ou est ce que tu vis",
            "ou est ce que tu habites", "tu viens d ou", "d ou tu viens", "d ou viens tu", "tu tournes sur quoi",
            "tu tournes ou", "t es hebergee ou", "tu es hebergee ou",
            "where do you live",
        }),
        (HerTopic.Favourite, new[]
        {
            "tu aimes quoi", "t aimes quoi", "tu kiffes quoi", "t kiffes quoi", "qu est ce que tu aimes",
            "qu est ce que t aimes", "tu manges quoi", "tu bois quoi", "tu ecoutes quoi", "tu joues a quoi",
            "c est quoi tes gouts", "tes gouts",
            // « t'aimes quel jeu ? » asks her taste; read later, the opinion would rate « quel jeu ».
            // No « tu préfères quel »: « tu préfères quel jeu, Minecraft ou Fortnite ? » is PreferLines.
            "tu aimes quel", "t aimes quel", "tu aimes quelle", "t aimes quelle", "tu aimes quels", "t aimes quels",
            "tu aimes quelles", "t aimes quelles", "tu kiffes quel", "t kiffes quel", "tu kiffes quelle", "t kiffes quelle",
            "tu preferes quoi comme", "t preferes quoi comme",
            "t as des hobbies", "tu as des hobbies", "t as un hobby", "tu as un hobby", "tes hobbies", "ton hobby",
            "t as des passions", "tu as des passions", "t as une passion", "tu as une passion", "tes passions",
            "t as des loisirs", "tu as des loisirs", "tes loisirs",
        }),
    };

    // Her version: « t'es en quelle version ? », « c'est quoi ta version ? », « t'es à jour ? ».
    // Closing the message or its question, or before _versionFollowers only: « t'es en quelle
    // version de Minecraft ? » and « ta version des faits » are something else.
    private static readonly string[] _versionPhrases =
    {
        "t es en quelle version", "tu es en quelle version", "tes en quelle version", "t es quelle version",
        "tu es quelle version", "tes quelle version", "t es a quelle version", "tu es a quelle version",
        "t en es a quelle version", "tu en es a quelle version", "quelle version tu es", "quelle version es tu",
        "quelle version t es", "t es en version combien", "tu es en version combien", "tu tournes en quelle version",
        "tu tournes sur quelle version", "t as quelle version", "tu as quelle version",
        "c est quoi ta version", "c quoi ta version", "quelle est ta version", "ta version c est quoi",
        "ta version actuelle", "ton numero de version", "ta derniere version", "ta derniere mise a jour",
        "ta derniere maj", "t es a jour", "tu es a jour", "tes a jour",
        "what version are you", "what version are you on", "what s your version", "which version are you",
    };
    private static readonly HashSet<string> _versionFollowers = new()
    {
        "syncs", "sync", "actuellement", "maintenant", "aujourd", "exactement", "deja", "la", "now", "today",
    };

    // « ton jeu préféré », « ta couleur favorite »: a possessive, then « préféré » within three words.
    private static readonly HashSet<string> _yourWords = new() { "ton", "ta", "tes" };
    private static readonly HashSet<string> _favouriteWords = new()
    {
        "prefere", "preferee", "preferes", "preferees", "prefer", "favori", "favorite", "favoris", "favorites", "fav",
    };
    private const int FavouriteWindow = 3;

    // Her love life, asked about: « t'es célibataire ? », « t'as un crush ? ». Not a declaration —
    // « je suis amoureux de toi » is Love, read first.
    private static readonly string[] _crushPhrases =
    {
        "t es celibataire", "tu es celibataire", "t es en couple", "tu es en couple", "t as un copain",
        "tu as un copain", "t as un mec", "tu as un mec", "t as une copine", "t as un amoureux", "tu as un amoureux",
        "t as quelqu un dans ta vie", "tu as quelqu un dans ta vie", "t as un crush", "tu as un crush", "c est qui ton crush",
        "ton crush", "t es amoureuse", "tu es amoureuse", "tu aimes quelqu un", "t aimes quelqu un",
        "tu sors avec quelqu un", "t sors avec quelqu un", "ton genre de robot", "ton style de robot",
        "t as un faible pour", "tu as un faible pour",
        "tu kiffes quelqu un", "t kiffes quelqu un", "your crush",
        "t aimes bien qui sur le serveur", "tu aimes bien qui sur le serveur", "t aimes qui sur le serveur",
        "tu aimes qui sur le serveur", "tu kiffes qui sur le serveur", "t as quelqu un en vue", "tu as quelqu un en vue",
        "qui te fait craquer",
    };

    // The same asked short, closing the message or before _crushFollowers (IsClosingAsk): « tu kiffes
    // qui ? », « t'as quelqu'un ? ». « t'as quelqu'un pour jouer ? » wants a teammate, « tu aimes qui
    // comme perso ? » and « tu kiffes qui en jungle ? » are the game.
    private static readonly string[] _crushClosingPhrases =
    {
        "tu kiffes qui", "t kiffes qui", "tu aimes qui", "t aimes qui", "tu aimes bien qui", "t aimes bien qui",
        "t as quelqu un", "tu as quelqu un",
    };
    private static readonly HashSet<string> _crushFollowers = new()
    {
        "syncs", "sync", "hein", "ici", "secretement", "vraiment", "alors", "toi",
    };

    // What she feels about the asker: « tu m'aimes ? », « tu me détestes ? », « tu m'en veux ? ».
    // Closing the message or its question, or before _lovesMeFollowers (IsClosingAsk), with or
    // without a « ? »: « tu m'aimes hein » asks too. « tu m'en veux si je pars ? » and « tu me
    // supportes en bot lane ? » are something else. Read after Love (« je t'aime, tu m'aimes ? »
    // declares) and Apology (« désolé, tu m'en veux ? » apologises).
    private static readonly string[] _doYouLoveMePhrases =
    {
        "tu m aimes", "tu m aime", "t m aimes", "tu maimes", "tu m adores", "tu m adore", "tu me kiffes",
        "tu me kiffe", "tu m apprecies", "tu m apprecie", "tu me detestes", "tu me deteste", "tu me hais",
        "tu m en veux", "tu m en veut", "tu me supportes", "tu me supporte",
        "je suis ton prefere", "je suis ta preferee", "c est moi ton prefere", "c est moi ta preferee",
        "je suis ton humain prefere", "je suis ta personne preferee", "je suis ton chouchou", "je suis ta chouchoute",
        "do you love me", "do you hate me",
    };
    private static readonly HashSet<string> _lovesMeFollowers = new()
    {
        "bien", "un", "peu", "pas", "plus", "encore", "toujours", "hein", "syncs", "sync", "vraiment", "quand",
        "meme", "moi", "au", "moins", "beaucoup", "trop", "ou", "quoi", "non", "oui", "dis", "alors", "du",
        "tout", "secretement", "maintenant", "aussi",
    };

    // Whether she remembers the asker: « tu te souviens de moi ? », « comment je m'appelle ? ».
    // Closing, or before _rememberFollowers: « tu te souviens de moi en vocal hier ? » and « tu sais
    // qui je suis allé voir ? » are something else. Read after WhoAreYou (« c'est quoi ton nom » is
    // hers; « c'est quoi mon nom » is theirs).
    private static readonly string[] _rememberMePhrases =
    {
        "tu te souviens de moi", "tu te souviens pas de moi", "tu te rappelles de moi", "tu te rappelle de moi",
        "tu te rappelles moi", "tu te rappelles pas de moi", "tu sais qui je suis", "tu sais c est qui moi",
        "tu sais comment je m appelle", "comment je m appelle", "comment je m apelle", "comment je m appel",
        "c est quoi mon nom", "c est quoi mon prenom", "c est quoi mon pseudo", "mon nom c est quoi",
        "mon prenom c est quoi", "mon pseudo c est quoi", "tu connais mon nom", "tu connais mon prenom",
        "tu connais mon pseudo", "tu sais mon nom", "tu sais mon prenom", "tu m as oublie", "tu m as oubliee",
        "tu m as deja oublie", "tu m as deja oubliee", "tu m oublies", "do you remember me",
    };

    // The same, only asked: « tu me connais, je suis toujours en retard » says "you know me",
    // and « je suis qui pour juger » asks nothing of her.
    private static readonly string[] _rememberMeAskedPhrases =
    {
        "tu me connais", "tu me reconnais", "je suis qui", "qui suis je", "c est qui moi", "who am i",
    };
    private static readonly HashSet<string> _rememberFollowers = new()
    {
        "syncs", "sync", "deja", "au", "moins", "hein", "encore", "toujours", "vraiment", "ou", "quoi", "pas",
        "moi", "dis", "alors", "bien", "meme", "enfin",
    };

    // Her mood, asked: « t'es fâchée ? », « tu boudes ? », « t'es triste ? », « t'es jalouse ? ».
    // Only with a « ? », closing the message or its question, or before _upsetFollowers: « t'es
    // triste » said flat is slang for lame, and the mean side answers it. Both genders listed: some
    // get hers wrong, and the question is the same.
    private static readonly string[] _areYouUpsetPhrases =
    {
        "t es fachee", "t es fache", "tu es fachee", "tu es fache", "t es pas fachee", "t es pas fache",
        "t es en colere", "tu es en colere", "t es enervee", "t es enerve", "tu es enervee",
        "tu boudes", "tu boude", "tu fais la tete", "tu me fais la tete", "tu fais la gueule", "tu me fais la gueule",
        "t es vexee", "t es vexe", "tu es vexee", "t es pas vexee", "t es pas vexe",
        "t es triste", "tu es triste", "t es pas triste", "t es jalouse", "t es jaloux", "tu es jalouse",
        "t es pas jalouse", "t es pas jaloux", "t es de mauvaise humeur", "t es blessee", "tu es blessee",
        "t es deg", "t es degoutee", "t es degoute",
    };
    private static readonly HashSet<string> _upsetFollowers = new()
    {
        "syncs", "sync", "contre", "encore", "toujours", "hein", "ou", "quoi", "la", "vraiment", "un", "peu",
        "maintenant", "de", "pas", "avec", "moi", "aujourd", "depuis", "aussi", "alors",
    };

    // A reply that is only a yes or a no (ReadAnswer), compared squashed (« ouiii », « nooon »), once
    // _answerFillers are dropped and repeats folded (« oui oui », « bah non »). Not « exactement »'s
    // agreement nor « peut-être », which keep their own intents.
    private static readonly HashSet<string> _yesAnswers = SquashAll(
        "oui", "ouais", "ouai", "ouaip", "ouep", "ui", "wi", "yes", "yep", "yup", "yeah", "si", "carrement", "grave",
        "evidemment", "evidement", "bien sur", "bien entendu", "absolument", "tout a fait", "affirmatif",
        "clairement", "totalement", "certainement", "of course", "ofc", "volontiers", "avec plaisir", "oui merci");
    private static readonly HashSet<string> _noAnswers = SquashAll(
        "non", "nan", "nn", "no", "nope", "nah", "niet", "pas du tout", "du tout", "absolument pas", "jamais",
        "certainement pas", "surtout pas", "negatif", "pas vraiment", "non merci", "jamais de la vie");
    private static readonly HashSet<string> _answerFillers = SquashAll(
        "bah", "ben", "beh", "euh", "heu", "ah", "oh", "eh", "bon", "mais", "alors", "hm", "hmm", "mh", "bref");
    private const int MaxAnswerTokens = 5;

    private static HashSet<string> SquashAll(params string[] cues) => cues.Select(Squash).ToHashSet();

    // Asking how she works or what she can do: pointed at /help, or at the topic's command when one
    // is named (« comment ça marche les sondages ? »). Read before Request, which takes over when a
    // request names a topic (« tu peux m'aider à organiser une session ? »).
    private static readonly string[] _helpPhrases =
    {
        "comment ca marche", "comment ca fonctionne", "comment ca s utilise",
        "comment on utilise", "comment t utiliser", "comment on t utilise", "comment je t utilise",
        "tu sais faire quoi", "qu est ce que tu sais faire", "que sais tu faire", "tu peux faire quoi",
        "qu est ce que tu peux faire", "c est quoi les commandes", "c est quoi tes commandes", "quelles commandes",
        "tes commandes", "la liste des commandes", "liste des commandes",
        "t as des commandes", "tu as des commandes", "t as quoi comme commandes", "tu as quoi comme commandes",
        // « pas » is inside the phrase, so the negator check before it can't cancel it.
        "sais pas comment t utiliser", "sais pas comment on t utilise", "sais pas comment je t utilise",
        "help", "aide moi", "aidez moi", "a l aide", "j ai besoin d aide", "besoin d aide", "tu peux m aider",
        "tu pourrais m aider",
    };
    // Only closing the message: « je suis perdu dans le donjon » is the game, « comment on fait pour
    // battre ce boss » and « je comprends rien à ce boss » ask about it.
    // In a short message only: « … qu'il m'a dit, je comprends rien » is about someone else.
    private const int MaxHelpClosingTokens = 6;
    private static readonly string[] _helpClosingPhrases =
    {
        "je suis perdu", "je suis perdue", "jsuis perdu", "jsuis perdue", "comment on fait", "comment je fais",
        "je comprends rien", "j y comprends rien", "je capte rien", "je pige rien",
    };

    // « comment on fait un sondage ? », « comment je fais pour créer une session ? »: how to use one of
    // her features, anywhere in the message, but only with one of _featureWords. The request topics are
    // too wide for it (« partie », « raid », « équipe »: « comment on fait pour finir le raid ? » is
    // the game), so they still pick the command once this has matched.
    private static readonly string[] _howToPhrases =
    {
        "comment on fait", "comment je fais", "comment faire", "comment on cree", "comment je cree", "comment creer",
        "comment on lance", "comment je lance", "comment lancer", "comment on organise", "comment organiser",
        "comment on programme", "comment on met", "comment je mets", "comment mettre",
    };
    private static readonly HashSet<string> _featureWords = new()
    {
        "sondage", "sondages", "poll", "rappel", "rappels", "remindme", "session", "sessions", "giveaway", "giveaways",
        "quiz", "commande", "commandes",
    };

    // Boredom, said to her: she has games for that. A negator around cancels (« je m'ennuie pas »).
    // No bare « rien à faire »: « j'en ai rien à faire » doesn't care. In a message of MaxBoredTokens at most.
    // Never followed by « de » (_missingWords): « je m'ennuie de toi » misses someone.
    private static readonly string[] _boredPhrases =
    {
        "je m ennuie", "jm ennuie", "je m emmerde", "je me fais chier", "jme fais chier", "je me fais ch",
        "on s ennuie", "on s emmerde", "on se fait chier",
        "je sais pas quoi faire", "jsp quoi faire", "j sais pas quoi faire",
        "c est mort ici", "c est calme ici", "personne joue",
        "i m bored", "im bored", "so bored", "boring", "ennui", "l ennui",
    };
    private static readonly HashSet<string> _missingWords = new() { "de", "d", "du", "des" };

    // « rien à faire » only closing the message or before _nothingToDoFollowers: « y a rien à faire
    // ce soir » is bored, « y a rien à faire contre ce boss » is hopeless.
    private static readonly string[] _nothingToDoPhrases =
    {
        "y a rien a faire", "ya rien a faire", "il y a rien a faire", "j ai rien a faire",
    };
    private static readonly HashSet<string> _nothingToDoFollowers = new()
    {
        "ici", "la", "aujourd", "ce", "cet", "cette", "en", "sur", "de", "du", "dans", "a", "pendant", "trop",
    };
    // « on fait quoi ? » only closing the message: « on fait quoi pour la session de samedi » plans.
    private static readonly string[] _boredClosingPhrases = { "on fait quoi", "on fait koi", "on fait quoi la" };
    private const int MaxBoredTokens = 10;

    // Feeling down, said about themselves: she answers with something gentle to hold on to. Never
    // when the message is laughing (« j'en peux plus 😂 », « je pleure de rire »).
    // « je suis » / « je me sens », up to two of _downIntensifiers, then one of _downAdjectives:
    // « je suis trop triste », not « je suis pas triste ».
    private static readonly string[] _downSubjects =
    {
        "je suis", "jsuis", "j suis", "chui", "chuis", "je me sens", "jme sens", "je me sent", "je me suis senti",
        "je me suis sentie",
    };
    private static readonly HashSet<string> _downIntensifiers = new()
    {
        "trop", "tellement", "si", "vraiment", "grave", "super", "hyper", "mega", "tres", "un", "peu", "assez",
        "completement", "totalement", "carrement", "encore", "toujours", "tout", "toute", "a", "au", "bien",
    };
    private static readonly HashSet<string> _downAdjectives = new()
    {
        "triste", "tristes", "deprime", "deprimee", "deg", "degoute", "degoutee", "decu", "decue", "down", "sad",
        "abattu", "abattue", "demoralise", "demoralisee", "desespere", "desesperee", "malheureux", "malheureuse",
        "seul", "seule", "stresse", "stressee", "angoisse", "angoissee", "anxieux", "anxieuse", "vide", "nul", "nulle",
        "blase", "blasee", "bout", "plat", "mal",
    };
    // Anywhere in the message; a negator before cancels (« j'ai jamais le cafard »).
    private static readonly string[] _downPhrases =
    {
        "j ai pas le moral", "j ai plus le moral", "jai pas le moral", "pas le moral", "plus le moral", "j ai le cafard",
        "jai le cafard", "coup de cafard", "coup de mou", "coup de blues", "gros coup de mou", "j en peux plus", "jen peux plus", "j en ai marre",
        "jen ai marre", "j en ai ras le bol", "ras le bol", "rien ne va", "plus rien ne va", "tout va mal", "ma vie est nulle",
        "envie de pleurer", "je deprime", "je vais mal", "j ai le seum", "jai le seum",
        "personne m aime", "i m sad", "im sad", "i feel bad",
    };
    // About their day, not a question: « sale journée » closes nothing, but « t'as passé une sale
    // journée ? » asks her.
    private static readonly string[] _downDayPhrases =
    {
        "sale journee", "journee de merde", "journee pourrie", "journee horrible", "journee difficile", "dure journee",
        "mauvaise journee", "journee nulle", "pas une bonne journee", "sale semaine", "semaine de merde",
        "semaine pourrie", "semaine horrible", "sale soiree", "soiree pourrie",
    };
    // Closing the message or a sentence, or before _downFollowers (IsClosingAsk), and never asked:
    // « ça va pas fort », « je craque ». Not « ça va pas la tête », « ça va pas ce soir » (can't
    // come), « je craque pour ce skin ». No « je pleure »: in chat it is mostly laughing.
    private static readonly string[] _downClosingPhrases =
    {
        "ca va pas", "ca va pas fort", "ca va pas trop", "ca va pas du tout", "sa va pas", "pas la forme",
        "pas trop la forme", "je craque", "je vais craquer", "je suis a bout", "je suis au bout",
        "ca va pas en ce moment", "ca va pas fort en ce moment", "ca va pas trop en ce moment", "pas la forme en ce moment",
    };
    // No « ce », « la », « en », « du »: « ça va pas ce soir », « … la tête », « … du tout ce patch ».
    private static readonly HashSet<string> _downFollowers = new()
    {
        "vraiment", "syncs", "sync", "hein", "fort", "trop", "aujourd", "top", "moi",
    };

    // « je devrais y aller ? », « je l'achète ? »: a yes-or-no about what they should do, answered
    // with /yesno's verdict. Only asked (a « ? »): « je devrais dormir » is said, not asked.
    private static readonly string[] _shouldIPhrases =
    {
        "je devrais", "jdevrais", "j devrais", "est ce que je devrais", "tu crois que je devrais", "tu penses que je devrais",
        "je ferais bien de", "je ferais mieux de", "c est une bonne idee", "bonne idee ou pas", "oui ou non",
        "je tente", "je me lance", "je l achete", "je le prends", "je la prends", "je les prends", "je le fais",
        "je la fais", "je fonce", "je lui ecris", "je lui dis", "je lui reponds", "je l appelle", "je postule",
        "should i",
    };
    // « … ou pas ? » closing a first-person question: « j'y vais ou pas ? », « on le fait ou pas ? ».
    private static readonly HashSet<string> _firstPerson = new() { "je", "j", "jsuis", "on", "me", "m", "moi" };
    // An open question is no yes-or-no: « je lui dis quoi ? », « je le fais comment ? ».
    private static readonly HashSet<string> _openQuestionWords = new()
    {
        "quoi", "koi", "comment", "combien", "quand", "pourquoi", "qui", "quel", "quelle", "quels", "quelles", "lequel", "laquelle",
    };

    // « raconte une blague », « fais-moi rire ». « c'est une blague ? » asks whether something is
    // one, and « sans blague » is surprise: only these phrases, never a bare « blague ».
    private static readonly string[] _jokePhrases =
    {
        "raconte une blague", "raconte moi une blague", "raconte nous une blague", "raconte des blagues",
        "dis une blague", "dis moi une blague", "dis nous une blague", "fais une blague", "fais moi une blague",
        "fais nous une blague", "t as une blague", "tu as une blague", "t as pas une blague", "tu connais une blague",
        "tu connais des blagues", "tu connais pas une blague", "une autre blague", "encore une blague",
        "raconter une blague", "raconter des blagues", "me faire rire", "nous faire rire",
        "fais moi rire", "fais nous rire", "fait moi rire", "dis un truc drole", "dis moi un truc drole",
        "dis quelque chose de drole", "raconte un truc drole", "tell me a joke", "say something funny",
    };
    // « une blague ? », « une blague stp »: the whole message.
    private static readonly HashSet<string> _jokeAlone = new() { "une blague", "blague", "une blague stp", "une blague svp" };

    // « un fun fact ? », « apprends-moi un truc »: one of her morning facts.
    private static readonly string[] _funFactPhrases =
    {
        "fun fact", "un fait interessant", "un truc interessant", "dis moi un truc interessant", "apprends moi un truc",
        "apprends moi quelque chose", "apprends nous un truc", "apprends nous quelque chose", "une anecdote",
        "raconte une anecdote", "raconte moi une anecdote", "le saviez vous", "tu savais que quoi", "random fact",
    };
    // « dis-moi un truc », « raconte-moi quelque chose »: closing only, or « dis-moi un truc, tu viens
    // ce soir ? » would get a fact.
    private static readonly string[] _funFactClosingPhrases =
    {
        "dis moi un truc", "raconte moi un truc", "raconte moi quelque chose", "dis moi quelque chose",
        "raconte nous un truc", "raconte nous quelque chose", "dis nous un truc", "dis quelque chose",
    };
    private static readonly HashSet<string> _funFactFollowers = new() { "stp", "svp", "syncs", "sync", "please", "pls", "toi" };

    // « c'est la faute de qui ? »: /blame's verdict, drawn from the asker's voice call. Asked only.
    private static readonly string[] _blamePhrases =
    {
        "c est la faute de qui", "c est la faute a qui", "c est de la faute de qui", "c est de la faute a qui",
        "la faute de qui", "la faute a qui", "a qui la faute", "c est qui le coupable", "qui est le coupable",
        "c est qui le responsable", "qui est responsable", "qui on blame", "on blame qui", "qui est ce qu on blame",
        "c est a cause de qui", "a cause de qui", "who s to blame", "whose fault",
    };

    // Asking her to apologise: « excuse-toi », « dis pardon », « tu me dois des excuses ». Read before
    // Apology, whose « pardon » and « désolé » sit inside « dis pardon » and « dis désolée ». She
    // refuses; Papa gets her apology (OwnerIntentLines). « excuse-moi » is theirs: Apology.
    private static readonly string[] _demandApologyPhrases =
    {
        "excuse toi", "excuses toi", "excusez vous", "dis pardon", "dis moi pardon", "dis nous pardon", "dis lui pardon",
        "demande pardon", "demande moi pardon", "demande nous pardon", "dis desole", "dis desolee", "dis moi desolee",
        "dis sorry", "say sorry", "apologize", "apologise", "tu me dois des excuses", "tu nous dois des excuses",
        "tu dois t excuser", "faut que tu t excuses", "tu devrais t excuser", "je veux des excuses", "j attends des excuses",
        "j exige des excuses", "on attend des excuses", "fais tes excuses", "presente tes excuses", "presente moi tes excuses",
        "des excuses peut etre", "tu t excuses pas", "tu vas t excuser", "tu t excuses quand", "j attends tes excuses",
        "tu peux t excuser", "tu pourrais t excuser", "tu vas pas t excuser", "je veux que tu t excuses",
        "j aimerais que tu t excuses", "tu dois me demander pardon", "tu dois demander pardon", "dis que t es desolee",
        "dis que tu es desolee", "j attends toujours tes excuses", "elles sont ou mes excuses", "et mes excuses",
        "fais des excuses", "presente des excuses", "des excuses svp", "des excuses stp", "des excuses s il te plait",
    };
    // « tu t'excuses ? », « des excuses ? »: asked, closing.
    private static readonly string[] _demandApologyAsked = { "tu t excuses", "tu t excuse", "tu demandes pardon", "des excuses" };
    // Said by them or of someone else, not asked of her: « je lui dis pardon », « je demande pardon »
    // (an apology: Apology), « il m'a demandé pardon ».
    private static readonly HashSet<string> _notDemandAfter = new()
    {
        "je", "j", "lui", "leur", "il", "elle", "on", "me", "m", "te", "nous", "vous", "ils", "elles", "qu", "que",
        "a", "as", "ai", "avait", "avais",
    };

    // Telling her to be quiet: she sulks and keeps quiet in that channel for ChatHush.Quiet. In a short
    // message only (MaxShutUpTokens): « tais-toi » inside a long one is a quote or a story. « ta gueule »
    // stays an insult (the mean pools); a mean message never reads an intent at all.
    private static readonly string[] _shutUpPhrases =
    {
        "tais toi", "tait toi", "tes toi", "taisez vous", "arrete de parler", "arretes de parler", "arrete de repondre",
        "arrete de nous repondre", "arrete de me repondre", "parle plus", "arrete de spammer", "arrete de spam",
        "laisse nous tranquille", "laisse moi tranquille", "fiche nous la paix", "fous nous la paix", "laisse nous parler",
        "on t a rien demande", "personne t a demande", "personne t a sonnee", "on t a pas sonnee", "shut up",
        "mets toi en sourdine", "mode silencieux", "baisse d un ton", "ferme ta bouche", "arrete de causer",
        "arrete de jacasser", "arrete de bavarder", "mute toi", "mets la en veilleuse", "mets la sourdine",
        "un peu de silence", "moins de bruit", "personne t a sonne", "on t a pas sonne",
    };
    // Only closing the message: « tu parles trop » shushes her, « tu parles trop vite » doesn't.
    private static readonly string[] _shutUpClosing =
    {
        "tu parles trop", "t parles trop", "tu parles beaucoup trop", "parle moins", "tu fais trop de bruit",
    };
    private static readonly HashSet<string> _shutUpClosingFollowers = new()
    {
        "syncs", "sync", "la", "toi", "hein", "franchement", "stp", "svp", "aujourd", "hui",
    };
    // The whole message, give or take her name and a « stp »: « chut », « silence », « la ferme ».
    // Not inside a sentence: « chut, il dort » is said to the room, « silence radio depuis hier »,
    // « à la ferme », « ferme la porte » and « boucle la ceinture » are something else.
    // Compared squashed, both sides: « chuuut » is « chut », « shhh » is « sh », « laisse » is « laise ».
    private static readonly HashSet<string> _shutUpAlone = SquashAll(
        "chut", "silence", "la ferme", "ferme la", "ferme", "stop", "chut chut", "sh", "silence radio", "boucle la",
        "la boucle", "laisse nous", "laisse moi", "stfu");
    private static readonly HashSet<string> _shutUpFillers = new()
    {
        "syncs", "sync", "stp", "svp", "toi", "un", "peu", "maintenant", "la", "deja", "bon", "allez", "oh", "eh", "mais",
        "please", "pls", "hein", "merci", "s", "il", "te", "plait", "plais", "vous",
    };
    private const int MaxShutUpTokens = 6;
    private static readonly HashSet<int> _noBreaks = new();

    // Their own numbers (/recap's), by what they ask about. Asked only: a « ? », or a question word in
    // the phrase itself (« je suis quel niveau », « combien j'ai de cailloux »), or « montre ».
    // « j'ai gagné 200 cailloux » and « mon plynling a faim » tell, and stay Sharing.
    private static readonly (MyStatsTopic Topic, string[] Phrases)[] _myStatsPhrases =
    {
        (MyStatsTopic.Level, new[]
        {
            "je suis quel niveau", "je suis niveau combien", "je suis a quel niveau", "j en suis a quel niveau",
            "j ai quel niveau", "j ai quel lvl", "je suis quel lvl", "c est quoi mon niveau", "quel est mon niveau",
            "c est quoi mon lvl", "combien j ai d xp", "j ai combien d xp", "combien d xp j ai", "j ai combien d experience",
            "c est quoi mon xp", "je suis combien au classement", "je suis ou au classement", "je suis quel rang",
            "c est quoi mon rang", "quel est mon rang", "je suis classe combien", "je suis combien dans le classement",
            "je suis level combien", "je suis lvl combien", "quel niveau je suis", "c est quoi mon level", "j ai quel level",
            "j ai combien de niveaux", "combien j ai de niveaux", "je suis classe ou", "c est quoi ma place au classement",
            "ma place au classement", "je suis ou dans le classement", "mon xp c est combien", "mon niveau c est combien",
        }),
        (MyStatsTopic.Balance, new[]
        {
            "combien j ai de cailloux", "j ai combien de cailloux", "combien de cailloux j ai", "c est quoi mon solde",
            "quel est mon solde", "il me reste combien de cailloux", "combien il me reste de cailloux", "je suis riche",
            "je suis pauvre", "j ai des cailloux", "j ai combien d argent", "combien j ai d argent", "j ai combien de sous",
            "combien j ai de sous", "j ai combien de thunes", "combien j ai de thunes",
        }),
        (MyStatsTopic.Plynling, new[]
        {
            "comment va mon plynling", "mon plynling va bien", "il va bien mon plynling", "mon plynling va comment",
            "des nouvelles de mon plynling", "comment il va mon plynling", "comment se porte mon plynling",
            "mon plynling est vivant", "mon plynling est en vie", "il est vivant mon plynling", "mon plynling il va bien",
            "il devient quoi mon plynling", "des news de mon plynling",
        }),
        (MyStatsTopic.All, new[]
        {
            "mes stats", "mes statistiques", "c est quoi mes stats", "montre moi mes stats", "mes chiffres", "mon profil",
            "mon bilan", "donne moi mes stats", "montre moi mon profil", "mon recap",
        }),
    };
    // Words that make a phrase asked without its « ? ».
    private static readonly HashSet<string> _myStatsAskWords = new() { "quel", "combien", "comment", "quoi", "ou", "montre", "donne" };
    // « mon niveau ? », « mes cailloux ? »: asked, short, closing.
    private static readonly (MyStatsTopic Topic, string[] Phrases)[] _myStatsShortAsks =
    {
        (MyStatsTopic.Level, new[] { "mon niveau", "mon lvl", "mon level", "mon xp", "mes xp", "mon rang", "mon classement" }),
        (MyStatsTopic.Balance, new[] { "mes cailloux", "mon solde", "mon argent", "mes sous", "ma thune", "mon porte monnaie", "ma bourse" }),
        (MyStatsTopic.Plynling, new[] { "mon plynling" }),
    };
    private static readonly HashSet<string> _myStatsFollowers = new()
    {
        "syncs", "sync", "stp", "svp", "actuel", "actuellement", "la", "maintenant", "alors", "deja", "exactement", "s",
    };
    private const int MaxMyStatsShortTokens = 4;

    // When the next session is, or who's coming: answered from the sessions themselves. Asked only (a
    // « ? », or opening on « quand », « qui », « y a »…). Read before ShouldI (« on joue ce soir ou
    // pas ? ») and Request (« tu peux me dire c'est quand la prochaine session ? »). Not « on lance une
    // game ce soir ? » or « on joue à quoi ce soir » (proposals), not « tu viens ce soir ? » (her), not
    // « qui est chaud pour un raid ? » (a call to the room), and never « la prochaine partie / game »:
    // « la prochaine game tu joues support ? » is the game.
    private static readonly string[] _nextSessionPhrases =
    {
        "la prochaine session", "prochaine session", "les prochaines sessions", "la prochaine soiree", "prochaine soiree",
        "le prochain event", "prochain event", "le prochain evenement",
        "y a une session", "il y a une session", "ya une session", "y a session", "y a des sessions", "y a pas de session",
        "il y a pas de session", "y a une soiree", "y a quoi de prevu", "il y a quoi de prevu", "ya quoi de prevu",
        "y a quelque chose de prevu", "y a un truc de prevu", "y a rien de prevu", "on a quoi de prevu",
        "c est quoi le programme", "quel est le programme", "le programme de ce soir", "le programme de la semaine",
        "qu est ce qui est prevu", "on joue quand", "quand est ce qu on joue", "quand est ce qu on fait une session",
        "quand est ce qu on refait une session", "on joue ce soir", "on joue demain", "on joue ce week end", "on joue ce weekend",
        "on fait une session", "qui vient ce soir", "qui vient a la session", "qui vient a la soiree", "qui est inscrit",
        "qui s est inscrit", "y a qui ce soir", "qui joue ce soir", "on sera combien", "on est combien a la session",
        "combien d inscrits", "la session de ce soir", "la session de demain", "la session de ce week end",
        "c est quand la session", "quand est la session", "la session c est quand", "c est a quelle heure la session",
        "la session commence quand", "la session est a quelle heure", "la session c est a quelle heure",
        "c est quand la soiree", "c est quand la prochaine", "la prochaine c est quand", "y a du monde ce soir", "ya session",
        "session ce soir", "c est prevu quand", "c est prevu pour quand", "on rejoue quand", "quand est ce qu on rejoue",
        "on joue quel jour", "y a un event", "y a un evenement", "qui sera la ce soir", "qui sera la a la session",
    };
    private static readonly HashSet<string> _nextSessionOpeners = new() { "quand", "qui", "y", "ya", "il", "c", "combien", "quel" };

    // « tu me notes ? », « je vaux combien ? », « tu me trouves comment ? »: /rate's verdict on them.
    // « tu penses quoi de moi ? » is already an Opinion on « moi », and gets the same verdict.
    private static readonly string[] _rateMePhrases =
    {
        "note moi", "notes moi", "tu me notes", "tu me noterais", "tu me mets combien", "tu me donnes combien",
        "tu me donnerais combien", "je vaux combien", "je merite combien", "rate me", "tu me trouves comment",
        "je suis a combien sur 10", "je suis combien sur 10", "sur 10 je suis a combien", "j ai quelle note",
        "tu me mettrais combien", "evalue moi", "juge moi", "tu me donnes quelle note", "je merite quelle note",
    };
    // « je suis beau ? », « tu me trouves drôle ? »: one of these, up to two _downIntensifiers, then an
    // adjective closing the message or before _rateMeFollowers. Asked only. « je suis nul » is Down,
    // read later: comfort answers it better than a score.
    private static readonly string[] _rateMeSubjects =
    {
        "je suis", "jsuis", "chui", "j suis", "tu me trouves", "tu me trouve", "t me trouves", "tu trouves que je suis",
    };
    private static readonly HashSet<string> _rateMeAdjectives = new()
    {
        "beau", "belle", "bo", "canon", "mignon", "mignonne", "moche", "drole", "marrant", "marrante", "intelligent",
        "intelligente", "malin", "maligne", "bg", "sexy", "sympa", "gentil", "gentille", "doue", "douee", "parfait",
        "parfaite", "genial", "geniale", "attachant", "attachante", "charmant", "charmante", "comment", "joli", "jolie",
        "style", "stylee", "chiant", "chiante", "relou", "bizarre", "normal", "normale", "fou", "folle", "cool", "fort",
        "forte",
    };
    // No « en » or « de »: « je suis fort en maths ? » and « je suis fou de joie » aren't asking for a score.
    private static readonly HashSet<string> _rateMeFollowers = new()
    {
        "syncs", "sync", "hein", "franchement", "vraiment", "toi", "ou", "pas", "quand", "meme", "alors", "non", "oui",
        "aujourd", "sur", "10", "physiquement", "comme", "personne",
    };

    // Thanks. A negator before cancels (« non merci »); the short forms count only as (nearly) the
    // whole reply, since « ty » and « thx » can sit in anything.
    private static readonly string[] _thanksPhrases =
    {
        "merci", "mercii", "merci beaucoup", "merci bien", "merci mille fois", "mille mercis", "grand merci",
        "je te remercie", "jte remercie", "je vous remercie", "cimer", "thanks", "thank you", "thank u", "tysm",
        "c est gentil", "c est trop gentil",
    };
    private static readonly string[] _thanksShortWords = { "ty", "thx", "mrc", "mci", "merki" };

    // Not buying what she said: « fais genre », « mon œil », « avoue ». Teasing more than
    // contradicting, so it gets its own pool — typically after one of her denials. In a reply of
    // MaxDisbeliefTokens at most (« il fait genre qu'il sait jouer » is about him), never after
    // « il », « elle », « on », and never « j'avoue » (« true »: YouAreRight). No « oui bien sûr »:
    // that one is meant.
    private static readonly string[] _disbeliefPhrases =
    {
        "fais genre", "fait genre", "c est ca ouais", "c est ca oui", "mais oui c est ca", "ouais c est ca",
        "oui oui c est ca", "mais bien sur", "mais oui bien sur", "on te croit", "on y croit", "je te crois pas",
        "j te crois pas", "jte crois pas", "je te crois moyen", "personne te croit", "mon oeil", "a d autres",
        "cause toujours", "ben voyons", "bah voyons", "et la marmotte", "pas credible", "t es pas credible",
        "avoue", "avoue le", "avoue tout", "allez avoue", "t es grillee", "tu es grillee", "on t a grillee",
        "je t ai grillee", "t es demasquee", "tu rougis", "t as rougi", "sure jan", "yeah right",
    };
    private static readonly HashSet<string> _notDisbeliefAfter = new() { "il", "elle", "on", "j", "je", "ca" };

    // Doubt, asked: « t'es sûre ? », « sérieux ? ». Only with a « ? », closing the message or its
    // question or before _doubtFollowers (IsClosingAsk): « t'es sûre que tu viens ? » asks about a plan.
    // « sérieux » only opening the message: « il est sérieux ? » is about him.
    private static readonly string[] _sureAsks =
    {
        "t es sure", "tu es sure", "t es sur", "tu es sur", "t es sure de toi", "tu es sure de toi", "t es sur de toi",
        "sure de toi", "t es sure de ca", "tu es sure de ca", "t es serieuse", "tu es serieuse", "t es serieux",
    };
    private static readonly HashSet<string> _seriousWords = new() { "serieux", "serieusement", "seriously", "srx" };
    // « ça m'étonnerait », « j'en doute »: closing a sentence or before _doubtFollowers, so « ça
    // m'étonnerait pas » agrees and « ça m'étonnerait qu'il vienne » is about him.
    private static readonly string[] _doubtPhrases =
    {
        "ca m etonnerait", "ca m etonnerais", "sa m etonnerait", "j en doute", "jen doute", "permets moi d en douter",
    };
    private static readonly HashSet<string> _doubtFollowers = new()
    {
        "syncs", "sync", "hein", "la", "quand", "meme", "fort", "beaucoup", "un", "peu", "toi",
    };

    // Calling her a liar: the same disbelief, said harder. « menteuse » is an insult cue too, but a
    // short accusation aimed at her is answered as teasing (ChatterService.IsMean). The phrases are
    // cancelled by a negator after them (« tu mens pas »), the single words by one before them
    // (« t'es pas une menteuse »); « il ment » is someone else (_notDisbeliefAfter).
    private static readonly string[] _liarPhrases =
    {
        "tu mens", "tu nous mens", "tu me mens", "tu mentais", "t as menti", "tu as menti", "ta menti",
        "arrete de mentir", "arretes de mentir", "arrete de mytho", "arrete de mythoner", "tu sais mentir",
        "tu mythones", "tu mythonnes", "tu mytho", "tu bluffes", "tu baratines", "tu racontes des bobards",
        "tu racontes des salades", "c est du mytho", "c est du pipeau", "c est du flan", "c est un mensonge",
        "sale menteuse", "sale mytho", "les bots mentent", "une bot qui ment",
    };
    private static readonly string[] _liarWords =
    {
        "menteuse", "menteur", "menteuses", "menteurs", "mytho", "mythomane", "mensonge", "mensonges",
        "bobard", "bobards", "pipeau", "baratin", "bluff", "liar",
    };

    // A bare liar word is about her only in a reply of two words at most (« menteuse ! ») or one
    // that speaks to her: « il est un menteur » is someone else.
    private static readonly HashSet<string> _secondPerson = new() { "t", "tu", "toi", "es", "te", "ta", "ton", "tes" };
    private const int MaxDisbeliefTokens = 6;

    // « ça va, merci », « bien merci »: the politeness closing an answer, not thanks for anything.
    private static readonly HashSet<string> _notThanksAfter = new() { "va", "vais", "bien", "roule", "tranquille" };
    private const int MaxShortThanksTokens = 3;

    // A goodbye is short: « à plus » closes « bon, à plus », but « il y a plus de place » is
    // something else, and so is any long message that happens to contain a farewell.
    private const int MaxFarewellTokens = 8;

    // Matched raw, shortened or squashed, so « mdrrrr » and « ptdrrr » land on their cue (squashed),
    // and so does « jpppp » (shortened to « jpp »: squashing would make it « jp »).
    private static readonly HashSet<string> _laughterWords = new()
    {
        "mdr", "ptdr", "pdtr", "xptdr", "lol", "lool", "lolilol", "xd", "jpp", "jptr", "jpptdr", "lmao", "lmfao",
        "rofl", "kek", "kekw", "lul", "lel", "omegalul", "niark",
    };

    // Phrases that laugh, or say she was funny: answered by the Laughter pool (« Merci, je suis
    // drôle »), which fits both better than a plain compliment. No bare « je pleure »: it cries as
    // often as it laughs.
    private static readonly string[] _laughterPhrases =
    {
        "mort de rire", "morte de rire", "je suis mort", "je suis morte", "jsuis mort", "jsuis morte", "je meurs",
        "je suis plie", "je suis pliee", "jsuis plie", "jsuis pliee", "plie de rire", "pliee de rire",
        "je pleure de rire", "j en pleure", "tu me tues", "tu m as tue", "ca m a tue",
        "trop drole", "t es drole", "t es trop drole", "c est drole", "tu es drole", "trop marrant",
        "t es marrante", "tu es marrante", "hilarant", "bonne blague", "elle est bonne celle la",
    };

    private static readonly string[] _laughterSymbols = { "😂", "🤣", "😆", "😹", "💀", "😄", "😁", "🤭" };

    // Custom emotes that laugh, matched by id on the raw text (ReadIntent drops emote markup before
    // tokenizing, so a message of only the emote still lands here).
    private static readonly string[] _laughterEmoteIds = { Emotes.GigaLaughId };

    // « haha », « ahah », « hihi », « héhé », « hoho », and the villain laughs « bahaha », « bwahaha »,
    // « mouahaha », « muahaha », « mwahaha », « gnahaha »: matched on the squashed token, so
    // « haaaaha » and « bahahaaa » count. Four letters at least (« bah » and « aha » don't).
    private static readonly Regex _laughRun = new(@"^(?:b|bw|bou|mou|mu|mw|gn)?[aeiu]?(?:ha|he|hi|hu|ho)+h?$", RegexOptions.Compiled);

    // A laugh is the whole message, or nearly: « mdr » answers her, « mdr il a raté » doesn't.
    private const int MaxLaughterTokens = 5;

    // A reply made only of these (three words at most) says nothing she could answer: « ok »,
    // « ah bon », « bah oui », « d'accord » (two tokens once the apostrophe goes).
    private static readonly HashSet<string> _lowEffortWords = new()
    {
        "ok", "okay", "okk", "oki", "okey", "oke", "k", "kk", "dac", "dacc", "daccord", "d", "accord",
        "ouais", "ouai", "ouaip", "ouep", "yep", "yes", "ui", "oui", "non", "nan", "nope", "mouais", "bof",
        "bref", "hm", "hmm", "mh", "ah", "oh", "eh", "bon", "bah", "ben", "euh", "si", "voila", "soit", "certes",
        "okie", "okidoki", "ouki", "acc", "yup", "yeah", "nah", "no", "we", "wai", "hein", "heu", "mm", "mmh",
        "pf", "pff", "ts", "tss", "tsk", "rah", "roh", "oula", "oulah", "olala", "ohlala", "la", "genre",
        "alors", "et", "donc", "enfin", "tiens", "tkt", "tqt", "np", "osef",
    };
    private const int MaxLowEffortTokens = 3;

    // A question typed without its « ? », as chat often does: one of these opening a message of
    // three words or more (« pourquoi t'as dit ça »), or one of _questionOpenerPhrases. No bare
    // « quel » (« quel plaisir » is an exclamation) and no bare « quand » (« quand tu veux »).
    private static readonly HashSet<string> _questionOpeners = new()
    {
        "pourquoi", "pq", "pk", "pkoi", "pourkoi", "pourquoa", "comment", "cmt", "koman", "koment", "combien",
        "estce", "kesk", "keske", "kesako", "kezako", "qui", "why", "how", "what", "who", "when", "where",
    };

    // « où » too, but not « ou alors » / « ou bien » / « ou pas », which go on a sentence rather
    // than ask anything.
    private static readonly HashSet<string> _notQuestionAfterOu = new() { "alors", "bien", "pas", "sinon", "meme", "plutot" };

    private static readonly string[] _questionOpenerPhrases =
    {
        "est ce", "quand est", "c est quand", "c est qui", "c est quoi", "c est ou", "c est combien", "c est comment",
        "quel est", "quelle est", "quels sont", "quelles sont", "quelle heure", "quel jour", "quel jeu",
        "tu sais si", "tu sais ou", "tu sais quand", "tu sais qui", "tu sais comment", "tu crois que", "tu penses que",
        "tu connais", "t as vu",
    };
    private const int MinUnmarkedQuestionTokens = 3;

    // Sharing: telling her about their own day (« j'ai mangé de la soupe », « bon, je suis allé au
    // ciné », « on a perdu la game »). A first-person statement, not a question, opening the message
    // once these fillers are skipped. Read after everything else bar LowEffort, so « j'ai faim »
    // reaches it but « je m'ennuie » (Bored) or « je suis d'accord » (YouAreRight) never do.
    private static readonly HashSet<string> _sharingFillers = new()
    {
        "bon", "bah", "ben", "alors", "moi", "perso", "du", "coup", "sinon", "au", "fait", "ah", "oh", "eh", "et",
        "mais", "ouais", "oui", "non", "nan", "bref", "enfin", "hier", "ce", "matin", "soir", "aujourd", "hui",
        "tiens", "btw", "d", "ailleurs", "la", "mdr", "lol", "ptdr", "wesh", "frr",
        "enfaite", "en", "vrai", "tantot", "omg", "ptn", "putain", "wow", "waw", "olala", "genre", "xd", "nous",
        "news", "info", "anecdote", "storytime", "cette", "nuit", "l", "autre", "jour", "semaine", "dernier",
        "derniere", "weekend", "week", "end", "midi", "tout", "a", "instant", "heure", "finalement", "franchement",
    };

    // Openers that announce news but would trip the checks below: « devine quoi, j'ai gagné » asks
    // nothing, and « tu sais quoi » isn't about « toi ». Skipped with the fillers.
    private static readonly string[] _sharingOpeners =
    {
        "devine quoi", "devinez quoi", "tu sais quoi", "vous savez quoi", "guess what", "breaking news",
        "petite info", "story time", "oh la la",
    };

    // « je », « j' », and the SMS forms glued to their verb.
    private static readonly HashSet<string> _sharingSelf = new()
    {
        "je", "j", "jai", "jsuis", "chui", "jsui", "jme", "jviens", "jvais", "jsors", "jrentre", "jpars", "jmange",
        "jdois", "jfais", "jreviens", "jjoue", "jregarde",
    };
    private static readonly HashSet<string> _sharingUsVerbs = new()
    {
        "a", "est", "s", "vient", "sort", "part", "rentre", "mange", "regarde", "etait", "avait",
    };
    // « nous avons », « nous sommes » — « nous » itself is a filler (« nous on a perdu »).
    private static readonly HashSet<string> _sharingWeVerbs = new() { "avons", "sommes", "etions", "avions" };

    // Their own things: « mon chat a vomi sur le clavier », « ma sœur m'a offert un jeu ». A possessive,
    // then one of these within three words.
    private static readonly HashSet<string> _sharingMine = new() { "mon", "ma", "mes" };
    private static readonly HashSet<string> _sharingMineVerbs = new()
    {
        "a", "est", "m", "ont", "sont", "vient", "viennent", "etait", "avait", "s",
    };
    private const int SharingMineWindow = 3;
    private static readonly HashSet<string> _notSharingMineNouns = new()
    {
        "avis", "opinion", "idee", "impression", "question", "reponse", "choix", "vote", "probleme",
    };

    // What comes after « je » when it isn't news: an opinion or a wish (« je pense », « je veux »), or
    // something said to the listener (« je t'envoie le lien », « je te rejoins »).
    private static readonly HashSet<string> _notSharingAfterSelf = new()
    {
        "sais", "pense", "crois", "trouve", "veux", "voudrais", "aimerais", "peux", "pourrais", "comprends",
        "t", "te", "vous", "ne", "prefere", "aime", "adore", "deteste", "kiffe", "arrive", "reviens", "y", "en",
    };
    // A message about « toi » or asking « quoi » isn't news about their day: « je suis pas amoureux de
    // toi » answers her, « on joue à quoi ce soir » proposes.
    private static readonly HashSet<string> _notSharingWords = new()
    {
        "toi", "tu", "t", "te", "ton", "ta", "tes", "quoi", "koi", "pourquoi", "comment", "quand", "combien",
    };
    private const int MinSharingTokens = 3;
    private const int MaxSharingTokens = 20;

    // Her name as an address, opening the message (after « dis », « hey »…) or closing it before its
    // punctuation. Not when it is the subject of a sentence about her (« syncs a raison », « syncs
    // est là ? »): those read on the name, through _thirdPersonAfter.
    private static readonly Regex _leadingAddress = new(
        @"^\s*(?:(?:dis|hey|eh|oh|yo|allez|bon|alors|ok|ah|wesh)[\s,]+)?syncs?(?![\w.])(?!\s+(?:"
        + string.Join("|", _thirdPersonAfter) + @")(?!\w))[\s,:!]*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex _trailingAddress = new(@"[\s,]+syncs?(?![\w.])(?=[\s?!.…]*$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // Quoted words, French or English quotes, and Discord's « > » quote lines.
    private static readonly Regex _quotedSpeech = new(@"«[^»]*»|“[^”]*”|""[^""]*""|^>.*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    // « a+ » / « @+ » is « à plus »: the tokenizer would keep only the « a ».
    private static readonly Regex _aPlus = new(@"(?<![\w<])[aà@]\s?\+(?!\w)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Mentions (« <@123> »), custom emotes and links are dropped before reading, or a mention
    // would tokenize into a number and count as a word.
    private static readonly Regex _mentionMarkup = new(@"<[@#][!&]?\d+>", RegexOptions.Compiled);

    /// <summary>
    /// What a message aimed at her is doing. First match wins, in the order of
    /// <see cref="ChatIntent"/>. A negator just after a declaration or a « ça va » cancels it
    /// (« je t'aime pas », « ça va pas la tête ? »), and just before an apology (« pas désolé »).
    /// </summary>
    public static ChatIntent ReadIntent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return ChatIntent.None;

        // Reported speech is someone else's words (« il a écrit « je t'aime » à sa copine »): quotes are
        // dropped when something is said outside them. A message that is only a quote keeps it.
        var unquoted = _quotedSpeech.Replace(content, " ");
        if (TokenizeOrdered(_mentionMarkup.Replace(unquoted, " ")).Count > 0) content = unquoted;

        var unaddressed = _aPlus.Replace(_link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " "), " a plus ");
        // « syncs ? » alone is checked on the name itself, before the address is dropped below.
        var nameAlone = TokenizeOrdered(unaddressed) is { Count: > 0 } named && named.All(w => _selfNames.Contains(Squash(w)));
        // Her name as an address (« syncs, organise une session », « merci syncs ») is dropped, so the
        // rules that look at the first or last word see the message itself.
        var stripped = _trailingAddress.Replace(_leadingAddress.Replace(unaddressed, " "), "");
        // Her name alone, unasked, with nothing else (not even « 😂 »), is her name said: the
        // talked-about line, not a reply to read.
        if (nameAlone && !unaddressed.Contains('?') && stripped.Trim(' ', ',', '.', '!', ':').Length == 0)
            return ChatIntent.None;
        var t = TokenizeOrdered(stripped);
        var asks = stripped.Contains('?');

        // « Pardon ? » alone is "what did you say", not an apology.
        if (t.Count == 1 && t[0] == "pardon" && asks) return ChatIntent.Question;

        if (FindPhrase(t, _lovePhrases, cancelAfter: true, cancelBefore: true)) return ChatIntent.Love;
        var breaks = QuestionBreaks(stripped);
        // Before the apology: « dis pardon » asks for one, it doesn't give one.
        if (FindPhrase(t, _demandApologyPhrases, cancelAfter: false, cancelBefore: true, notAfter: _notDemandAfter)
            || (asks && IsClosingAsk(t, _demandApologyAsked, breaks, _noWords)))
            return ChatIntent.DemandApology;
        if (FindPhrase(t, _apologyPhrases, cancelAfter: false, cancelBefore: true)
            || (t.Count <= MaxShortApologyTokens && FindPhrase(t, _apologyShortWords, cancelAfter: false, cancelBefore: false)))
            return ChatIntent.Apology;
        if (IsShutUp(t)) return ChatIntent.ShutUp;
        if (IsClosingAsk(t, _doYouLoveMePhrases, breaks, _lovesMeFollowers)) return ChatIntent.DoYouLoveMe;
        // « comment tu vas » needs no « ? » to be a question; « cv », « tranquille »… count opening a
        // short one (« cv et toi ? »).
        var isOpener = t.Count is > 0 and <= MaxHowAreYouOpenerTokens && _howAreYouOpeners.Contains(t[0])
                       && t.Skip(1).All(_askingFollowers.Contains);
        if ((asks || (t.Count > 0 && t[0] == "comment"))
            && (IsClosingAsk(t, _howAreYouPhrases, breaks, _askingFollowers) || isOpener))
            return ChatIntent.HowAreYou;
        // « t'es là ? », « réveille-toi », or her name alone with a « ? » (« syncs ? »).
        if (IsClosingAsk(t, _areYouTherePhrases, breaks, _thereFollowers)
            || (t.Count <= MaxWakeUpTokens && FindPhrase(t, _wakeUpPhrases, cancelAfter: false, cancelBefore: false))
            || (asks && nameAlone)
            || (t.Count > 0 && Squash(t[0]) == "alo" && t.Skip(1).All(_alloFollowers.Contains)))
            return ChatIntent.AreYouThere;
        if (FindPhrase(t, _whoAreYouPhrases, cancelAfter: false, cancelBefore: false)) return ChatIntent.WhoAreYou;
        if (IsClosingAsk(t, _rememberMePhrases, breaks, _rememberFollowers)
            || (asks && IsClosingAsk(t, _rememberMeAskedPhrases, breaks, _rememberFollowers)))
            return ChatIntent.RememberMe;
        // Theirs, not hers: « mon niveau ? » before AboutHer's « ton … préféré ».
        if (ReadMyStatsTopic(t, asks, breaks) is not null) return ChatIntent.MyStats;
        if (ReadHerTopic(t, breaks) is not null) return ChatIntent.AboutHer;
        if (FindPhrase(t, _areYouABotPhrases, cancelAfter: false, cancelBefore: false)) return ChatIntent.AreYouABot;
        if (asks && IsClosingAsk(t, _areYouUpsetPhrases, breaks, _upsetFollowers)) return ChatIntent.AreYouUpset;
        if (FindPhrase(t, _crushPhrases, cancelAfter: false, cancelBefore: false)
            || IsClosingAsk(t, _crushClosingPhrases, breaks, _crushFollowers))
            return ChatIntent.Crush;
        if (FindPhrase(t, _whatAreYouDoingPhrases, cancelAfter: false, cancelBefore: false)) return ChatIntent.WhatAreYouDoing;
        // On the raw text: « t'en penses quoi de @Luca ? » keeps its subject. Ship first: « tu penses
        // quoi de @A et @B ? » asks about a pair of people, not about the word « et ».
        if (ShipAsk.Read(content) is not null) return ChatIntent.Ship;
        // Before the opinion: « qui a raison, moi ou Luca ? » sides with one of two people.
        if (TakeSides.Read(content) is not null) return ChatIntent.TakeSides;
        if (Opinion.Read(content) is not null) return ChatIntent.Opinion;
        if (IsRateMe(t, asks, breaks)) return ChatIntent.RateMe;
        if (FindPhrase(t, _blamePhrases, cancelAfter: false, cancelBefore: false) && (asks || _blamePhrases.Any(p => EndsWith(t, p))))
            return ChatIntent.Blame;
        // Before ShouldI (« on joue ce soir ou pas ? ») and Request (« tu peux me dire c'est quand la
        // prochaine session ? »): the schedule answers both.
        if ((asks || (t.Count > 0 && _nextSessionOpeners.Contains(t[0])))
            && FindPhrase(t, _nextSessionPhrases, cancelAfter: false, cancelBefore: false))
            return ChatIntent.NextSession;
        if (IsShouldI(t, stripped, asks, breaks)) return ChatIntent.ShouldI;
        // Before the request: « tu peux lancer un dé ? » wants the roll.
        if (Dice.Read(stripped) is not null) return ChatIntent.Roll;
        // Before the request: « tu peux me raconter une blague ? » wants the joke, not a command.
        if (FindPhrase(t, _jokePhrases, cancelAfter: false, cancelBefore: true) || _jokeAlone.Contains(string.Join(' ', t)))
            return ChatIntent.Joke;
        if (FindPhrase(t, _funFactPhrases, cancelAfter: false, cancelBefore: true)
            || IsClosingAsk(t, _funFactClosingPhrases, breaks, _funFactFollowers))
            return ChatIntent.FunFact;
        // A request naming a topic is a request (« tu peux m'aider à lancer un sondage ? »); without
        // one, asking for help is Help (« tu peux m'aider ? »), and anything else asked is a request.
        var topic = ReadRequestTopic(t);
        if (topic is not null and not RequestTopic.Other) return ChatIntent.Request;
        if (FindPhrase(t, _helpPhrases, cancelAfter: true, cancelBefore: true)
            || (t.Count <= MaxHelpClosingTokens && _helpClosingPhrases.Any(p => EndsWith(t, p)))
            || (t.Any(_featureWords.Contains) && FindPhrase(t, _howToPhrases, cancelAfter: false, cancelBefore: true)))
            return ChatIntent.Help;
        if (topic is not null) return ChatIntent.Request;
        var sentenceBreaks = SentenceBreaks(stripped);
        // Before boredom and the farewells: « journée de merde, je vais dormir » is the day, said.
        if (IsDown(t, stripped, content, asks, sentenceBreaks)) return ChatIntent.Down;
        if (t.Count <= MaxBoredTokens
            && (FindPhrase(t, _boredPhrases, cancelAfter: true, cancelBefore: true, notFollowedBy: _missingWords)
                || IsClosingAsk(t, _nothingToDoPhrases, sentenceBreaks, _nothingToDoFollowers)
                || IsClosingAsk(t, _boredClosingPhrases, breaks, _noWords)))
            return ChatIntent.Bored;
        if (t.Count <= MaxFarewellTokens)
        {
            if (FindPhrase(t, _goodnightPhrases, cancelAfter: false, cancelBefore: false, notAfter: _notGoodnightAfter)
                || IsClosingAsk(t, _sleepPhrases, sentenceBreaks, _sleepFollowers))
                return ChatIntent.Goodnight;
            if (IsShortFarewell(t) || FindPhrase(t, _goodbyePhrases, cancelAfter: false, cancelBefore: false)
                || _closingGoodbyePhrases.Any(p => EndsWith(t, p)))
                return ChatIntent.Goodbye;
        }

        // Before agreement and disagreement: « mais oui c'est ça » agrees with nothing, and « mais
        // non, fais genre » is teasing, not a correction.
        if (t.Count <= MaxDisbeliefTokens
            && (FindPhrase(t, _disbeliefPhrases, cancelAfter: false, cancelBefore: false, notAfter: _notDisbeliefAfter)
                || FindPhrase(t, _liarPhrases, cancelAfter: true, cancelBefore: false, notAfter: _notDisbeliefAfter)
                || ((t.Count <= 2 || t.Any(_secondPerson.Contains))
                    && FindPhrase(t, _liarWords, cancelAfter: false, cancelBefore: true))
                || (asks && IsClosingAsk(t, _sureAsks, breaks, _doubtFollowers))
                || (asks && t.Count > 0 && _seriousWords.Contains(Squash(t[0])) && t.Skip(1).All(w => _doubtFollowers.Contains(w) || w == "quoi"))
                || IsClosingAsk(t, _doubtPhrases, sentenceBreaks, _doubtFollowers)))
            return ChatIntent.Disbelief;

        if (t.Count <= MaxMaybeTokens
            && (FindPhrase(t, _maybePhrases, cancelAfter: false, cancelBefore: false)
                || FindPhrase(t, _maybeNegatablePhrases, cancelAfter: false, cancelBefore: true)))
            return ChatIntent.Maybe;

        if ((t.Count <= MaxDisagreementTokens && FindPhrase(t, _youAreRightPhrases, cancelAfter: false, cancelBefore: false))
            || (!asks && t.Count <= MaxDisagreementTokens && FindPhrase(t, _cestVrai, cancelAfter: false, cancelBefore: false))
            || (!asks && t.Count <= MaxAgreementTokens
                && t.Any(w => _youAreRightWords.Contains(w) || _youAreRightWords.Contains(Squash(w)))))
            return ChatIntent.YouAreRight;

        if (t.Count <= MaxDisagreementTokens
            && (FindPhrase(t, _youAreWrongPhrases, cancelAfter: false, cancelBefore: false)
                || (t.Count <= MaxShortDisagreementTokens && FindPhrase(t, _youAreWrongShortPhrases, cancelAfter: false, cancelBefore: false))
                || (t.Count <= MaxAgreementTokens && FindPhrase(t, _youAreWrongWords, cancelAfter: false, cancelBefore: true))))
            return ChatIntent.YouAreWrong;

        if (FindPhrase(t, _thanksPhrases, cancelAfter: false, cancelBefore: true, notAfter: _notThanksAfter)
            || (t.Count <= MaxShortThanksTokens && FindPhrase(t, _thanksShortWords, cancelAfter: false, cancelBefore: false)))
            return ChatIntent.Thanks;

        if (t.Count <= MaxLaughterTokens && IsLaughing(t, stripped, content)) return ChatIntent.Laughter;

        if (asks && t.Count >= 2) return ChatIntent.Question;
        if (t.Count >= MinUnmarkedQuestionTokens
            && (_questionOpeners.Contains(t[0])
                || (t[0] == "ou" && !_notQuestionAfterOu.Contains(t[1]))
                || _questionOpenerPhrases.Any(p => PhraseStartsAt(t, 0, p))))
            return ChatIntent.Question;

        if (!asks && t.Count is >= MinSharingTokens and <= MaxSharingTokens && IsSharing(t)) return ChatIntent.Sharing;

        if (t.Count == 0 || (t.Count <= MaxLowEffortTokens
                             && t.All(w => _lowEffortWords.Contains(w) || _lowEffortWords.Contains(Squash(w)))))
            return ChatIntent.LowEffort;

        return ChatIntent.None;
    }

    /// <summary>
    /// <see cref="ChatIntent.Yes"/> or <see cref="ChatIntent.No"/> when the whole message is a plain
    /// answer (« oui », « bah non », « carrément », « pas du tout »), else null. Never asked
    /// (« oui ? » is someone answering a call), and never mixed (« oui et non », « oui mais demain »).
    /// The caller decides whether there was a question to answer.
    /// </summary>
    public static ChatIntent? ReadAnswer(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var stripped = _trailingAddress.Replace(
            _leadingAddress.Replace(_link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " "), " "), "");
        if (stripped.Contains('?')) return null;

        var t = TokenizeOrdered(stripped);
        if (t.Count is 0 or > MaxAnswerTokens) return null;
        var words = new List<string>();
        foreach (var w in t.Select(Squash))
            if (!_answerFillers.Contains(w) && (words.Count == 0 || words[^1] != w)) words.Add(w);

        var said = string.Join(' ', words);
        if (_yesAnswers.Contains(said)) return ChatIntent.Yes;
        if (_noAnswers.Contains(said)) return ChatIntent.No;
        return null;
    }

    // A laugh anywhere: a laughter word or run (« mdr », « hahaha »), a phrase, an emoji, GigaLaugh.
    private static bool IsLaughing(List<string> t, string stripped, string content) =>
        t.Any(w => _laughterWords.Contains(w) || _laughterWords.Contains(Shorten(w))
                   || _laughterWords.Contains(Squash(w))
                   || (Squash(w).Length >= 4 && _laughRun.IsMatch(Squash(w))))
        || FindPhrase(t, _laughterPhrases, cancelAfter: false, cancelBefore: true)
        || _laughterSymbols.Any(stripped.Contains)
        || _laughterEmoteIds.Any(content.Contains);

    // Feeling down, about themselves (see _downSubjects and the lists after it). Not while laughing.
    private static bool IsDown(List<string> t, string stripped, string content, bool asks, HashSet<int> sentenceBreaks)
    {
        if (IsLaughing(t, stripped, content) || t.Contains("rire")) return false;
        if (FindPhrase(t, _downPhrases, cancelAfter: false, cancelBefore: true)) return true;
        if (!asks && (FindPhrase(t, _downDayPhrases, cancelAfter: false, cancelBefore: true)
                      || IsClosingAsk(t, _downClosingPhrases, sentenceBreaks, _downFollowers)))
            return true;

        foreach (var subject in _downSubjects)
        {
            var length = subject.Split(' ').Length;
            for (var i = 0; i + length < t.Count; i++)
            {
                if (!PhraseStartsAt(t, i, subject)) continue;
                var j = i + length;
                var skipped = 0;
                while (j < t.Count && skipped < 2 && _downIntensifiers.Contains(t[j])) (j, skipped) = (j + 1, skipped + 1);
                if (j < t.Count && _downAdjectives.Contains(t[j]) && IsDownAdjective(t, j)) return true;
            }
        }

        return false;
    }

    // Words that only read as feeling down closing the message or before one of _downAdjectiveFollowers:
    // « je me sens mal », not « je suis mal placé »; « je suis seul », not « je suis seul en vocal ».
    // « bout » and « plat » only after « à » / « au » (« je suis à bout », « à plat »).
    private static readonly HashSet<string> _closingDownAdjectives = new() { "mal", "seul", "seule", "vide" };
    private static readonly HashSet<string> _downAdjectiveFollowers = new()
    {
        "aujourd", "syncs", "sync", "hein", "depuis", "moi", "ce", "en", "la",
    };

    private static bool IsDownAdjective(List<string> t, int j)
    {
        if (t[j] is "bout" or "plat") return j > 0 && t[j - 1] is "a" or "au";
        if (!_closingDownAdjectives.Contains(t[j])) return true;
        if (j + 1 == t.Count) return true;
        // « en » and « la » only as « en ce moment » / « là », not « seul en vocal ».
        var next = t[j + 1];
        if (next == "en") return j + 2 < t.Count && t[j + 2] == "ce";
        if (next == "la") return j + 2 == t.Count;
        return _downAdjectiveFollowers.Contains(next);
    }

    /// <summary>
    /// The two options of a « X ou Y ? » asked with nothing else (« Valo ou LoL ? », « je joue ou je
    /// dors ? », « pile ou face »), or null. Each side is one to four words; « … ou pas ? », « … ou
    /// quoi ? » are not options, and a side opening on a question word or on « tu » asks something
    /// else (« c'est vrai ou faux ? », « tu dors ou quoi ? »).
    /// </summary>
    public static (string A, string B)? ReadChoice(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var text = Opinion.TrimTail(_leadingAddress.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " ")).Trim();
        var match = _choiceRegex.Match(text);
        if (!match.Success) return null;
        if (!text.EndsWith('?') && !string.Equals(text.Trim('.', '!', ' '), "pile ou face", StringComparison.OrdinalIgnoreCase))
            return null;

        var (a, b) = (match.Groups["a"].Value.Trim(), match.Groups["b"].Value.Trim());
        var (ta, tb) = (TokenizeOrdered(a), TokenizeOrdered(b));
        if (ta.Count is 0 or > MaxChoiceSideTokens || tb.Count is 0 or > MaxChoiceSideTokens) return null;
        if (_notChoiceStart.Contains(ta[0]) || _notChoiceEnd.Contains(tb[0])) return null;
        return (a, b);
    }

    // The side's words, the « ou » between, and a closing « ? » (or « pile ou face » as said).
    private static readonly Regex _choiceRegex = new(
        @"^(?:(?:alors|bon|du coup|bref|dis|dis moi|dis-moi)[\s,]+)?(?<a>[^?,;.!:]+?)\s+ou\s+(?<b>[^?,;.!:]+?)[\s?!.]*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private const int MaxChoiceSideTokens = 4;
    private static readonly HashSet<string> _notChoiceStart = new()
    {
        "qui", "quoi", "quel", "quelle", "quels", "quelles", "comment", "pourquoi", "ou", "quand", "combien", "est",
        "tu", "t", "vous", "ca", "c", "il", "elle", "ils", "elles", "mais", "et", "sinon",
    };
    private static readonly HashSet<string> _notChoiceEnd = new()
    {
        "pas", "non", "quoi", "bien", "alors", "sinon", "plus", "meme", "ptet", "autre", "koi", "comment", "rien",
    };

    // « je devrais y aller ? », « j'y vais ou pas ? », « Valo ou LoL ? », « pile ou face ».
    private static bool IsShouldI(List<string> t, string stripped, bool asks, HashSet<int> breaks)
    {
        if (ReadChoice(stripped) is not null) return true;
        if (!asks || t.Any(_openQuestionWords.Contains)) return false;
        if (FindPhrase(t, _shouldIPhrases, cancelAfter: false, cancelBefore: false)) return true;
        // « … ou pas ? » closing a question that is about the asker.
        return t.Count is >= 3 and <= 10 && t.Any(_firstPerson.Contains)
            && (EndsWith(t, "ou pas") || breaks.Any(b => b >= 2 && t[b - 2] == "ou" && t[b - 1] == "pas"));
    }

    // « tais-toi » in a short message, or « chut » / « silence » / « la ferme » as (nearly) all of it.
    private static bool IsShutUp(List<string> t)
    {
        if (t.Count is 0 or > MaxShutUpTokens) return false;
        if (FindPhrase(t, _shutUpPhrases, cancelAfter: false, cancelBefore: true)) return true;
        if (IsClosingAsk(t, _shutUpClosing, _noBreaks, _shutUpClosingFollowers)) return true;
        var said = string.Join(' ', t.Where(w => !_shutUpFillers.Contains(w)).Select(Squash));
        return _shutUpAlone.Contains(said) || _shutUpAlone.Contains(string.Join(' ', t.Select(Squash)));
    }

    /// <summary>What a MyStats question asks about (level, cailloux, Plynling, everything), or null.</summary>
    public static MyStatsTopic? ReadMyStatsTopic(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var text = _link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " ");
        return ReadMyStatsTopic(TokenizeOrdered(text), text.Contains('?'), QuestionBreaks(text));
    }

    private static MyStatsTopic? ReadMyStatsTopic(List<string> t, bool asks, HashSet<int> breaks)
    {
        foreach (var (topic, phrases) in _myStatsPhrases)
            foreach (var phrase in phrases)
                if ((asks || phrase.Split(' ').Any(_myStatsAskWords.Contains))
                    && FindPhrase(t, [phrase], cancelAfter: false, cancelBefore: false))
                    return topic;

        if (!asks || t.Count > MaxMyStatsShortTokens) return null;
        foreach (var (topic, phrases) in _myStatsShortAsks)
            if (IsClosingAsk(t, phrases, breaks, _myStatsFollowers)) return topic;
        return null;
    }

    // « tu me notes ? », or « je suis beau ? » / « tu me trouves drôle ? » asked with the adjective closing.
    private static bool IsRateMe(List<string> t, bool asks, HashSet<int> breaks)
    {
        if (FindPhrase(t, _rateMePhrases, cancelAfter: false, cancelBefore: true)) return true;
        if (!asks) return false;

        foreach (var subject in _rateMeSubjects)
        {
            var length = subject.Split(' ').Length;
            for (var i = 0; i + length < t.Count; i++)
            {
                if (!PhraseStartsAt(t, i, subject)) continue;
                var j = i + length;
                var skipped = 0;
                while (j < t.Count && skipped < 2 && _downIntensifiers.Contains(t[j])) (j, skipped) = (j + 1, skipped + 1);
                if (j >= t.Count || !_rateMeAdjectives.Contains(Squash(t[j])) && !_rateMeAdjectives.Contains(t[j])) continue;
                var end = j + 1;
                if (end == t.Count || breaks.Contains(end) || _rateMeFollowers.Contains(t[end])) return true;
                // « en vrai » only: « je suis beau en vrai ? » asks, « je suis fort en maths ? » doesn't.
                if (t[end] == "en" && end + 1 < t.Count && t[end + 1] == "vrai") return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A call and nothing else: her name or tag with at most an interjection and punctuation (« syncs ? »,
    /// « hé syncs », a bare tag). What comes next from that person is for her (ChatListening). Shared
    /// with her Minecraft body's call (McTalkReader.IsBareCall), mentions and emotes dropped first.
    /// </summary>
    public static bool IsBareCall(string content)
    {
        if (content is null) return false;
        var text = _link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " ");
        return McTalkReader.IsBareCall(text);
    }

    /// <summary>
    /// Whether the message speaks to someone (« tu », « toi », « t'es »…). A sad message that does is
    /// not let through a mean reading: « j'en ai marre de toi, t'es nulle » is an insult.
    /// </summary>
    public static bool SpeaksToSomeone(string content) =>
        !string.IsNullOrWhiteSpace(content) && TokenizeOrdered(_mentionMarkup.Replace(content, " ")).Any(_secondPerson.Contains);

    /// <summary>
    /// What a Request asks her to do, or null when the message isn't one. A request marker
    /// (« tu peux », « stp »…) not cancelled by a negator (« tu peux pas »), or an imperative
    /// opening the message; then the first topic it names. A marker without a topic is
    /// <see cref="RequestTopic.Other"/>; an imperative without one (« fais gaffe ») is no request.
    /// </summary>
    public static RequestTopic? ReadRequestTopic(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        return ReadRequestTopic(TokenizeOrdered(_link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " ")));
    }

    private static RequestTopic? ReadRequestTopic(List<string> tokens)
    {
        var marker = FindPhrase(tokens, _requestMarkers, cancelAfter: true, cancelBefore: false, notAfter: _notRequestAfter);
        var imperative = tokens.Count > 0 && _requestImperatives.Contains(tokens[0]);
        if (!marker && !imperative) return null;

        var topic = FindTopic(tokens);
        return topic != RequestTopic.Other ? topic : marker ? RequestTopic.Other : null;
    }

    /// <summary>
    /// The command topic a message names, request or not (« comment ça marche les sondages ? » is
    /// Poll), or <see cref="RequestTopic.Other"/>: what a Help answer points at.
    /// </summary>
    public static RequestTopic ReadTopic(string content) =>
        string.IsNullOrWhiteSpace(content)
            ? RequestTopic.Other
            : FindTopic(TokenizeOrdered(_link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " ")));

    private static RequestTopic FindTopic(List<string> tokens)
    {
        foreach (var (topic, words) in _requestTopics)
            if (FindPhrase(tokens, words, cancelAfter: false, cancelBefore: false)) return topic;
        return RequestTopic.Other;
    }

    /// <summary>What an AboutHer question asks (age, birthday, home, favourites, version), or null.</summary>
    public static HerTopic? ReadHerTopic(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var text = _link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " ");
        return ReadHerTopic(TokenizeOrdered(text), QuestionBreaks(text));
    }

    private static HerTopic? ReadHerTopic(List<string> tokens, HashSet<int> questionBreaks)
    {
        if (IsClosingAsk(tokens, _versionPhrases, questionBreaks, _versionFollowers)) return HerTopic.Version;
        foreach (var (topic, phrases) in _aboutHerPhrases)
            if (FindPhrase(tokens, phrases, cancelAfter: false, cancelBefore: false)) return topic;

        // « ton jeu préféré », « c'est quoi ta couleur favorite ».
        for (var i = 0; i < tokens.Count; i++)
        {
            if (!_yourWords.Contains(tokens[i])) continue;
            for (var j = i + 1; j <= Math.Min(tokens.Count - 1, i + FavouriteWindow); j++)
                if (_favouriteWords.Contains(tokens[j])) return HerTopic.Favourite;
        }

        return null;
    }

    // « à plus » / « à toute » opening the message, or closing it after a word that isn't « y »,
    // « en », « il »…
    private static bool IsShortFarewell(List<string> tokens)
    {
        if (tokens.Count < 2) return false;
        if (tokens[0] == "a" && _shortFarewells.Contains(tokens[1])) return true;

        var i = tokens.Count - 2;
        return tokens[i] == "a" && _shortFarewells.Contains(tokens[i + 1])
               && (i == 0 || !_notFarewellBefore.Contains(tokens[i - 1]));
    }

    // A first-person statement about their own life, once the opening fillers are skipped: « je »
    // or « j' » with something after it that isn't an opinion, a wish or « te » (_notSharingAfterSelf),
    // or « on » with a verb of what people do (« on a perdu », « on est sortis »).
    private static bool IsSharing(List<string> tokens)
    {
        // Fillers and openers first, so « tu sais quoi, j'ai gagné » is judged on « j'ai gagné ».
        var k = 0;
        while (k < tokens.Count)
        {
            if (_sharingFillers.Contains(tokens[k])) { k++; continue; }
            var opener = _sharingOpeners.FirstOrDefault(p => PhraseStartsAt(tokens, k, p));
            if (opener is null) break;
            k += opener.Split(' ').Length;
        }
        if (k + 1 >= tokens.Count) return false;
        if (tokens.Skip(k).Any(_notSharingWords.Contains)) return false;

        var (first, next) = (tokens[k], tokens[k + 1]);
        if (_sharingSelf.Contains(first)) return !_notSharingAfterSelf.Contains(next);
        // « on s'est fait battre » tells; « on s'appelle plus tard » proposes.
        if (first == "on" && next == "s") return k + 2 < tokens.Count && tokens[k + 2] == "est";
        if (first == "on") return _sharingUsVerbs.Contains(next);
        if (_sharingWeVerbs.Contains(first)) return true;
        // « mon avis c'est que… » is an opinion, and « c'est » describes rather than tells.
        if (!_sharingMine.Contains(first) || _notSharingMineNouns.Contains(next)) return false;
        for (var j = k + 1; j < Math.Min(tokens.Count, k + 1 + SharingMineWindow); j++)
            if (_sharingMineVerbs.Contains(tokens[j]) && tokens[j - 1] != "c") return true;
        return false;
    }

    // One of the phrases asking something of her: closing the message or its own question (« salut,
    // ça va ? tu viens ce soir ? »), or followed by one of the followers (« toi », « bien »…) — never
    // by anything else, a negator included (« ça va pas la tête », « comment tu vas faire », « t'es là
    // ce soir ? »).
    private static bool IsClosingAsk(List<string> tokens, string[] phrases, HashSet<int> questionBreaks,
        HashSet<string> followers)
    {
        foreach (var phrase in phrases)
        {
            var length = phrase.Split(' ').Length;
            for (var i = 0; i + length <= tokens.Count; i++)
            {
                if (!PhraseStartsAt(tokens, i, phrase)) continue;
                var end = i + length;
                if (end == tokens.Count || questionBreaks.Contains(end) || followers.Contains(tokens[end])) return true;
            }
        }

        return false;
    }

    // The token indexes a « ? » sits just before: « ça va ? tu viens » gives { 2 }. The « ? » is a
    // separator to TokenizeOrdered, so tokenizing each piece and counting lands on the same tokens.
    private static HashSet<int> QuestionBreaks(string text) => Breaks(text, '?');

    // The same for any end of sentence or clause: « je vais dormir, à demain » gives { 3 }.
    private static HashSet<int> SentenceBreaks(string text) => Breaks(text, '?', '!', '.', ',', ';', '…', '\n');

    private static HashSet<int> Breaks(string text, params char[] separators)
    {
        var breaks = new HashSet<int>();
        var count = 0;
        var pieces = text.Split(separators);
        for (var p = 0; p < pieces.Length - 1; p++)
        {
            count += TokenizeOrdered(pieces[p]).Count;
            breaks.Add(count);
        }

        return breaks;
    }

    // Whether the phrase closes the message.
    private static bool EndsWith(List<string> tokens, string phrase)
    {
        var length = phrase.Split(' ').Length;
        return tokens.Count >= length && PhraseStartsAt(tokens, tokens.Count - length, phrase);
    }

    // One of the phrases on adjacent tokens, not cancelled by a negator within two tokens after
    // it (cancelAfter) or three before it (cancelBefore). « plus que » after a phrase is the
    // comparative, not a negation: « je t'aime plus que tout ». A phrase right after a word of
    // notAfter, or right before a word of notFollowedBy, doesn't count either.
    private static bool FindPhrase(List<string> tokens, string[] phrases, bool cancelAfter, bool cancelBefore,
        HashSet<string>? notAfter = null, HashSet<string>? notFollowedBy = null)
    {
        foreach (var phrase in phrases)
        {
            var length = phrase.Split(' ').Length;
            for (var i = 0; i + length <= tokens.Count; i++)
            {
                if (!PhraseStartsAt(tokens, i, phrase)) continue;
                if (notAfter is not null && i > 0 && notAfter.Contains(tokens[i - 1])) continue;
                if (notFollowedBy is not null && i + length < tokens.Count && notFollowedBy.Contains(tokens[i + length])) continue;

                var end = i + length;
                var negatedAfter = cancelAfter && Enumerable.Range(end, Math.Min(ForwardNegationWindow, tokens.Count - end))
                    .Any(j => IsNegator(tokens[j]) && !(tokens[j] == "plus" && j + 1 < tokens.Count && tokens[j + 1] == "que"));
                var negatedBefore = cancelBefore && Enumerable.Range(Math.Max(0, i - NegationWindow), i - Math.Max(0, i - NegationWindow))
                    .Any(j => IsNegator(tokens[j]));
                if (!negatedAfter && !negatedBefore) return true;
            }
        }

        return false;
    }

    // True when the message calls the bot by the wrong name "Inabot" — written as
    // one word ("inabot"), split in two ("ina bot"), or spelled out letter by
    // letter ("I.N.A.B.O.T", "I N A B O T", "i-n-a-b-o-t"). The bot is SYNCS, and
    // it takes offence at being mistaken for Inabot.
    public static bool IsMistakenIdentity(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;

        var words = TokenizeOrdered(content);
        // A spelled-out name tokenizes into isolated letters (the dots and dashes
        // are dropped as separators), so try again with those runs glued back
        // together. Gluing only runs of single letters keeps innocent text safe:
        // "domina bottes" has no such run, and "in a bot" starts with a two-letter
        // token, so neither collapses into the name.
        //
        // Each form is also tried with repeated letters squashed, so drawing the
        // name out ("innnabot") doesn't smuggle it past her. "nabot" — which is a
        // real word, and one of Amandine's comebacks — squashes to itself and stays
        // safe, since the test needs "ina" before "bot".
        var squashed = words.Select(Squash).ToList();

        return NamesInabot(words) || NamesInabot(GlueSpelledOutRuns(words))
            || NamesInabot(squashed) || NamesInabot(GlueSpelledOutRuns(squashed));
    }

    // The name test itself: one word, or "ina" somewhere before "bot".
    private static bool NamesInabot(List<string> words)
    {
        if (words.Contains("inabot")) return true;
        int ina = words.IndexOf("ina");
        return ina >= 0 && words.IndexOf("bot") > ina;
    }

    // Merges every run of consecutive single-letter tokens into one word,
    // preserving the order of everything else: ["i","n","a","bot"] -> ["ina","bot"].
    private static List<string> GlueSpelledOutRuns(List<string> words)
    {
        var glued = new List<string>(words.Count);
        var run = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length == 1)
            {
                run.Append(word);
                continue;
            }

            if (run.Length > 0)
            {
                glued.Add(run.ToString());
                run.Clear();
            }
            glued.Add(word);
        }

        if (run.Length > 0) glued.Add(run.ToString());
        return glued;
    }

    // Splits text into an ordered list of lowercase, accent-stripped words,
    // preserving position for cues that depend on word order. The ligatures are spelled out, since
    // they have no decomposition: « cœur » reads as the cue "coeur".
    private static List<string> TokenizeOrdered(string content)
    {
        var sb = new StringBuilder(content.Length);
        foreach (var ch in content.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            if (ch == 'œ') { sb.Append("oe"); continue; }
            if (ch == 'æ') { sb.Append("ae"); continue; }
            sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }
        return sb.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }
}
