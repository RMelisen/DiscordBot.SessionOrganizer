namespace ProjectSYNCS.Helpers;

/// <summary>What /minecraft invite does. In memory only: free to reorder.</summary>
public enum McInviteVerdict
{
    /// <summary>She joins now and stays at least <see cref="McInvite.Length"/>.</summary>
    Go,
    /// <summary>The owner hasn't allowed her to play (/debug minecraft).</summary>
    NotAllowed,
    /// <summary>The owner sent her away less than 30 minutes ago.</summary>
    Held,
    AlreadyIn,
    /// <summary>The server didn't answer its last ping: a body started now would only fail to connect.</summary>
    ServerDown,
    AlreadyInvited,
}

/// <summary>/minecraft invite's rule, pure: the first refusal that applies, in the enum's order, else go.</summary>
public static class McInvite
{
    /// <summary>How long an invite keeps her in, whatever the presence rule says.</summary>
    public static readonly TimeSpan Length = TimeSpan.FromHours(1);

    /// <param name="serverUp">The server answered its last status ping.</param>
    public static McInviteVerdict Decide(bool allowed, bool serverUp, bool inGame, DateTimeOffset holdUntil,
        DateTimeOffset invitedUntil, DateTimeOffset now) =>
        !allowed ? McInviteVerdict.NotAllowed
        : now < holdUntil ? McInviteVerdict.Held
        : inGame ? McInviteVerdict.AlreadyIn
        : !serverUp ? McInviteVerdict.ServerDown
        : now < invitedUntil ? McInviteVerdict.AlreadyInvited
        : McInviteVerdict.Go;

    /// <summary>Who her arrival line credits: the inviter while the invite runs, nobody once it ran out (its body may
    /// never have spawned, and her next arrival is her own).</summary>
    public static string? Inviter(string? invitedBy, DateTimeOffset invitedUntil, DateTimeOffset now) =>
        now < invitedUntil ? invitedBy : null;
}
