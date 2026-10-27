using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Verdicts on her (good bot / bad bot) and shutdown threats.
internal static partial class MessageCues
{
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
}
