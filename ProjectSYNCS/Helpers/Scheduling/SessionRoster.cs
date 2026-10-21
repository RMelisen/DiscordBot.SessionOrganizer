namespace ProjectSYNCS.Helpers;

/// <summary>
/// Who is signed up for a session, in finished words for her chat lines (« 3 inscriptions (Luca,
/// Sandra et Ina) »), so no line has to agree anything with a count. The lines put it after a label
/// (« Liste d'appel : {3} »), so it opens lower-case and never carries its own colon. Pure: the names
/// arrive already resolved and sanitised.
/// </summary>
public static class SessionRoster
{
    // Past this many names, the rest is a count: a chat line is not the session card.
    public const int MaxNames = 5;

    /// <param name="joined">Display names of those who said yes, in sign-up order.</param>
    /// <param name="maybes">How many said « peut-être »: her pet peeve, so she counts them out loud.</param>
    public static string Describe(IReadOnlyList<string> joined, int maybes)
    {
        var who = joined.Count switch
        {
            0 => "aucune inscription",
            1 => $"1 inscription ({joined[0]})",
            _ => $"{joined.Count} inscriptions ({Names(joined)})",
        };
        return maybes switch
        {
            0 => who,
            1 => $"{who}, plus un « peut-être »",
            _ => $"{who}, plus {maybes} « peut-être »",
        };
    }

    // « Luca, Sandra et Ina », or the first MaxNames and « et 3 autres ».
    private static string Names(IReadOnlyList<string> names)
    {
        if (names.Count > MaxNames)
        {
            var rest = names.Count - MaxNames;
            return $"{string.Join(", ", names.Take(MaxNames))} et {rest} {(rest == 1 ? "autre" : "autres")}";
        }
        return $"{string.Join(", ", names.Take(names.Count - 1))} et {names[^1]}";
    }
}
