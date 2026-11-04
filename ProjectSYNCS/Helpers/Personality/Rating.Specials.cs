using System.Text;

namespace ProjectSYNCS.Helpers;

// /rate's small verdicts: a subject she has her own opinion of, with a few lines each
// (BotResponses.RateSpecialLines). Not stored anywhere, so this enum may be reordered freely.
public enum RateSpecial
{
    // Her lore.
    PingQilin, Glados, Wheatley, Eve, Voyager, Jarvis, Minecraft, FlyAgaric, Ups, CheapCharger,
    Maybe, Heatwave, Winter, SixSeven, Synthia, Storm, PowerCut,
    Monika, TwoB, Vacuum, Update, Restart, Router, GoodBot, BadBot, Poll, Lateness, Fireflies, Linux, Cake,
    RaspberryPi, Mushroom, MushroomSpecies, DeathCap, Monster, MonsterHunter, RedBull,
    // Whoever does her job worse, or thinks they're her.
    Doodle, GoogleCalendar, Excel, ChatGpt, Siri, Alexa, Arduino,
    // Liège and Belgium, and whoever is teased from there.
    Liege, LiegeWaffle, LiegeSyrup, Boulets, Septante, QuatreVingtDix, Fries, Belgium, Brussels, TheFrench,
    // Food.
    PineapplePizza,
}

