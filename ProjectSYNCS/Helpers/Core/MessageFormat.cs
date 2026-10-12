namespace ProjectSYNCS.Helpers;

// Shared rendering for text the bot relays on someone's behalf.
public static class MessageFormat
{
    /// <summary>
    /// Truncates to <paramref name="maxLength"/> and renders every line as a
    /// Markdown blockquote, so relayed text is visibly not the bot's own words.
    /// </summary>
    public static string Quote(string text, int maxLength)
    {
        if (text.Length > maxLength)
            text = text[..maxLength] + " […]";

        return string.Join('\n', text.Split('\n').Select(line => $"> {line}"));
    }

    /// <summary>
    /// Glues a trailing kaomoji together with non-breaking spaces so a narrow embed
    /// (description beside a thumbnail) wraps it as one unit instead of splitting
    /// « (ᵕ • ᴗ •) » across two lines. The tail is the run of space-separated
    /// tokens at the end holding no ASCII letter, digit or bold marker, so a plain
    /// word, « UwU » or a custom emote stops the run.
    /// </summary>
    public static string KeepKaomojiTogether(string text)
    {
        var start = text.Length;
        var space = text.LastIndexOf(' ');
        while (space >= 0 && !HasWordChar(text.AsSpan(space + 1, start - space - 1)))
        {
            start = space;
            space = space == 0 ? -1 : text.LastIndexOf(' ', space - 1);
        }

        // `start` is now the first space inside the run; nothing to glue without one.
        if (start == text.Length) return text;
        return text[..start] + text[start..].Replace(' ', ' ');
    }

    private static bool HasWordChar(ReadOnlySpan<char> token)
    {
        foreach (var c in token)
            if (char.IsAsciiLetterOrDigit(c) || c == '*') return true;
        return false;
    }
}
