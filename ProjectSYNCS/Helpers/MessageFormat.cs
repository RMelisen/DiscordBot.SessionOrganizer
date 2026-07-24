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
}
