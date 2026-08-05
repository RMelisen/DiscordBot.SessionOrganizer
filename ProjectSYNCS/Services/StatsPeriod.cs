namespace ProjectSYNCS.Services;

/// <summary>Which window a leaderboard covers.</summary>
/// <remarks>
/// Shared by every ranking command — <c>/emotestats</c> and <c>/goodbot</c> — which
/// is why it sits in its own file rather than inside whichever service happened to
/// need it first.
/// </remarks>
public enum StatsPeriod
{
    /// <summary>The last 30 days, today included.</summary>
    Month,
    /// <summary>The last 7 days, today included.</summary>
    Week,
    /// <summary>Everything ever counted, including before daily buckets existed.</summary>
    AllTime,
}
