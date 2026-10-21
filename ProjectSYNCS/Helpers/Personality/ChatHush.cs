namespace ProjectSYNCS.Helpers;

/// <summary>
/// The channels where she was told to be quiet (« chut », « tais-toi »), and until when. Pure and
/// self-locking, in memory, lost on restart by design. One singleton, read by every service that
/// chats on its own in a channel (ChatterService, RobotHeartService, RivalryService's mutters):
/// being told to hush is not one service's business. Papa is never refused: the callers let him
/// through.
/// </summary>
public sealed class ChatHush
{
    /// <summary>How long she keeps quiet once told.</summary>
    public static readonly TimeSpan Quiet = TimeSpan.FromMinutes(30);

    private readonly object _lock = new();
    private readonly Dictionary<ulong, DateTimeOffset> _until = new();

    /// <summary>Quiet in that channel from now; a second « chut » starts the wait again.</summary>
    public void Hush(ulong channelId, DateTimeOffset now)
    {
        lock (_lock)
        {
            _until[channelId] = now + Quiet;
            // The map only ever holds channels told recently: anything past its time goes.
            foreach (var stale in _until.Where(e => e.Value <= now).Select(e => e.Key).ToList()) _until.Remove(stale);
        }
    }

    /// <summary>Whether she is keeping quiet in that channel right now.</summary>
    public bool IsHushed(ulong channelId, DateTimeOffset now)
    {
        lock (_lock)
            return _until.TryGetValue(channelId, out var until) && now < until;
    }
}
