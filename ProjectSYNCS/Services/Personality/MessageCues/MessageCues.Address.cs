using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Her name: Synthia, pet names, whether a message speaks to her, her robot heart. _leadingAddress
// stays here, after _thirdPersonAfter which it reads: static field order across partial files is undefined.
internal static partial class MessageCues
{
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

    // Her name as an address, opening the message (after « dis », « hey »…) or closing it before its
    // punctuation. Not when it is the subject of a sentence about her (« syncs a raison », « syncs
    // est là ? »): those read on the name, through _thirdPersonAfter.
    private static readonly Regex _leadingAddress = new(
        @"^\s*(?:(?:dis|hey|eh|oh|yo|allez|bon|alors|ok|ah|wesh)[\s,]+)?syncs?(?![\w.])(?!\s+(?:"
        + string.Join("|", _thirdPersonAfter) + @")(?!\w))[\s,:!]*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex _trailingAddress = new(@"[\s,]+syncs?(?![\w.])(?=[\s?!.…]*$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}
