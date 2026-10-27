using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The rules around a session's places and the names on its cards: when a full session has opened
/// a place for its « peut-être » (the waiting list the card promises), when they get their nudge,
/// and how many mentions a card lists before the rest becomes a count. Pure, so the checks can pin
/// them.
/// </summary>
public static class SessionSeats
{
    /// <summary>
    /// Names per roster field on the session card. 20 mentions of up to 23 characters, one per
    /// line, plus the « … et N autre(s) » tail stay far under the 1024 a field holds; MaxPlayers
    /// goes up to 67, and an unlimited session can draw any crowd.
    /// </summary>
    public const int MaxCardNames = 20;

    /// <summary>
    /// Voters listed per option on a poll or vote card. Ten options, each with a label of up to 80
    /// characters, its bar and this many mentions, stay under the 4000 characters a Components V2
    /// message holds, closed card included; the checks build that case.
    /// </summary>
    public const int MaxPollVoterNames = 8;

    /// <summary>A capped session holds between these many people; 0 is unlimited.</summary>
    public const int MinPlayers = 2;
    public const int MaxPlayers = 67;

    /// <summary>The refusal both the creation and the edit give a bad number of places.</summary>
    public const string CapRule = "Le nombre de places doit être vide ou 0 (illimité), ou un nombre entre 2 et 67.";

    /// <summary>
    /// The one rule for a typed number of places, at creation and at edit alike: empty or 0 means
    /// unlimited, otherwise 2–67.
    /// </summary>
    public static bool TryParseCap(string? input, out int max)
    {
        max = 0;
        var text = input?.Trim() ?? string.Empty;
        if (text.Length == 0) return true;
        return int.TryParse(text, out max) && IsValidCap(max);
    }

    public static bool IsValidCap(int max) => max == 0 || (max >= MinPlayers && max <= MaxPlayers);

    /// <summary>A new cap that would leave people signed up beyond it: refused, nobody is kicked out.</summary>
    public static bool CapBelowJoined(int newMax, int joined) => newMax > 0 && newMax < joined;

    /// <summary>
    /// Whether the card's buttons still take an answer: not once the session is cancelled or has
    /// started. The lifecycle pass removes the buttons up to five minutes later; this closes the gap.
    /// </summary>
    public static bool CanRespond(SessionEvent session, DateTimeOffset now, out string refusal)
    {
        if (session.IsCancelled)
        {
            refusal = "Cette session a été annulée.";
            return false;
        }
        if (now >= session.ScheduledAt)
        {
            refusal = session.PhaseAt(now) == SessionPhase.InProgress
                ? "La session a commencé : les inscriptions sont fermées."
                : "Cette session est terminée.";
            return false;
        }
        refusal = string.Empty;
        return true;
    }

    /// <summary>The nudge goes out between these two leads before the start: about an hour.</summary>
    public static readonly TimeSpan NudgeLeadMin = TimeSpan.FromMinutes(55);
    public static readonly TimeSpan NudgeLeadMax = TimeSpan.FromMinutes(70);

    /// <summary>
    /// True when a full session has a place again: capped, full before, not full after. That is
    /// the one moment its « peut-être » hear about it. An unlimited session never fills; a session
    /// already short of people opened nothing.
    /// </summary>
    public static bool PlaceOpened(int maxPlayers, int joinedBefore, int joinedAfter) =>
        maxPlayers > 0 && joinedBefore >= maxPlayers && joinedAfter < maxPlayers;

    /// <summary>
    /// Same, when the organiser edits the cap: it was full under the old cap, and the new one
    /// leaves room (0 being unlimited).
    /// </summary>
    public static bool PlaceOpenedByEdit(int oldMax, int newMax, int joined) =>
        oldMax > 0 && joined >= oldMax && (newMax == 0 || joined < newMax);

    /// <summary>
    /// True when the « peut-être » should be nudged now: inside the lead window, and with room left
    /// (on a full session « Peut-être » is the waiting list, and nudging it to join would be a tease).
    /// </summary>
    public static bool NudgeDue(DateTimeOffset start, DateTimeOffset now, int maxPlayers, int joined)
    {
        var lead = start - now;
        bool hasRoom = maxPlayers == 0 || joined < maxPlayers;
        return hasRoom && lead >= NudgeLeadMin && lead <= NudgeLeadMax;
    }

    /// <summary>
    /// The first <paramref name="max"/> mentions, joined by <paramref name="separator"/>, then
    /// « … et N autre(s) » for the rest. Empty for none.
    /// </summary>
    public static string Mentions(IReadOnlyList<ulong> ids, int max, string separator = "\n")
    {
        if (ids.Count == 0) return string.Empty;
        var shown = string.Join(separator, ids.Take(max).Select(id => $"<@{id}>"));
        return ids.Count > max ? $"{shown}{separator}… et {ids.Count - max} autre(s)" : shown;
    }
}
