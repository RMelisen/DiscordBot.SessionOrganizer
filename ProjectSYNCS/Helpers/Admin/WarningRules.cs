using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// When a warning counts, and when staff are offered a mute for it.
/// </summary>
public static class WarningRules
{
    /// <summary>
    /// How long a warning counts. Older ones stay in the history but no longer add up, so one
    /// bad evening in March doesn't follow someone into the next year.
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(90);

    /// <summary>
    /// From this many active warnings, the confirmation offers a one-hour mute. Offered, never
    /// applied: a person decides.
    /// </summary>
    public const int MuteSuggestionThreshold = 3;

    /// <summary>The mute the button offers, in minutes.</summary>
    public const int SuggestedMuteMinutes = 60;

    public static bool IsActive(MemberWarning warning, DateTimeOffset now) =>
        warning.RevokedAt is null && now - warning.CreatedAt < Lifetime;

    public static bool SuggestsMute(int activeCount) => activeCount >= MuteSuggestionThreshold;
}
