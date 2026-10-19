namespace ProjectSYNCS.Services;

/// <summary>One message she found nothing specific to answer, as read back. No author: only what was said.</summary>
public readonly record struct UnansweredMessage(DateTimeOffset At, string Path, string Kind, string Text);

/// <summary>
/// The rules of the unanswered-message log behind `/debug answers view: Examples` and the owner's
/// monthly digest: the raw material for the next intents. The rows are UnansweredExample, written by
/// <see cref="ChatAnswerService.AddAsync"/> in the same save as the answer's count, and kept
/// <see cref="KeptDays"/> rolling days — a restart no longer loses them.
/// </summary>
/// <remarks>
/// Deliberately keeps no author id or name, at the owner's request: the point is what people say to
/// her, not who said it. The text is kept as written (mentions included), newlines collapsed and
/// capped at <see cref="MaxTextLength"/>.
/// </remarks>
public static class UnansweredLog
{
    public const int KeptDays = 30;
    public const int MaxTextLength = 200;

    /// <summary>The text as stored: whitespace runs collapsed to one space, capped; empty when nothing is left.</summary>
    public static string Clean(string text)
    {
        var clean = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return clean.Length > MaxTextLength ? clean[..(MaxTextLength - 1)] + "…" : clean;
    }
}