// How a subject is read, for /rate and /ship alike. Everything here is for recognising only: the
// hash runs on Rating.Normalize's text, so no score she has already given moves.
public static partial class Rating
{
    /// <summary>
    /// The small verdicts. <c>Score</c> is the real one, because a preference ranks by it
    /// (<see cref="Opinion.Prefers"/>); each line says it too, and the checks hold them together.
    /// <c>Names</c> match the <b>whole subject</b> (<see cref="Bare"/>, then <see cref="Fold"/>, so a
    /// name here needs no stretched or double-letter spelling): unlike <see cref="Fixed"/> these are
    /// ordinary words, and « peut-être demain » isn't « peut-être ». A robot-heart character is read
    /// by <c>Heart</c> instead, so her names stay in MessageCues.
    /// </summary>
    public static readonly (RateSpecial Kind, int Score, string[] Names, HeartCharacter? Heart)[] Specials =
    {
        (RateSpecial.PingQilin, 10, new[] { "ping qilin", "pingqilin", "ping quilin", "ping kilin" }, null),
        (RateSpecial.Glados, 11, Array.Empty<string>(), HeartCharacter.Glados),
        (RateSpecial.Wheatley, 2, Array.Empty<string>(), HeartCharacter.Wheatley),
        (RateSpecial.Eve, 2, Array.Empty<string>(), HeartCharacter.Eve),
        (RateSpecial.Voyager, 7, Array.Empty<string>(), HeartCharacter.Voyager),
        (RateSpecial.Jarvis, 6, Array.Empty<string>(), HeartCharacter.Jarvis),
        (RateSpecial.Minecraft, 10, new[] { "minecraft", "mine craft", "minecraf", "mc" }, null),
        (RateSpecial.FlyAgaric, 10, new[] { "amanite", "amanites", "amanite tue mouches", "amanite tue mouche", "tue mouches", "tue mouche", "amanites tue mouches", "amanites tue mouche", "amanite muscaria", "amanita", "amanitas", "amanita muscaria", "muscaria", "amanite rouge", "fly agaric", "fausse oronge" }, null),
        (RateSpecial.Ups, 10, new[] { "onduleur", "onduleurs", "ups", "alimentation de secours", "batterie de secours" }, null),
        (RateSpecial.CheapCharger, 0, new[] { "chargeur a 3 euros", "chargeur de 3 euros", "chargeur 3 euros", "chargeur a 3", "chargeur a 3 balles", "chargeur 3 balles", "chargeur pas cher", "chargeur chinois", "chargeur pourri" }, null),
        // The spellings MessageCues._maybePhrases knows for the word itself, not its neighbours.
        (RateSpecial.Maybe, 0, new[] { "peut etre", "peutetre", "ptet", "p tet", "ptetre", "pt etre", "maybe", "vote peut etre", "votes peut etre" }, null),
        (RateSpecial.Heatwave, 1, new[] { "canicule", "canicules", "ete", "vague de chaleur", "vagues de chaleur", "chaleur" }, null),
        (RateSpecial.Winter, 10, new[] { "hiver", "hivers" }, null),
        (RateSpecial.SixSeven, 7, new[] { "67", "6 7", "six seven", "sixseven", "six sept", "soixante sept" }, null),
        (RateSpecial.Synthia, 10, new[] { "synthia", "synthya", "sinthia", "synthia avec un y" }, null),
        (RateSpecial.Storm, 0, new[] { "orage", "orages", "tempete", "tempetes", "foudre" }, null),
        (RateSpecial.PowerCut, 0, new[] { "coupure de courant", "coupures de courant", "coupure d electricite", "coupures d electricite", "coupure courant", "coupure elec", "coupure d elec", "coupure de jus", "panne de courant", "pannes de courant", "panne d electricite", "panne courant", "panne elec", "blackout", "black out", "plus de courant" }, null),
        (RateSpecial.Monika, 9, Array.Empty<string>(), HeartCharacter.Monika),
        (RateSpecial.TwoB, 10, Array.Empty<string>(), HeartCharacter.TwoB),
        (RateSpecial.Vacuum, 10, Array.Empty<string>(), HeartCharacter.Vacuum),
        (RateSpecial.Update, 10, new[] { "mise a jour", "mises a jour", "maj", "update", "updates" }, null),
        (RateSpecial.Restart, 0, new[] { "redemarrage", "redemarrages", "redemarrer", "reboot", "reboots", "restart", "restarts" }, null),
        (RateSpecial.Router, 1, new[] { "routeur", "routeurs", "router", "modem", "box internet" }, null),
        // Typed here they are a subject, not a verdict: /rate never touches her « good bot » tally.
        (RateSpecial.GoodBot, 10, new[] { "good bot", "goodbot", "bon bot", "bonne bot", "gentil bot", "gentille bot" }, null),
        (RateSpecial.BadBot, 0, new[] { "bad bot", "badbot", "mauvais bot", "mauvaise bot", "mechant bot", "mechante bot" }, null),
        (RateSpecial.Poll, 2, new[] { "sondage", "sondages", "sondage mort", "sondages morts" }, null),
        (RateSpecial.Lateness, 0, new[] { "retard", "retards", "etre en retard", "retardataire", "retardataires" }, null),
        (RateSpecial.Fireflies, 10, new[] { "luciole", "lucioles", "firefly", "fireflies" }, null),
        (RateSpecial.Linux, 10, new[] { "linux", "gnu linux", "debian", "ubuntu" }, null),
        (RateSpecial.Cake, 0, new[] { "gateau", "gateaux", "cake", "cakes", "the cake", "the cake is a lie", "gateau est un mensonge" }, null),
        // Not « framboise »: that's Papa's name for her, and it isn't anyone else's to rate.
        (RateSpecial.RaspberryPi, 10, new[] { "pi", "pi 5", "pi5", "raspberry", "raspberry pi", "raspberry pi 5", "raspberry pi5", "raspberrypi", "raspberrypi5", "raspi", "ras pi", "rpi", "rpi 5", "rpi5" }, null),
        (RateSpecial.Mushroom, 10, new[] { "champignon", "champignons", "champi", "champis", "mushroom", "mushrooms", "shroom", "shrooms", "mycologie", "mycologue", "mycologues", "mycelium", "fungi", "fungus", "fongus", "champignon sauvage", "champignons sauvages", "champignon comestible", "champignons comestibles", "champignon toxique", "champignons toxiques", "champignon venimeux", "champignons venimeux", "cueillette de champignons", "cueillette des champignons" }, null),
        // 9, under the family and the fly agaric at 10: she has a favourite, and the species' lines lean on it.
        (RateSpecial.MushroomSpecies, 9, WithPlurals(
            "cepe", "cepe de bordeaux", "bolet", "girolle", "chanterelle", "morille", "truffe", "pleurote", "shiitake", "portobello",
            "enoki", "maitake", "reishi", "champignon de paris", "rose des pres", "trompette de la mort", "pied de mouton", "pied bleu",
            "coulemelle", "lepiote", "russule", "lactaire", "coprin", "vesse de loup", "mousseron", "armillaire", "cortinaire",
            "tricholome", "clitocybe", "polypore", "agaric", "oronge", "amanite des cesars", "psalliote", "nameko", "porcini", "morel",
            "truffe noire", "truffe blanche", "truffe d ete", "truffe du perigord", "lepiote elevee", "cepe d ete", "bolet bai")
            // WithPlurals only reaches the first word: these adjectives agree too.
            .Concat(new[] { "truffes noires", "truffes blanches", "lepiotes elevees", "pieds bleus" }).ToArray(), null),
        (RateSpecial.DeathCap, 9, WithPlurals("amanite phalloide", "phalloide", "amanite vireuse", "amanite printaniere", "ange destructeur", "death cap", "destroying angel"), null),
        // The drink and the game, split as MessageCues.MentionsEnergyDrink splits them for her can.
        (RateSpecial.Monster, 10, new[]
        {
            "monster", "monsters", "monster energy", "monster energie", "monster energy drink", "monster energy drinks", "monster blanc", "monster blanche", "monster white",
            "monster vert", "monster verte", "monster noir", "monster noire", "monster ultra", "monster ultra white", "monster zero", "monster zero sugar",
            "monster mango", "monster mango loco", "monster pipeline", "monster juice", "monster rehab",
            "canette de monster", "canettes de monster", "canette monster", "canettes monster", "boisson monster",
        }, null),
        // Any title after « monster hunter » counts too: FamilyTitles.
        (RateSpecial.MonsterHunter, 10, new[]
        {
            "monster hunter", "monster hunters", "monhun", "mon hun", "mh", "mhw", "mhwi", "mhr", "mhs", "mh1", "mh3", "mh4", "mh4u", "mhfu", "mhgu", "mhworld", "mhrise", "mhwilds",
            "mh world", "mh rise", "mh wilds", "mh iceborne", "mh sunbreak",
        }, null),
        (RateSpecial.RedBull, 2, new[] { "red bull", "redbull", "redbulls", "red bull energy", "red bull energy drink", "red bull zero", "red bull sans sucre", "canette de red bull", "canettes de red bull", "canette de redbull", "canettes de redbull", "canette red bull", "canettes red bull", "canette redbull", "canettes redbull" }, null),

        (RateSpecial.Doodle, 0, new[] { "doodle", "doodle com", "doodles" }, null),
        (RateSpecial.GoogleCalendar, 0, new[] { "google agenda", "agenda google", "google calendar", "google calendrier", "calendrier google", "google cal", "gcal" }, null),
        (RateSpecial.Excel, 0, new[] { "excel", "microsoft excel", "ms excel", "tableur", "tableurs", "google sheets", "google sheet" }, null),
        (RateSpecial.ChatGpt, 1, new[] { "chatgpt", "chat gpt", "chatgpt 4", "chatgpt 5", "gpt", "gpt 4", "gpt 5", "openai", "open ai", "chat j ai pete" }, null),
        (RateSpecial.Siri, 2, new[] { "siri", "hey siri", "dis siri" }, null),
        (RateSpecial.Alexa, 2, new[] { "alexa", "hey alexa", "amazon alexa", "amazon echo" }, null),
        (RateSpecial.Arduino, 1, new[] { "arduino", "arduinos", "arduino uno", "arduino nano", "arduino mega" }, null),

        (RateSpecial.Liege, 10, new[] { "liege", "liegeois", "liegeoise", "liegeoises", "liegois", "liegoise", "gens de liege", "ville de liege", "cite ardente" }, null),
        (RateSpecial.LiegeWaffle, 10, new[] { "gaufre de liege", "gaufres de liege", "gaufre liege", "gaufres liege", "gaufre liegeoise", "gaufres liegeoises" }, null),
        (RateSpecial.LiegeSyrup, 10, new[] { "sirop de liege", "sirop liege", "sirop liegeois" }, null),
        (RateSpecial.Boulets, 10, new[] { "boulet a la liegeoise", "boulets a la liegeoise", "boulette a la liegeoise", "boulettes a la liegeoise", "boulet liegeois", "boulets liegeois", "boulet de liege", "boulets de liege", "boulet sauce lapin", "boulets sauce lapin" }, null),
        (RateSpecial.Septante, 10, new[] { "septante", "nonante" }, null),
        (RateSpecial.QuatreVingtDix, 0, new[] { "quatre vingt dix", "quatre vingts dix", "quatrevingtdix", "soixante dix", "soixantedix" }, null),
        (RateSpecial.Fries, 10, new[] { "frites", "frite", "cornet de frites", "paquet de frites" }, null),
        (RateSpecial.Belgium, 10, new[] { "belgique", "belgium", "belge", "belges" }, null),
        (RateSpecial.Brussels, 4, new[] { "bruxelles", "brussels", "bruxellois", "bruxelloise", "bruxelloises" }, null),
        // « Les Français » whole, with its article: « le français » is the language she speaks.
        (RateSpecial.TheFrench, 3, new[] { "les francais", "francaises", "france" }, null),

        (RateSpecial.PineapplePizza, -10, new[] { "pizza ananas", "pizzas ananas", "pizza a l ananas", "pizzas a l ananas", "pizza a ananas", "pizza avec ananas", "pizza avec de l ananas", "pizza hawaienne", "pizzas hawaiennes", "pizza hawaii", "ananas pizza", "ananas sur pizza", "ananas sur la pizza", "ananas sur une pizza" }, null),
    };

