using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// One lock per session and one per poll, held around « check → write → reload → redraw ».
/// Without it two people clicking « Rejoindre » on the last place both pass the capacity check,
/// and two votes landing together can redraw the card out of order. Singleton: it owns the state.
/// </summary>
public sealed class SchedulingLocks
{
    public KeyedLock<int> Sessions { get; } = new();
    public KeyedLock<int> Polls { get; } = new();
}
