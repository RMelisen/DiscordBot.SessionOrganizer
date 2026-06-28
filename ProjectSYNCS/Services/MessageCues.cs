using System.Globalization;
using System.Text;

namespace ProjectSYNCS.Services;

// Lightweight intent detection over message text: is this a compliment, a
// greeting, or an insult? Matching is done on tokenized, lowercased,
// accent-stripped words so "Félicitations !" matches the cue "felicitations".
internal static class MessageCues
{
    // The "hi_cat" waving emote counts as a greeting on its own. Matched by its
    // ID so a rename of the emote won't break detection.
    private const ulong GreetingEmoteId = 1482305105276571774;

    // Cue words that flag a kind message.
    private static readonly string[] _niceCues =
    {
        "adorable", "adorables", "adorbs", "adore", "adores", "aime", "amazing", "awesome",
        "best", "bisous", "bravissimo", "bravo", "brillant", "brillante",
        "calin", "calins", "champion", "championne", "chapeau", "chou", "choupi", "choupinou", "classe", "coeur", "content", "contente", "cool", "cute",
        "dingue", "douce", "doux",
        "epique", "exceptionnel", "exceptionnelle", "extraordinaire",
        "fantastique", "felicitation", "felicitations", "fier", "fiere", "formidable",
        "genial", "geniale", "geniales", "geniaux", "gentil", "gentille", "gentilles", "gentils", "gg", "goat", "great",
        "heureuse", "heureux",
        "iconique", "incroyable", "incroyables", "intelligent", "intelligente",
        "kiff", "kiffe", "kiffer", "king",
        "legendaire", "legende", "love", "lovely",
        "magnifique", "magnifiques", "meilleur", "meilleure", "meilleures", "meilleurs", "mercbeaucoup", "merci", "mercii", "merciii", "merveilleuse", "merveilleux", "mignon", "mignonne", "mignonnes", "mignons", "mrc",
        "nice",
        "parfait", "parfaite", "parfaites", "parfaits", "perle", "precieuse", "precieux",
        "queen",
        "ravi", "ravie", "reine", "respect", "roi",
        "slay", "splendide", "style", "stylee", "sublime", "super", "superbe",
        "talentueuse", "talentueux", "thanks", "thx", "top", "tresor", "ty",
        "wonderful",
    };

    // Emoji/symbol cues for a kind message.
    private static readonly string[] _niceSymbols = { "❤", "🥰", "😍", "♡", "💖", "😊", "💕" };

    // Cue words that flag a greeting.
    private static readonly string[] _greetingCues =
    {
        "bonjour", "bonsoir", "cc", "coucou", "coucouu", "coucouuu",
        "hello", "helloo", "hellooo", "helloooo", "hey", "heyy", "heyyy", "heyyyy", "heyyyyy", "hi", "hola",
        "kilou", "pweeet", "pweet", "pwet", "salut", "slt", "yo",
    };

    // Cue words that flag a mean message.
    private static readonly string[] _meanCues =
    {
        "3.0",
        "abruti", "abrutie", "abruties", "abrutis", "affligeant", "affligeante", "affreuse", "affreux", "agacant", "agacante", "agacants", "arrogant", "arrogante",
        "barbant", "barbante", "bete", "betes", "betise", "betises", "blaireau", "blaireaux", "boiteuse", "boiteux", "boloss", "boulet", "boulette",
        "casse", "claque", "claquee", "clown", "clowns", "consternant", "consternante", "cretin", "cretine", "cretines", "cretins", "cringe",
        "debile", "debiles", "decevant", "decevante", "degage", "detestable", "deteste",
        "eclate", "eclatee", "ennuyeuse", "ennuyeux",
        "fade", "ferme",
        "gueguerre", "guignol", "guignols",
        "hais", "horrible", "horribles",
        "idiot", "idiote", "idiotes", "idiots", "imbecile", "imbeciles", "incompetent", "incompetente", "insipide", "insolent", "insolente", "insupportable", "insupportables", "inutile", "inutiles",
        "laid", "laide", "laides", "laids", "lamentable", "lamentables", "loser",
        "mauvais", "mauvaise", "minable", "minables", "moche", "moches",
        "naze", "nazes", "nul", "nullard", "nullarde", "nulle", "nulles", "nullos", "nuls",
        "pathetique", "pathetiques", "penible", "penibles", "pourri", "pourrie", "pourries", "pourris", "pretentieuse", "pretentieux",
        "quoka", "quokka",
        "raclure", "rate", "ratee", "ratees", "rates", "relou", "reloue", "relous", "ridicule", "ridicules",
        "saoulant", "saoulante", "soulant", "soulante", "stupide", "stupides",
        "tais", "tocard", "tocarde", "trash",
        "useless",
        "vilain", "vilaine",
        "zero", "zinzin"
    };

    // True when the message reads as a compliment (kind word or warm emoji).
    public static bool IsNice(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        if (_niceSymbols.Any(content.Contains)) return true;
        var words = Tokenize(content);
        return _niceCues.Any(words.Contains);
    }

    // True when the message reads as a greeting.
    public static bool IsGreeting(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        // The waving hi_cat emote is a greeting all by itself.
        if (content.Contains(GreetingEmoteId.ToString())) return true;
        var words = Tokenize(content);
        return _greetingCues.Any(words.Contains);
    }

    // True when the message contains an insult/mean word.
    public static bool IsMean(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        var words = Tokenize(content);
        return _meanCues.Any(words.Contains);
    }

    // Splits text into a set of lowercase, accent-stripped words for cue matching.
    private static HashSet<string> Tokenize(string content)
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
            .ToHashSet();
    }
}
