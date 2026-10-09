using System.Globalization;
using System.Text;

namespace ProjectSYNCS.Helpers;

// Does a chat message answer an open quiz question? Pure, so it can be checked without a
// gateway.
//
// Forgiving where people really type differently, and strict where forgiveness would let
// someone win without knowing:
//
// - **Case, accents, punctuation and spacing don't count.** "Pétrichor !" and "petri chor"
//   both match "pétrichor": the comparison runs on accent-stripped letters and digits, with
//   the spaces squashed out of both sides.
// - **The answer may sit inside a sentence** ("je pense que c'est backrub non ?"), as long
//   as the rest is filler. More than MaxExtraWords other words and the message is a list of
//   guesses, not an answer: « mario luigi peach bowser » must not win "Luigi".
// - **Typos** are tolerated by length: none under 5 letters (a one-letter slip on "link" is
//   another word), one from 5, two from 10. Never on an answer holding a digit ("h2o").
//
// Authoring rule this relies on (see QuizBank): an open question's answer space is too big
// to brute-force. Years, small numbers and yes/no are multiple choice.
public static class QuizAnswer
{
    /// <summary>How many non-filler words a message may carry besides the answer.</summary>
    public const int MaxExtraWords = 2;

    // Words that say nothing about which answer was meant. Accent-stripped, lowercase,
    // as Tokenize produces them ("c'est" arrives as "c" + "est").
    private static readonly HashSet<string> Filler = new(StringComparer.Ordinal)
    {
        "je", "j", "pense", "crois", "dirais", "dis", "que", "qu", "c", "est", "ce", "cest", "ca",
        "sont", "serait", "non", "oui", "euh", "heu", "hmm", "bah", "ben", "alors", "peut", "etre",
        "le", "la", "les", "l", "un", "une", "des", "de", "du", "d", "en", "a", "au", "aux",
        "the", "an", "it", "is", "its", "reponse", "facile", "trop", "ez", "lol", "mdr",
    };

    // Stripped from the front of an accepted answer, so "le Vatican" is stored naturally
    // and still matches a bare "vatican".
    private static readonly HashSet<string> Articles = new(StringComparer.Ordinal)
    {
        "le", "la", "les", "l", "un", "une", "des", "the", "a", "an",
    };

    /// <summary>
    /// Whether <paramref name="message"/> gives one of <paramref name="accepted"/>.
    /// </summary>
    public static bool Matches(string message, IReadOnlyList<string> accepted)
    {
        var words = Tokenize(message);
        if (words.Count == 0) return false;

        foreach (var answer in accepted)
        {
            var target = Tokenize(answer);
            while (target.Count > 1 && Articles.Contains(target[0])) target.RemoveAt(0);
            if (target.Count == 0) continue;

            if (Contains(words, target)) return true;
        }
        return false;
    }

    // Slides a window of one to (answer words + 1) message words, squashed together, over
    // the message; a window within tolerance of the squashed answer is a hit, provided the
    // words outside it are mostly filler.
    private static bool Contains(List<string> words, List<string> target)
    {
        var squashed = string.Concat(target);
        var tolerance = ToleranceFor(squashed);
        var maxWindow = Math.Min(words.Count, target.Count + 1);

        for (var size = 1; size <= maxWindow; size++)
        {
            for (var start = 0; start + size <= words.Count; start++)
            {
                var window = string.Concat(words.Skip(start).Take(size));
                if (Math.Abs(window.Length - squashed.Length) > tolerance) continue;
                if (Distance(window, squashed) > tolerance) continue;

                var extra = words
                    .Where((_, i) => i < start || i >= start + size)
                    .Count(w => !Filler.Contains(w));
                if (extra <= MaxExtraWords) return true;
            }
        }
        return false;
    }

    private static int ToleranceFor(string squashed)
    {
        if (squashed.Any(char.IsDigit)) return 0;
        if (squashed.Length >= 10) return 2;
        if (squashed.Length >= 5) return 1;
        return 0;
    }

    /// <summary>Lowercase, accent-free words of letters and digits; everything else splits.</summary>
    public static List<string> Tokenize(string text)
    {
        var decomposed = text.ToLowerInvariant()
            .Replace("œ", "oe").Replace("æ", "ae")
            .Normalize(NormalizationForm.FormD);

        var words = new List<string>();
        var current = new StringBuilder();
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c))
            {
                current.Append(c);
                continue;
            }
            if (current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }
        }
        if (current.Length > 0) words.Add(current.ToString());
        return words;
    }

    // Plain Levenshtein: answers are a few dozen characters at most.
    private static int Distance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }
            (previous, current) = (current, previous);
        }
        return previous[b.Length];
    }
}