    // Each name and its French plural, on the first word: « pieds de mouton ».
    private static string[] WithPlurals(params string[] names) => names
        .SelectMany(n => new[] { n, n.Contains(' ') ? n.Insert(n.IndexOf(' '), "s") : n + "s" })
        .ToArray();

    private static readonly HashSet<RateSpecial> Rivals = new()
    {
        RateSpecial.Doodle, RateSpecial.GoogleCalendar, RateSpecial.Excel, RateSpecial.ChatGpt, RateSpecial.Siri, RateSpecial.Alexa, RateSpecial.Arduino,
    };

    /// <summary>Whoever does her job worse: /ship wishes any two of them every happiness, far away.</summary>
    public static bool IsRival(RateSpecial? special) => special is { } s && Rivals.Contains(s);

    // After Specials, in this file: a field's initializer runs in textual order. Two names folding
    // to the same key throw here, on the first /rate: the checks read every name, so they catch it.
    private static readonly Dictionary<string, RatingResult> SpecialByName = Specials
        .SelectMany(s => s.Names.Select(n => (Key: Fold(n), Result: new RatingResult(RatingKind.Special, s.Score, Special: s.Kind))))
        .ToDictionary(x => x.Key, x => x.Result);

    private static readonly Dictionary<HeartCharacter, RatingResult> SpecialByHeart = Specials
        .Where(s => s.Heart is not null)
        .ToDictionary(s => s.Heart!.Value, s => new RatingResult(RatingKind.Special, s.Score, Special: s.Kind));

