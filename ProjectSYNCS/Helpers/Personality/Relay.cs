using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>What a relay carries: a message (« que c'est un caca ») or a greeting (« coucou »).</summary>
public enum RelayKind
{
    Message,
    Greeting,
}

/// <summary>
/// « dis à @Luca que c'est un caca », as read: who to tell (<see cref="TargetId"/>), what (<see cref="Said"/>),
/// whose tag opened the message if one did (<see cref="LeadId"/>: hers on the mention path, anyone
/// else's means it was said to them), and whether her name addressed it (<see cref="Named"/>: « syncs,
/// dis à… », « …syncs »). A message's <see cref="Said"/> keeps its « que » / « qu' » (« qu'il est
/// beau ») or « de » / « d' » (« de venir »), or is « : « … » » when the words came after a colon, so it
/// always follows « te dire ».
/// </summary>
public sealed record RelayAsk(RelayKind Kind, ulong TargetId, string Said, ulong? LeadId, bool Named);

/// <summary>
/// Someone asking her to pass something on to someone else, pure. Read on the raw text, since the
/// target is a tag. Same guards as <see cref="Opinion"/>: whitespace squeezed, a timeout on every
/// pattern, and nothing longer than <see cref="MaxLength"/> is read. The caller checks the target is a
/// real person (not her, not the asker, not a bot) and that the message was said to her.
/// </summary>
public static partial class Relay
{
    public const int MaxLength = 300;
    // What she passes on, at most: a sentence, not a speech.
    public const int MaxSaidLength = 200;

    // A tag opening the message: hers on the mention path (« @SYNCS dis à @Luca… »).
    [GeneratedRegex(@"^<@!?(?<id>\d+)>[\s,:]*", RegexOptions.CultureInvariant, Opinion.MatchTimeoutMs)]
    private static partial Regex LeadTagRegex();

    // Her name as an address at the start (« syncs, dis à… », « hey syncs dis à… »).
    [GeneratedRegex(@"^(?:(?:hey|eh|oh|yo|allez|bon|alors|ok|ah|wesh)[\s,]+)?syncs?(?![\w.])[\s,:!]*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Opinion.MatchTimeoutMs)]
    private static partial Regex LeadingNameRegex();

    // …or at the end (« …que c'est un caca syncs », « tu peux lui dire… syncs ? »).
    [GeneratedRegex(@"[\s,]+syncs?(?![\w.])[\s!?.…]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Opinion.MatchTimeoutMs)]
    private static partial Regex TrailingNameRegex();

    // « dis à @Luca que… », « tu peux dire à @Luca qu'… », « répète à @Luca : … », « va dire à @Luca que… »,
    // « préviens @Luca que… », « rappelle à @Luca que… », « dis à @Luca de venir ». The words before the
    // verb are the ways of asking; the verb is never in the past (« j'ai dit à @Luca que… » tells, it
    // doesn't ask), but « dit » is how « dis » is often typed. « rappelle » only with « à »: « rappelle
    // @Luca demain » is a reminder.
    [GeneratedRegex(@"^(?:(?:stp|steuplait|s['’ ]?il te pla[iî]t)[\s,]+)?(?:(?:est[- ]ce que\s+)?(?:tu peux|tu pourrais|tu veux bien|tu voudrais bien|peux[- ]tu|pourrais[- ]tu)\s+)?(?:(?:va|vas|aller)\s+)?(?:(?:dire|dis|dit|di|dites|r[ée]p[èée]te|r[ée]p[ée]ter|transmets|transmettre|pr[ée]viens|pr[ée]venir)\s+(?:lui\s+)?(?:(?:à|a)\s+)?|rappelle[rz]?\s+(?:à|a)\s+)<@!?(?<target>\d+)>[\s,]*(?:(?<said>(?:que|qu['’ ]|de\s|d['’ ])\s*\S.*)|:\s*(?<quoted>\S.*))$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline, Opinion.MatchTimeoutMs)]
    private static partial Regex MessageRegex();

    // « dis bonjour à @Luca », « passe le bonjour à @Luca de ma part », « fais un coucou à @Luca »,
    // « souhaite bonne nuit à @Luca », « dis merci à @Luca ».
    [GeneratedRegex(@"^(?:(?:stp|steuplait|s['’ ]?il te pla[iî]t)[\s,]+)?(?:(?:est[- ]ce que\s+)?(?:tu peux|tu pourrais|tu veux bien|peux[- ]tu|pourrais[- ]tu)\s+)?(?:dire|dis|dit|di|passe|passer|fais|faire|envoie|envoyer|souhaite|souhaiter)\s+(?<greet>bonjour|bonsoir|salut|coucou|hello|bonne nuit|bonne journ[ée]e|bonne soir[ée]e|un coucou|le bonjour|un bisou|des bisous|un câlin|un calin|des câlins|des calins|merci|bravo|f[ée]licitations|bienvenue|bonne chance|bon courage|bon anniversaire|joyeux anniversaire|un bon anniversaire|un joyeux anniversaire)\s+(?:(?:à|a)\s+)?<@!?(?<target>\d+)>(?:\s+de ma part)?(?:[\s,]+(?:stp|s['’ ]?il te pla[iî]t))?[\s!.…]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Opinion.MatchTimeoutMs)]
    private static partial Regex GreetingRegex();

    // The greeting as she says it: « un coucou » is a « coucou », « des câlins » a « câlin ».
    private static string Greeting(string greet) => greet.ToLowerInvariant() switch
    {
        "un coucou" => "coucou",
        "le bonjour" => "bonjour",
        "un bisou" or "des bisous" => "bisous",
        "un bon anniversaire" => "bon anniversaire",
        "un joyeux anniversaire" => "joyeux anniversaire",
        "un câlin" or "un calin" or "des câlins" or "des calins" => "câlin",
        var g => g,
    };

    /// <summary>The relay asked for, or null when the message asks her to pass nothing on.</summary>
    public static RelayAsk? Read(string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > MaxLength) return null;
        try
        {
            var text = Opinion.Squeeze(content);
            ulong? lead = null;
            if (LeadTagRegex().Match(text) is { Success: true } tag)
            {
                lead = ulong.Parse(tag.Groups["id"].Value);
                text = text[tag.Length..];
            }

            var named = false;
            if (LeadingNameRegex().Match(text) is { Success: true, Length: > 0 } name)
            {
                named = true;
                text = text[name.Length..];
            }
            if (TrailingNameRegex().Match(text) is { Success: true } trailing)
            {
                named = true;
                text = text[..trailing.Index];
            }
            text = text.Trim();

            // A greeting ends on its tag: a trailing « 🙂 » is decoration. A message keeps its own.
            if (GreetingRegex().Match(Opinion.TrimTail(text)) is { Success: true } g && ulong.TryParse(g.Groups["target"].Value, out var greeted))
                return new RelayAsk(RelayKind.Greeting, greeted, Greeting(g.Groups["greet"].Value), lead, named);

            if (MessageRegex().Match(text) is not { Success: true } m || !ulong.TryParse(m.Groups["target"].Value, out var target))
                return null;
            var said = m.Groups["said"].Success
                ? m.Groups["said"].Value.Trim()
                : $": « {m.Groups["quoted"].Value.Trim().Trim('«', '»', '"', ' ')} »";
            said = said.TrimEnd(' ', '.', '!');
            // « que » alone passes nothing on.
            return said.Length is > 4 and <= MaxSaidLength ? new RelayAsk(RelayKind.Message, target, said, lead, named) : null;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }
}
