using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Scoring a message's mood, and the small reads beside it (shouting, « quoi », energy drinks).
internal static partial class MessageCues
{
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
}