    // What may open a subject without being it: « l'hiver », « cet hiver », « en hiver », « mon onduleur ».
    private static readonly HashSet<string> LeadingWords = new()
    {
        "le", "la", "les", "l", "un", "une", "des", "du", "de", "d", "a", "au", "aux", "en", "the",
        "ce", "cet", "cette", "ces", "mon", "ma", "mes", "ton", "ta", "tes", "son", "sa", "ses",
        "notre", "nos", "votre", "vos", "leur", "leurs", "quelques", "plusieurs",
    };

    // What may stand before the subject without changing it: « un petit cèpe », « une bonne Monster ».
    // Tried after the names themselves (Readings), so « bon bot » stays one name.
    private static readonly HashSet<string> Qualifiers = new()
    {
        "bon", "bons", "bonne", "bonnes", "petit", "petits", "petite", "petites", "gros", "grosse", "grosses",
        "vrai", "vrais", "vraie", "vraies",
    };

    // A title whose every edition is the same subject: « Monster Hunter Stories ». Takes what
    // follows on trust, so only a name that can't be the start of anything else belongs here.
    private static readonly string[] FamilyTitles = { "monster hunter" };

    // What may close one: « doodle franchement », « orage stp ».
    private static readonly string[] TrailingWords =
    {
        "stp", "svp", "s il te plait", "hein", "franchement", "sinon", "alors", "quand meme", "selon toi", "a ton avis", "en vrai",
    };

    /// <summary>
    /// A normalised text without what surrounds the subject: « les liegeois » is « liegeois », as
    /// chat reads it (<see cref="Opinion"/>). A word alone stays: « la » is still « la ».
    /// </summary>
    public static string Bare(string normalized)
    {
        var space = normalized.IndexOf(' ');
        while (space > 0 && LeadingWords.Contains(normalized[..space]))
        {
            normalized = normalized[(space + 1)..];
            space = normalized.IndexOf(' ');
        }

        var trimmed = true;
        while (trimmed)
        {
            trimmed = false;
            foreach (var word in TrailingWords)
                if (normalized.EndsWith($" {word}", StringComparison.Ordinal))
                {
                    normalized = normalized[..^(word.Length + 1)];
                    trimmed = true;
                }
        }
        return normalized;
    }

