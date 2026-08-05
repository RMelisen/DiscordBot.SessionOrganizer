using System.Globalization;
using System.Text;

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
    // ID so a rename of the emote won't break detection.
    private const ulong GreetingEmoteId = 1482305105276571774;

    // Custom emotes that read as affection on their own, matched by ID for the same
    // reason as the greeting one: a message body carries the full <:name:id> markup,
    // so the ID is present verbatim and survives any rename on the server.
    private static readonly ulong[] _niceEmoteIds =
    {
        982024259918499870,  // mcheart — pixelated heart
        982024469956669501,  // cathearte_
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
        "adorable", "adorables", "adorbs", "adore", "adores", "aime", "amazing", "awesome",
        // No "boss" and no "monstre", despite both being compliments in French chat
        // ("t'es un monstre"). This server organises *gaming* sessions, where they
        // overwhelmingly mean the enemy — "il est fort ce boss" was reading as praise.
        "banger", "beau", "bebou", "belle", "best", "bg", "bienveillant", "bienveillante", "bisous", "bravissimo", "bravo", "brillant", "brillante",
        "calin", "calins", "carre", "chaleureuse", "chaleureux", "champion", "championne", "chapeau", "chou", "choupi", "choupinou", "classe", "coeur", "content", "contente", "cool", "courage", "cracke", "crackee", "craquant", "craquante", "cute",
        "dingue", "divin", "divine", "douce", "doue", "douee", "douees", "doues", "doux",
        "epique", "excellent", "excellente", "exceptionnel", "exceptionnelle", "extraordinaire",
        "fantastique", "felicitation", "felicitations", "fier", "fiere", "formidable", "fort", "forte",
        "genial", "geniale", "geniales", "geniaux", "genie", "gentil", "gentille", "gentilles", "gentils", "gg", "gj", "goat", "goated", "great",
        "heroine", "heros", "heureuse", "heureux",
        "iconique", "impec", "impeccable", "incroyable", "incroyables", "insane", "intelligent", "intelligente",
        "joli", "jolie",
        "kiff", "kiffe", "kiffer", "king",
        "legendaire", "legende", "love", "lovely",
        "magnifique", "magnifiques", "meilleur", "meilleure", "meilleures", "meilleurs", "merci", "mercii", "merciii", "merveilleuse", "merveilleux", "mignon", "mignonne", "mignonnes", "mignons", "mrc", "mvp",
        "nice", "nickel",
        "ouah", "ouf",
        "parfait", "parfaite", "parfaites", "parfaits", "perle", "precieuse", "precieux", "pro", "propre",
        "queen",
        "ravi", "ravie", "reine", "respect", "roi", "royal", "royale",
        "sauveur", "sauveuse", "slay", "solide", "splendide", "style", "stylee", "sublime", "super", "superbe", "sympa",
        "talentueuse", "talentueux", "thanks", "thx", "top", "tresor", "trognon", "tuerie", "ty",
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
        "casse", "cassos", "cheh", "chelou", "chiant", "chiante", "chiantes", "chiants", "claque", "claquee", "clown", "clowns", "consternant", "consternante", "cretin", "cretine", "cretines", "cretins", "cringe",
        "debile", "debiles", "decevant", "decevante", "degage", "degueu", "degueulasse", "deplorable", "deplorables", "detestable", "deteste",
        "eclate", "eclatee", "ennuyeuse", "ennuyeux", "execrable",
        "fade", "ferme", "foireuse", "foireux",
        "gogol", "gonflant", "gonflante", "grotesque", "grotesques", "gueguerre", "guignol", "guignols",
        "hais", "horrible", "horribles",
        "idiot", "idiote", "idiotes", "idiots", "imbecile", "imbeciles", "immonde", "immondes", "incompetent", "incompetente", "insipide", "insolent", "insolente", "insupportable", "insupportables", "inutile", "inutiles",
        "laid", "laide", "laides", "laids", "lamentable", "lamentables", "loser", "loupe", "loupee",
        "manchot", "manchote", "mauvais", "mauvaise", "mediocre", "mediocres", "merde", "merdique", "merdiques", "minable", "minables", "moche", "moches",
        "navrant", "navrante", "nawak", "naze", "nazes", "noob", "noobs", "nul", "nullard", "nullarde", "nulle", "nulles", "nullissime", "nullos", "nuls",
        "pathetique", "pathetiques", "penible", "penibles", "pitoyable", "pitoyables", "pouilleuse", "pouilleux", "pourri", "pourrie", "pourries", "pourris", "pretentieuse", "pretentieux",
        "quoka", "quokka",
        "raclure", "rate", "ratee", "ratees", "rates", "relou", "reloue", "relous", "ridicule", "ridicules", "risible", "risibles",
        "salaud", "saoulant", "saoulante", "saoule", "saoules", "soulant", "soulante", "soule", "soulent", "soules", "stupide", "stupides",
        "tais", "teube", "tocard", "tocarde", "toxique", "toxiques", "trash",
        "useless",
        "vilain", "vilaine",
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
        "rien a foutre",
        "sers a rien",
        "va chier", "va crever", "va t faire", "va te faire",
    };

    private static readonly string[] _nicePhrases =
    {
        "bien joue", "bien ouej", "bien vise", "bien vu",
        "courage a toi",
        "force a toi",
        "lache pas",
        "tiens bon", "trop bien", "trop fort", "tu gere", "tu geres",
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
                       || content.Contains(GreetingEmoteId.ToString());

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
    private static double ScoreEmoteIds(string content, ulong[] ids, double weight) =>
        Math.Min(ids.Count(id => content.Contains(id.ToString())), 2) * weight;

    // How emphatic the message is, regardless of what it says: shouting, drawn-out
    // letters, exclamation marks. Only ever added to a side that already scored.
    private static double Emphasis(string content)
    {
        double bonus = 0;

        var letters = content.Count(char.IsLetter);
        if (letters >= 4)
        {
            var upper = content.Count(char.IsUpper);
            if (upper / (double)letters > 0.6) bonus += CapsBonus;
        }

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
    public static FeedbackKind ReadFeedback(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return FeedbackKind.None;

        var tokens = TokenizeOrdered(content).Select(Shorten).ToList();
        if (tokens.Count == 0) return FeedbackKind.None;

        var joined = " " + string.Join(' ', tokens) + " ";
        var squashed = " " + string.Join(' ', tokens.Select(Squash)) + " ";

        // Bad wins a tie, the same way Mean does: someone who says both has
        // landed on a complaint.
        if (SaysVerdict(tokens, joined, squashed, _badBotPhrases, _badBotWords)) return FeedbackKind.Bad;
        if (SaysVerdict(tokens, joined, squashed, _goodBotPhrases, _goodBotWords)) return FeedbackKind.Good;

        return FeedbackKind.None;
    }

    private static bool SaysVerdict(
        List<string> tokens, string joined, string squashed, string[] phrases, HashSet<string> words)
    {
        if (tokens.Any(t => words.Contains(t) || words.Contains(Squash(t)))) return true;

        // Squashed on both sides so a stretched "gooood bot" still lands.
        return phrases.Any(p => joined.Contains(" " + p + " ")
                                || squashed.Contains(" " + Squash(p) + " "));
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
    // preserving position for cues that depend on word order.
    private static List<string> TokenizeOrdered(string content)
    {
        var sb = new StringBuilder(content.Length);
        foreach (var ch in content.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }
        return sb.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }
}