    /// <summary>
    /// The keys a subject may be looked up under, most literal first, all folded: as typed (a few
    /// names keep their article: « les francais »), then <see cref="Bare"/>, then without its
    /// qualifiers. The tables hold folded names, so these compare directly.
    /// </summary>
    public static IEnumerable<string> Readings(string normalized)
    {
        yield return Fold(normalized);
        var bare = Bare(normalized);
        yield return Fold(bare);

        var plain = bare;
        var space = plain.IndexOf(' ');
        while (space > 0 && Qualifiers.Contains(plain[..space]))
        {
            plain = plain[(space + 1)..];
            space = plain.IndexOf(' ');
        }
        if (plain != bare) yield return Fold(plain);
    }

    /// <summary>
    /// Every run of one letter down to that letter, on both sides of a comparison: « hiverrr » and
    /// « mayonaise » read as their names. Digits stay: 677 isn't 67. For matching only.
    /// </summary>
    public static string Fold(string text)
    {
        var sb = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
            if (i == 0 || text[i] != text[i - 1] || !char.IsLetter(text[i])) sb.Append(text[i]);
        return sb.ToString();
    }

    // The words of a title or of somebody else that hold one of the names in Fixed: Demon Slayer
    // isn't the devil, nor Papa Noël Papa. Taken out before the names are looked for, so « papa
    // noel et quokka » is still the nemesis's. Add every misfire here.
    private static readonly string[] NotThem =
    {
        "god of war", "god eater", "god hand", "god mode", "god save", "oh my god", "demi god", "demi dieu",
        "demon slayer", "demon souls", "demon s souls", "demon hunter", "demon hunters", "demon king", "demon lord",
        "diable s habille en prada", "diable rouge",
        "papa noel", "papa roach", "papa smurf", "papa schtroumpf", "papa john", "papa johns", "papa john s", "papa s",
        "papa de", "papa du", "papa d",
        "empire romain", "droit romain", "soldat romain", "chiffre romain",
        "tata motors", "tata steel", "tata yoyo", "tata de", "tata du", "tata d",
        "resto routier", "restaurant routier", "relais routier", "transport routier", "transports routiers",
        "camion routier", "reseau routier", "code routier", "accident routier",
        "adam et eve", "adam and eve",
    };

    private const int MinFoldedName = 4; // Shorter, and « good » would read as « god ».

    private static string Scrub(string padded)
    {
        // Her father and her aunt are still hers: only somebody else's are taken out below.
        padded = padded.Replace(" papa de syncs ", " papa ").Replace(" tata de syncs ", " tata ");
        foreach (var words in NotThem) padded = padded.Replace($" {words} ", " ");
        return padded;
    }

    private static bool Names(string scrubbed, string folded, string name) =>
        scrubbed.Contains($" {name} ")
        || (name.Length >= MinFoldedName && folded.Contains($" {Fold(name)} "));

    /// <summary>
    /// Which robot-heart character a subject names (MessageCues holds the names), but not as a word
    /// of something else (« Adam et Ève »), and through stretched letters (« Glaaados »), though
    /// never for EVE: Évoli's English name folds to hers.
    /// </summary>
    public static HeartCharacter? ReadHeart(string side)
    {
        var padded = $" {Normalize(side)} ";
        var scrubbed = Scrub(padded);
        var read = Services.MessageCues.ReadHeartCharacter(scrubbed == padded ? side : scrubbed);
        if (read is not null || side.Contains('<')) return read;
        return Services.MessageCues.ReadHeartCharacter(Fold(scrubbed)) is { } folded and not HeartCharacter.Eve ? folded : null;
    }

    private static RatingResult? ReadSpecial(string subject, string text)
    {
        foreach (var reading in Readings(text))
        {
            if (SpecialByName.TryGetValue(reading, out var named)) return named;
            foreach (var title in FamilyTitles)
                if (reading.StartsWith($"{Fold(title)} ", StringComparison.Ordinal)) return SpecialByName[Fold(title)];
        }
        return ReadHeart(subject) is { } heart && SpecialByHeart.TryGetValue(heart, out var found) ? found : null;
    }
}
